using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.API.Options;
using StrongTowing.API.Services;
using StrongTowing.Core.Entities;
using StrongTowing.Core.Constants;
using StrongTowing.Core.Enums;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly IOptions<AuthOtpOptions> _otpOptions;
    private readonly IAuthOtpEmailService _authOtpEmailService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        IJwtService jwtService,
        ILogger<AuthController> logger,
        IWebHostEnvironment environment,
        IDataProtectionProvider dataProtectionProvider,
        IOptions<AuthOtpOptions> otpOptions,
        IAuthOtpEmailService authOtpEmailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
        _environment = environment;
        _dataProtectionProvider = dataProtectionProvider;
        _otpOptions = otpOptions;
        _authOtpEmailService = authOtpEmailService;
    }
    
    /// <summary>
    /// Gets the role name from RoleId - always use RoleId as source of truth
    /// </summary>
    private async Task<string> GetRoleNameFromRoleIdAsync(string roleId)
    {
        if (string.IsNullOrEmpty(roleId))
        {
            return string.Empty;
        }

        var role = await _roleManager.FindByIdAsync(roleId);
        return role?.Name ?? string.Empty;
    }

    /// <summary>
    /// Ensures a role exists with the specific ID from UserRoles constants
    /// </summary>
    private async Task<IdentityRole?> EnsureRoleExistsAsync(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        
        if (role == null)
        {
            var expectedRoleId = UserRoles.GetRoleId(roleName);
            
            // Check if role with this ID already exists
            var roleWithId = await _context.Roles.FirstOrDefaultAsync(r => r.Id == expectedRoleId);
            
            if (roleWithId != null)
            {
                // Update existing role name
                roleWithId.Name = roleName;
                roleWithId.NormalizedName = roleName.ToUpper();
                roleWithId.ConcurrencyStamp = Guid.NewGuid().ToString();
                await _roleManager.UpdateAsync(roleWithId);
                role = roleWithId;
            }
            else
            {
                // Create new role with specific ID using raw SQL
                var concurrencyStamp = Guid.NewGuid().ToString();
                var normalizedName = roleName.ToUpper();
                
                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES ({0}, {1}, {2}, {3})",
                    expectedRoleId, roleName, normalizedName, concurrencyStamp);
                
                role = await _roleManager.FindByNameAsync(roleName);
            }
        }
        else
        {
            // Verify role has correct ID
            var expectedRoleId = UserRoles.GetRoleId(roleName);
            if (role.Id != expectedRoleId)
            {
                _logger.LogWarning("Role {RoleName} has incorrect ID {CurrentId}, expected {ExpectedId}", 
                    roleName, role.Id, expectedRoleId);
                
                // Update role ID if target ID is not taken
                var roleWithTargetId = await _context.Roles.FirstOrDefaultAsync(r => r.Id == expectedRoleId);
                if (roleWithTargetId == null)
                {
                    var oldId = role.Id;
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE AspNetRoles SET Id = {0} WHERE Id = {1}",
                        expectedRoleId, oldId);
                    
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE AspNetUsers SET RoleId = {0} WHERE RoleId = {1}",
                        expectedRoleId, oldId);
                    
                    role = await _roleManager.FindByNameAsync(roleName);
                }
            }
        }
        
        return role;
    }

    /// <summary>
    /// User Login
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            
            if (user == null || !user.IsActive)
            {
                return Unauthorized(new { error = "Unauthorized", message = "Invalid credentials" });
            }

            if (!user.EmailConfirmed)
            {
                return Unauthorized(new
                {
                    error = "EmailNotVerified",
                    message = "Please verify your email before signing in. Check your inbox for the code or register again."
                });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
            
            if (!result.Succeeded)
            {
                return Unauthorized(new { error = "Unauthorized", message = "Invalid credentials" });
            }

            // Always use RoleId as source of truth - get role name from RoleId
            var roleName = await GetRoleNameFromRoleIdAsync(user.RoleId);
            
            if (string.IsNullOrEmpty(roleName))
            {
                _logger.LogWarning("User {UserId} has RoleId {RoleId} but role not found", user.Id, user.RoleId);
                return StatusCode(500, new { error = "Internal Server Error", message = "User role not found" });
            }

            // Sync Identity roles with RoleId to keep them in sync
            var identityRoles = await _userManager.GetRolesAsync(user);
            if (!identityRoles.Contains(roleName))
            {
                // Remove all existing Identity roles
                if (identityRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, identityRoles);
                }
                // Add the correct role based on RoleId
                await _userManager.AddToRoleAsync(user, roleName);
            }

            // Use role from RoleId for token generation
            var roles = new List<string> { roleName };
            
            // Generate JWT token
            var token = _jwtService.GenerateToken(user, roles);
            var expiresAt = _jwtService.GetExpirationTime();

            // Generate and store refresh token
            var refreshTokenValue = _jwtService.GenerateRefreshToken();
            var refreshTokenExpiresAt = _jwtService.GetRefreshTokenExpirationTime();
            
            var refreshToken = new RefreshToken
            {
                Token = refreshTokenValue,
                UserId = user.Id,
                ExpiresAt = refreshTokenExpiresAt,
                CreatedAt = DateTime.UtcNow
            };
            
            // Revoke old refresh tokens for this user (optional - for security)
            var now = DateTime.UtcNow;
            var oldTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null && rt.ExpiresAt > now)
                .ToListAsync();
            
            foreach (var oldToken in oldTokens)
            {
                oldToken.RevokedAt = DateTime.UtcNow;
            }
            
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            var response = new LoginResponse
            {
                Token = token,
                RefreshToken = refreshTokenValue,
                User = MapToUserDto(user, roleName),
                ExpiresAt = expiresAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}. Exception: {Exception}", request.Email, ex.ToString());
            // Include exception details in development for debugging
            var errorMessage = _environment.IsDevelopment() 
                ? $"An error occurred during login: {ex.Message}" 
                : "An error occurred during login";
            return StatusCode(500, new { error = "Internal Server Error", message = errorMessage });
        }
    }

    /// <summary>
    /// Public driver self-signup: stores pending registration and sends an email OTP (Postmark).
    /// </summary>
    [HttpPost("signup")]
    [AllowAnonymous]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => new { Field = x.Key, Message = e.ErrorMessage }))
                .ToList();

            return BadRequest(new
            {
                error = "Validation Error",
                message = "One or more validation errors occurred",
                errors = errors
            });
        }

        try
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Conflict(new { error = "Conflict", message = "A user with this email already exists", field = "email" });
            }

            var defaultRole = UserRoles.Driver;
            var role = await EnsureRoleExistsAsync(defaultRole);
            if (role == null)
            {
                return StatusCode(500, new { error = "Server Error", message = "Failed to create or retrieve role" });
            }

            var norm = _userManager.NormalizeEmail(request.Email) ?? request.Email.Trim().ToUpperInvariant();
            var protector = _dataProtectionProvider.CreateProtector("StrongTowing.PendingSignup.v1");
            var protectedPw = Convert.ToBase64String(protector.Protect(Encoding.UTF8.GetBytes(request.Password)));

            var pending = await _context.PendingDriverSignups.FirstOrDefaultAsync(x => x.NormalizedEmail == norm);
            if (pending != null)
            {
                _context.PendingDriverSignups.Remove(pending);
            }

            _context.PendingDriverSignups.Add(new PendingDriverSignup
            {
                NormalizedEmail = norm,
                Email = request.Email.Trim(),
                ProtectedPassword = protectedPw,
                FullName = request.FullName.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
                RoleId = role.Id,
                SmsOptIn = request.SmsOptIn,
                CreatedAtUtc = DateTime.UtcNow
            });

            await InvalidateOtpsAsync(norm, AuthOtpPurpose.Signup, CancellationToken.None);

            var otp = GenerateOtpCode(_otpOptions.Value.CodeLength);
            var pepper = _otpOptions.Value.Pepper;
            var hash = AuthOtpHasher.HashOtp(norm, otp, pepper);
            var expiry = DateTime.UtcNow.AddMinutes(Math.Clamp(_otpOptions.Value.ExpiryMinutes, 1, 60));

            _context.AuthOtpRecords.Add(new AuthOtpRecord
            {
                NormalizedEmail = norm,
                Email = request.Email.Trim(),
                Purpose = AuthOtpPurpose.Signup,
                OtpHash = hash,
                ExpiresAtUtc = expiry,
                Used = false,
                FailedAttemptCount = 0,
                CreatedAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            var plain = $"Your Strong Towing verification code is: {otp}\n\nIt expires in {_otpOptions.Value.ExpiryMinutes} minutes.";
            var html = AuthOtpEmailService.ToSimpleHtml("Verify your email", plain);
            var send = await _authOtpEmailService.SendAsync(request.Email.Trim(), "Verify your Strong Towing account", html, plain, HttpContext.RequestAborted);
            if (!send.Success)
            {
                _logger.LogWarning("Signup OTP email could not be sent: {Err}", send.ErrorMessage);
                return StatusCode(503, new
                {
                    error = "EmailUnavailable",
                    message = send.ErrorMessage ?? "Could not send verification email. Try again later or contact support."
                });
            }

            return Ok(new
            {
                requiresVerification = true,
                message = "We sent a verification code to your email. Enter it to complete registration.",
                email = request.Email.Trim()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during signup request for email: {Email}", request?.Email ?? "unknown");
            return StatusCode(500, new
            {
                error = "Internal Server Error",
                message = "An error occurred during signup. Please try again later.",
                details = _environment.IsDevelopment() ? ex.Message : null
            });
        }
    }

    /// <summary>Completes driver self-signup after the user enters the email OTP.</summary>
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> VerifySignup([FromBody] VerifySignupRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = "Validation Error", message = "Invalid request" });

        try
        {
            var norm = _userManager.NormalizeEmail(request.Email) ?? request.Email.Trim().ToUpperInvariant();
            var pending = await _context.PendingDriverSignups.FirstOrDefaultAsync(x => x.NormalizedEmail == norm);
            if (pending == null)
            {
                return BadRequest(new { error = "InvalidRequest", message = "No pending registration found. Start signup again." });
            }

            var otpRow = await _context.AuthOtpRecords
                .Where(x => x.NormalizedEmail == norm && x.Purpose == AuthOtpPurpose.Signup && !x.Used && x.ExpiresAtUtc > DateTime.UtcNow)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (otpRow == null)
            {
                return BadRequest(new { error = "InvalidOrExpired", message = "Code expired or not found. Request a new code." });
            }

            var pepper = _otpOptions.Value.Pepper;
            if (!AuthOtpHasher.Verify(norm, request.Otp, pepper, otpRow.OtpHash))
            {
                otpRow.FailedAttemptCount++;
                await _context.SaveChangesAsync();
                if (otpRow.FailedAttemptCount >= _otpOptions.Value.MaxFailedAttempts)
                {
                    otpRow.Used = true;
                    await _context.SaveChangesAsync();
                }

                return BadRequest(new { error = "InvalidCode", message = "Invalid verification code." });
            }

            var existingUser = await _userManager.FindByEmailAsync(pending.Email);
            if (existingUser != null)
            {
                _context.PendingDriverSignups.Remove(pending);
                otpRow.Used = true;
                await _context.SaveChangesAsync();
                return Conflict(new { error = "Conflict", message = "An account with this email already exists." });
            }

            var protector = _dataProtectionProvider.CreateProtector("StrongTowing.PendingSignup.v1");
            string password;
            try
            {
                password = Encoding.UTF8.GetString(protector.Unprotect(Convert.FromBase64String(pending.ProtectedPassword)));
            }
            catch
            {
                return BadRequest(new { error = "InvalidRequest", message = "Registration data expired. Please sign up again." });
            }

            var defaultRole = UserRoles.Driver;
            var newUser = new ApplicationUser
            {
                UserName = pending.Email,
                Email = pending.Email,
                EmailConfirmed = true,
                FullName = pending.FullName,
                PhoneNumber = pending.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                RoleId = pending.RoleId,
                SmsOptIn = pending.SmsOptIn,
                SmsOptInUpdatedAtUtc = pending.SmsOptIn ? DateTime.UtcNow : (DateTime?)null
            };

            var createResult = await _userManager.CreateAsync(newUser, password);
            if (!createResult.Succeeded)
            {
                var errorMessages = createResult.Errors.Select(e => new { field = "general", message = e.Description }).ToList();
                return BadRequest(new { error = "Validation Error", message = "Failed to create account", errors = errorMessages });
            }

            var roleResult = await _userManager.AddToRoleAsync(newUser, defaultRole);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(newUser);
                return StatusCode(500, new { error = "Server Error", message = "Failed to assign role" });
            }

            _context.PendingDriverSignups.Remove(pending);
            otpRow.Used = true;
            await _context.SaveChangesAsync();

            var roleName = await GetRoleNameFromRoleIdAsync(newUser.RoleId) ?? defaultRole;
            var roles = new List<string> { roleName };
            var token = _jwtService.GenerateToken(newUser, roles);
            var expiresAt = _jwtService.GetExpirationTime();

            var refreshTokenValue = _jwtService.GenerateRefreshToken();
            var refreshTokenExpiresAt = _jwtService.GetRefreshTokenExpirationTime();
            var refreshToken = new RefreshToken
            {
                Token = refreshTokenValue,
                UserId = newUser.Id,
                ExpiresAt = refreshTokenExpiresAt,
                CreatedAt = DateTime.UtcNow
            };
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return Ok(new LoginResponse
            {
                Token = token,
                RefreshToken = refreshTokenValue,
                User = MapToUserDto(newUser, roleName),
                ExpiresAt = expiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "verify-signup failed");
            return StatusCode(500, new { error = "Internal Server Error", message = "Could not complete verification." });
        }
    }

    /// <summary>Request a password reset code by email.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = "Validation Error", message = "Invalid email" });

        var message =
            "If an account exists for that email, we sent a verification code. Check your inbox and spam folder.";

        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                return Ok(new { message });
            }

            var norm = user.NormalizedEmail ?? _userManager.NormalizeEmail(request.Email) ?? request.Email.Trim().ToUpperInvariant();
            await InvalidateOtpsAsync(norm, AuthOtpPurpose.PasswordReset, CancellationToken.None);

            var otp = GenerateOtpCode(_otpOptions.Value.CodeLength);
            var hash = AuthOtpHasher.HashOtp(norm, otp, _otpOptions.Value.Pepper);
            var expiry = DateTime.UtcNow.AddMinutes(Math.Clamp(_otpOptions.Value.ExpiryMinutes, 1, 60));

            _context.AuthOtpRecords.Add(new AuthOtpRecord
            {
                NormalizedEmail = norm,
                Email = user.Email ?? request.Email.Trim(),
                Purpose = AuthOtpPurpose.PasswordReset,
                OtpHash = hash,
                ExpiresAtUtc = expiry,
                Used = false,
                FailedAttemptCount = 0,
                CreatedAtUtc = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var plain = $"Your Strong Towing password reset code is: {otp}\n\nIt expires in {_otpOptions.Value.ExpiryMinutes} minutes.";
            var html = AuthOtpEmailService.ToSimpleHtml("Reset your password", plain);
            await _authOtpEmailService.SendAsync(user.Email!, "Reset your Strong Towing password", html, plain, HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "forgot-password failed");
        }

        return Ok(new { message });
    }

    /// <summary>Reset password using the email OTP.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordWithOtpRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => new { Field = x.Key, Message = e.ErrorMessage }))
                .ToList();
            return BadRequest(new { error = "Validation Error", errors });
        }

        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                return BadRequest(new { error = "InvalidRequest", message = "Invalid email or code." });
            }

            var norm = user.NormalizedEmail ?? _userManager.NormalizeEmail(request.Email) ?? request.Email.Trim().ToUpperInvariant();
            var otpRow = await _context.AuthOtpRecords
                .Where(x => x.NormalizedEmail == norm && x.Purpose == AuthOtpPurpose.PasswordReset && !x.Used && x.ExpiresAtUtc > DateTime.UtcNow)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (otpRow == null)
            {
                return BadRequest(new { error = "InvalidOrExpired", message = "Code expired or not found. Request a new code." });
            }

            if (!AuthOtpHasher.Verify(norm, request.Otp, _otpOptions.Value.Pepper, otpRow.OtpHash))
            {
                otpRow.FailedAttemptCount++;
                if (otpRow.FailedAttemptCount >= _otpOptions.Value.MaxFailedAttempts)
                    otpRow.Used = true;
                await _context.SaveChangesAsync();
                return BadRequest(new { error = "InvalidCode", message = "Invalid verification code." });
            }

            var identityToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var reset = await _userManager.ResetPasswordAsync(user, identityToken, request.NewPassword);
            if (!reset.Succeeded)
            {
                return BadRequest(new
                {
                    error = "Validation Error",
                    message = string.Join(" ", reset.Errors.Select(e => e.Description))
                });
            }

            user.HasChangedPassword = true;
            user.PasswordChangedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            otpRow.Used = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password updated. You can sign in with your new password." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "reset-password failed");
            return StatusCode(500, new { error = "Internal Server Error", message = "Could not reset password." });
        }
    }

    /// <summary>Resend signup or password-reset OTP.</summary>
    [HttpPost("resend-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = "Validation Error", message = "Invalid request" });

        var purpose = ParseOtpPurpose(request.Purpose);
        if (purpose == null)
            return BadRequest(new { error = "Validation Error", message = "Purpose must be signup or passwordReset." });

        try
        {
            var norm = _userManager.NormalizeEmail(request.Email) ?? request.Email.Trim().ToUpperInvariant();

            if (purpose == AuthOtpPurpose.Signup)
            {
                var pending = await _context.PendingDriverSignups.FirstOrDefaultAsync(x => x.NormalizedEmail == norm);
                if (pending == null)
                    return BadRequest(new { error = "InvalidRequest", message = "No pending registration for this email." });

                await InvalidateOtpsAsync(norm, AuthOtpPurpose.Signup, CancellationToken.None);
                var otp = GenerateOtpCode(_otpOptions.Value.CodeLength);
                var hash = AuthOtpHasher.HashOtp(norm, otp, _otpOptions.Value.Pepper);
                _context.AuthOtpRecords.Add(new AuthOtpRecord
                {
                    NormalizedEmail = norm,
                    Email = pending.Email,
                    Purpose = AuthOtpPurpose.Signup,
                    OtpHash = hash,
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(Math.Clamp(_otpOptions.Value.ExpiryMinutes, 1, 60)),
                    Used = false,
                    FailedAttemptCount = 0,
                    CreatedAtUtc = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                var plain = $"Your Strong Towing verification code is: {otp}\n\nIt expires in {_otpOptions.Value.ExpiryMinutes} minutes.";
                var html = AuthOtpEmailService.ToSimpleHtml("Verify your email", plain);
                var send = await _authOtpEmailService.SendAsync(pending.Email, "Verify your Strong Towing account", html, plain, HttpContext.RequestAborted);
                if (!send.Success)
                    return StatusCode(503, new { error = "EmailUnavailable", message = send.ErrorMessage });

                return Ok(new { message = "A new code was sent to your email." });
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                return Ok(new { message = "If an account exists, a new code was sent." });
            }

            norm = user.NormalizedEmail ?? norm;
            await InvalidateOtpsAsync(norm, AuthOtpPurpose.PasswordReset, CancellationToken.None);
            var otp2 = GenerateOtpCode(_otpOptions.Value.CodeLength);
            var hash2 = AuthOtpHasher.HashOtp(norm, otp2, _otpOptions.Value.Pepper);
            _context.AuthOtpRecords.Add(new AuthOtpRecord
            {
                NormalizedEmail = norm,
                Email = user.Email!,
                Purpose = AuthOtpPurpose.PasswordReset,
                OtpHash = hash2,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(Math.Clamp(_otpOptions.Value.ExpiryMinutes, 1, 60)),
                Used = false,
                FailedAttemptCount = 0,
                CreatedAtUtc = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var plain2 = $"Your Strong Towing password reset code is: {otp2}\n\nIt expires in {_otpOptions.Value.ExpiryMinutes} minutes.";
            var html2 = AuthOtpEmailService.ToSimpleHtml("Reset your password", plain2);
            await _authOtpEmailService.SendAsync(user.Email!, "Reset your Strong Towing password", html2, plain2, HttpContext.RequestAborted);
            return Ok(new { message = "If an account exists, a new code was sent." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "resend-otp failed");
            return StatusCode(500, new { error = "Internal Server Error", message = "Could not resend code." });
        }
    }

    /// <summary>
    /// Register New User (Admin Only)
    /// </summary>
    /// <remarks>
    /// SuperAdmin can create Admins. SuperAdmin and Admin can create Dispatcher and Driver.
    /// </remarks>
    [HttpPost("register")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Get current user
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { error = "Unauthorized", message = "User not authenticated" });
            }

            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            if (currentUser == null || !currentUser.IsActive)
            {
                return Unauthorized(new { error = "Unauthorized", message = "User not found or inactive" });
            }

            // Always use RoleId as source of truth - get role name from RoleId
            var currentUserRole = await GetRoleNameFromRoleIdAsync(currentUser.RoleId);
            if (string.IsNullOrEmpty(currentUserRole))
            {
                return Unauthorized(new { error = "Unauthorized", message = "User role not found" });
            }

            // Validate requested role
            var requestedRole = request.Role.Trim();
            if (!UserRoles.All.Contains(requestedRole, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "Bad Request", message = $"Invalid role. Valid roles are: {string.Join(", ", UserRoles.All)}" });
            }

            // Check permissions based on role hierarchy
            bool canCreate = false;
            if (currentUserRole.Equals(UserRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            {
                // SuperAdmin can create Admins, Dispatchers, and Drivers
                canCreate = requestedRole.Equals(UserRoles.Administrator, StringComparison.OrdinalIgnoreCase) ||
                           requestedRole.Equals(UserRoles.Dispatcher, StringComparison.OrdinalIgnoreCase) ||
                           requestedRole.Equals(UserRoles.Driver, StringComparison.OrdinalIgnoreCase);
            }
            else if (currentUserRole.Equals(UserRoles.Administrator, StringComparison.OrdinalIgnoreCase))
            {
                // Admin can create Dispatchers and Drivers only
                canCreate = requestedRole.Equals(UserRoles.Dispatcher, StringComparison.OrdinalIgnoreCase) ||
                           requestedRole.Equals(UserRoles.Driver, StringComparison.OrdinalIgnoreCase);
            }

            if (!canCreate)
            {
                return StatusCode(403, new { error = "Forbidden", message = "You do not have permission to create users with this role." });
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { error = "Bad Request", message = "User with this email already exists" });
            }

            // Ensure the role exists and get its ID
            var roleName = requestedRole; // Already a string from constants
            var role = await EnsureRoleExistsAsync(roleName);
            if (role == null)
            {
                return StatusCode(500, new { error = "Server Error", message = "Failed to create or retrieve role" });
            }

            // Create new user with RoleId set
            var newUser = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                RoleId = role.Id // Set the RoleId foreign key
            };

            var createResult = await _userManager.CreateAsync(newUser, request.Password);
            
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return BadRequest(new { error = "Bad Request", message = $"Failed to create user: {errors}" });
            }

            // Also add to Identity role system for compatibility
            var roleResult = await _userManager.AddToRoleAsync(newUser, roleName);
            if (!roleResult.Succeeded)
            {
                // If role assignment fails, delete the user
                await _userManager.DeleteAsync(newUser);
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                return BadRequest(new { error = "Bad Request", message = $"Failed to assign role: {errors}" });
            }

            // Always use RoleId as source of truth - get role name from RoleId
            var finalRoleName = await GetRoleNameFromRoleIdAsync(newUser.RoleId);
            if (string.IsNullOrEmpty(finalRoleName))
            {
                finalRoleName = roleName; // Fallback to requested role
            }

            return CreatedAtAction(nameof(Login), new { id = newUser.Id }, MapToUserDto(newUser, finalRoleName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email: {Email}", request.Email);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred during registration" });
        }
    }

    /// <summary>
    /// Change Password
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<UserDto>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            // Get current user
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Unauthorized", message = "User not authenticated" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return Unauthorized(new { error = "Unauthorized", message = "User not found or inactive" });
            }

            // Verify current password
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
            if (!isPasswordValid)
            {
                return BadRequest(new { error = "Bad Request", message = "Current password is incorrect" });
            }

            // Change password
            var changePasswordResult = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                var errors = string.Join(", ", changePasswordResult.Errors.Select(e => e.Description));
                return BadRequest(new { error = "Bad Request", message = $"Failed to change password: {errors}" });
            }

            // Update password change tracking
            user.HasChangedPassword = true;
            user.PasswordChangedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogWarning("Password changed but failed to update tracking fields for user: {UserId}", user.Id);
            }

            var roleName = await GetRoleNameFromRoleIdAsync(user.RoleId) ?? string.Empty;

            return Ok(MapToUserDto(user, roleName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while changing password" });
        }
    }

    /// <summary>
    /// Driver: mark yourself available or off-duty for new job assignments.
    /// </summary>
    [HttpPut("driver/availability")]
    [Authorize(Roles = UserRoles.Driver)]
    public async Task<ActionResult<UserDto>> UpdateDriverAvailability([FromBody] UpdateDriverAvailabilityRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "Unauthorized", message = "User not authenticated" });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return Unauthorized(new { error = "Unauthorized", message = "User not found or inactive" });
        }

        user.IsAvailableForDispatch = request.IsAvailableForDispatch;
        user.UpdatedAt = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { error = "Bad Request", message = $"Failed to update availability: {errors}" });
        }

        var roleName = await GetRoleNameFromRoleIdAsync(user.RoleId) ?? UserRoles.Driver;

        return Ok(MapToUserDto(user, roleName));
    }

    /// <summary>
    /// Refresh Access Token
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest(new { error = "Bad Request", message = "Refresh token is required" });
            }

            // Find the refresh token
            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (refreshToken == null)
            {
                return Unauthorized(new { error = "Unauthorized", message = "Invalid refresh token" });
            }

            // Check if token is active
            if (!refreshToken.IsActive)
            {
                return Unauthorized(new { error = "Unauthorized", message = "Refresh token has expired or been revoked" });
            }

            // Check if user is still active
            if (refreshToken.User == null || !refreshToken.User.IsActive)
            {
                return Unauthorized(new { error = "Unauthorized", message = "User account is inactive" });
            }

            // Get user role
            var roleName = await GetRoleNameFromRoleIdAsync(refreshToken.User.RoleId);
            if (string.IsNullOrEmpty(roleName))
            {
                return StatusCode(500, new { error = "Internal Server Error", message = "User role not found" });
            }

            var roles = new List<string> { roleName };

            // Generate new access token
            var newToken = _jwtService.GenerateToken(refreshToken.User, roles);
            var expiresAt = _jwtService.GetExpirationTime();

            // Optional: Rotate refresh token (generate new one and revoke old one)
            var newRefreshTokenValue = _jwtService.GenerateRefreshToken();
            var newRefreshTokenExpiresAt = _jwtService.GetRefreshTokenExpirationTime();
            
            // Revoke old refresh token
            refreshToken.RevokedAt = DateTime.UtcNow;
            
            // Create new refresh token
            var newRefreshToken = new RefreshToken
            {
                Token = newRefreshTokenValue,
                UserId = refreshToken.User.Id,
                ExpiresAt = newRefreshTokenExpiresAt,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            var response = new LoginResponse
            {
                Token = newToken,
                RefreshToken = newRefreshTokenValue, // Return new refresh token
                User = MapToUserDto(refreshToken.User, roleName),
                ExpiresAt = expiresAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while refreshing token" });
        }
    }

    private async Task InvalidateOtpsAsync(string normalizedEmail, AuthOtpPurpose purpose, CancellationToken cancellationToken)
    {
        var old = await _context.AuthOtpRecords
            .Where(x => x.NormalizedEmail == normalizedEmail && x.Purpose == purpose && !x.Used)
            .ToListAsync(cancellationToken);
        foreach (var o in old)
            o.Used = true;
        if (old.Count > 0)
            await _context.SaveChangesAsync(cancellationToken);
    }

    private string GenerateOtpCode(int length)
    {
        length = Math.Clamp(length, 4, 8);
        var min = (int)Math.Pow(10, length - 1);
        var max = (int)Math.Pow(10, length) - 1;
        return Random.Shared.Next(min, max + 1).ToString(CultureInfo.InvariantCulture);
    }

    private static AuthOtpPurpose? ParseOtpPurpose(string? p)
    {
        if (string.Equals(p, "signup", StringComparison.OrdinalIgnoreCase))
            return AuthOtpPurpose.Signup;
        if (string.Equals(p, "passwordReset", StringComparison.OrdinalIgnoreCase))
            return AuthOtpPurpose.PasswordReset;
        return null;
    }

    private static UserDto MapToUserDto(ApplicationUser user, string roleName)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Role = roleName,
            RoleId = user.RoleId,
            IsActive = user.IsActive,
            HasChangedPassword = user.HasChangedPassword,
            PasswordChangedAt = user.PasswordChangedAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            IsAvailableForDispatch = user.IsAvailableForDispatch
        };
    }
}


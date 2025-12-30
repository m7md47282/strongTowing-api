using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.API.Services;
using StrongTowing.Core.Entities;
using StrongTowing.Core.Constants;
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

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
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

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
            
            if (!result.Succeeded)
            {
                return Unauthorized(new { error = "Unauthorized", message = "Invalid credentials" });
            }

            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);
            
            // Generate JWT token
            var token = _jwtService.GenerateToken(user, roles);
            var expiresAt = _jwtService.GetExpirationTime();

            // Map user to DTO
            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Role = roles.FirstOrDefault() ?? string.Empty,
                RoleId = user.RoleId,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            var response = new LoginResponse
            {
                Token = token,
                User = userDto,
                ExpiresAt = expiresAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Public User Signup
    /// </summary>
    /// <remarks>
    /// Public endpoint for users to create their own account. All users sign up as Drivers by default.
    /// </remarks>
    [HttpPost("signup")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Signup([FromBody] SignupRequest request)
    {
        // Validate model state
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
            // Validate required fields manually (additional check)
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { error = "Validation Error", message = "Email is required", field = "email" });
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "Validation Error", message = "Password is required", field = "password" });
            }

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return BadRequest(new { error = "Validation Error", message = "Full name is required", field = "fullName" });
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Conflict(new { error = "Conflict", message = "A user with this email already exists", field = "email" });
            }

            // Ensure the Driver role exists and get its ID
            var defaultRole = UserRoles.Driver;
            var role = await EnsureRoleExistsAsync(defaultRole);
            if (role == null)
            {
                return StatusCode(500, new { error = "Server Error", message = "Failed to create or retrieve role" });
            }

            // Create new user with RoleId set
            var newUser = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                RoleId = role.Id // Set the RoleId foreign key
            };

            var createResult = await _userManager.CreateAsync(newUser, request.Password);
            
            if (!createResult.Succeeded)
            {
                // Map Identity errors to user-friendly messages
                var errorMessages = createResult.Errors.Select(e => 
                {
                    var field = e.Code switch
                    {
                        "DuplicateUserName" or "DuplicateEmail" => "email",
                        "PasswordTooShort" or "PasswordRequiresDigit" or "PasswordRequiresLower" or 
                        "PasswordRequiresUpper" or "PasswordRequiresNonAlphanumeric" => "password",
                        _ => "general"
                    };
                    return new { field = field, message = e.Description };
                }).ToList();

                return BadRequest(new 
                { 
                    error = "Validation Error",
                    message = "Failed to create user account",
                    errors = errorMessages
                });
            }

            // Also add to Identity role system for compatibility
            var roleResult = await _userManager.AddToRoleAsync(newUser, defaultRole);
            if (!roleResult.Succeeded)
            {
                // If role assignment fails, delete the user
                await _userManager.DeleteAsync(newUser);
                var errors = roleResult.Errors.Select(e => new { field = "role", message = e.Description }).ToList();
                return StatusCode(500, new 
                { 
                    error = "Server Error",
                    message = "Failed to assign user role",
                    errors = errors
                });
            }

            // Get the assigned role
            var userRoles = await _userManager.GetRolesAsync(newUser);

            // Generate JWT token for immediate login
            var token = _jwtService.GenerateToken(newUser, userRoles);
            var expiresAt = _jwtService.GetExpirationTime();

            // Map to DTO
            var userDto = new UserDto
            {
                Id = newUser.Id,
                Email = newUser.Email ?? string.Empty,
                FullName = newUser.FullName,
                PhoneNumber = newUser.PhoneNumber,
                Role = userRoles.FirstOrDefault() ?? defaultRole,
                RoleId = newUser.RoleId,
                IsActive = newUser.IsActive,
                CreatedAt = newUser.CreatedAt,
                UpdatedAt = null
            };

            var response = new LoginResponse
            {
                Token = token,
                User = userDto,
                ExpiresAt = expiresAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during signup for email: {Email}. Exception: {Exception}", 
                request?.Email ?? "unknown", ex.ToString());
            
            return StatusCode(500, new 
            { 
                error = "Internal Server Error", 
                message = "An error occurred during signup. Please try again later.",
                details = ex.Message // Include exception message for debugging
            });
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

            // Get current user's roles
            var currentUserRoles = await _userManager.GetRolesAsync(currentUser);
            var currentUserRole = currentUserRoles.FirstOrDefault() ?? string.Empty;

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

            // Get the assigned role
            var userRoles = await _userManager.GetRolesAsync(newUser);

            // Map to DTO
            var userDto = new UserDto
            {
                Id = newUser.Id,
                Email = newUser.Email ?? string.Empty,
                FullName = newUser.FullName,
                PhoneNumber = newUser.PhoneNumber,
                Role = userRoles.FirstOrDefault() ?? roleName,
                RoleId = newUser.RoleId,
                IsActive = newUser.IsActive,
                CreatedAt = newUser.CreatedAt,
                UpdatedAt = null
            };

            return CreatedAtAction(nameof(Login), new { id = newUser.Id }, userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email: {Email}", request.Email);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred during registration" });
        }
    }
}


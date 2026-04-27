using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Core.Entities;
using StrongTowing.Core.Constants;
using StrongTowing.Infrastructure.Data;
using StrongTowing.Core.Enums;

namespace StrongTowing.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _logger = logger;
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
    /// Check if current user is Admin or SuperAdmin
    /// </summary>
    private async Task<bool> IsAdminOrSuperAdminAsync(ApplicationUser user)
    {
        var roleName = await GetRoleNameFromRoleIdAsync(user.RoleId);
        return roleName.Equals(UserRoles.Administrator, StringComparison.OrdinalIgnoreCase) ||
               roleName.Equals(UserRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get All Users with pagination and filters (Admin only)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<UserDto>>> GetAllUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null)
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

            // Check if user is Admin or SuperAdmin
            if (!await IsAdminOrSuperAdminAsync(currentUser))
            {
                return StatusCode(403, new { error = "Forbidden", message = "Only Administrators can access this endpoint" });
            }

            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Max page size

            // Build query
            var query = _userManager.Users.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(role))
            {
                var roleId = UserRoles.GetRoleId(role);
                if (!string.IsNullOrEmpty(roleId))
                {
                    query = query.Where(u => u.RoleId == roleId);
                }
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(u =>
                    (u.Email != null && u.Email.ToLower().Contains(search)) ||
                    (u.FullName != null && u.FullName.ToLower().Contains(search)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var users = await query
                .OrderBy(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to DTOs
            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var roleName = await GetRoleNameFromRoleIdAsync(user.RoleId);
                userDtos.Add(new UserDto
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
                });
            }

            var response = new PagedResponse<UserDto>
            {
                Data = userDtos,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while retrieving users" });
        }
    }

    /// <summary>
    /// Get Clients (Users with role=User) - For Dispatchers to select clients when creating jobs
    /// </summary>
    [HttpGet("clients")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<PagedResponse<UserDto>>> GetClients(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null)
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

            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            // Build query - Only get Users with role=User (clients)
            var userRoleId = UserRoles.GetRoleId(UserRoles.User);
            var query = _userManager.Users
                .Where(u => u.RoleId == userRoleId)
                .AsQueryable();

            // Apply filters
            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(u =>
                    (u.Email != null && u.Email.ToLower().Contains(search)) ||
                    (u.FullName != null && u.FullName.ToLower().Contains(search)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.ToLower().Contains(search)) ||
                    u.Id.ToLower().Contains(search));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var users = await query
                .OrderBy(u => u.FullName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to DTOs
            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    Role = UserRoles.User, // Always User role for clients
                    RoleId = user.RoleId,
                    IsActive = user.IsActive,
                    HasChangedPassword = user.HasChangedPassword,
                    PasswordChangedAt = user.PasswordChangedAt,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    IsAvailableForDispatch = user.IsAvailableForDispatch
                });
            }

            var response = new PagedResponse<UserDto>
            {
                Data = userDtos,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting clients");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while retrieving clients." });
        }
    }

    /// <summary>
    /// Get Drivers (Users with role=Driver) — pagination, search, optional assignment filter (assigned / unassigned active jobs).
    /// </summary>
    [HttpGet("drivers")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<DriversPagedResponse>> GetDrivers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        [FromQuery] bool? availableForDispatchOnly = null,
        [FromQuery] string? assignment = null)
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

            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            // Build query - Only get Users with role=Driver
            var driverRoleId = UserRoles.GetRoleId(UserRoles.Driver);
            var baseQuery = _userManager.Users
                .Where(u => u.RoleId == driverRoleId)
                .AsQueryable();

            // Apply filters (excluding assignment — applied later)
            if (isActive.HasValue)
            {
                baseQuery = baseQuery.Where(u => u.IsActive == isActive.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                var term = search.Trim().ToLower();
                baseQuery = baseQuery.Where(u =>
                    (u.Email != null && u.Email.ToLower().Contains(term)) ||
                    (u.FullName != null && u.FullName.ToLower().Contains(term)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
            }

            if (availableForDispatchOnly == true)
            {
                baseQuery = baseQuery.Where(u => u.IsAvailableForDispatch);
            }

            // Active job = not Completed / Cancelled (matches dispatcher UI JOB_STATUS_ACTIVE semantics)
            var withActiveJobCount = await baseQuery
                .Where(u => _context.Jobs.Any(j =>
                    j.DriverId == u.Id &&
                    j.Status != JobStatus.Completed &&
                    j.Status != JobStatus.Cancelled))
                .CountAsync();

            var withoutActiveJobCount = await baseQuery
                .Where(u => !_context.Jobs.Any(j =>
                    j.DriverId == u.Id &&
                    j.Status != JobStatus.Completed &&
                    j.Status != JobStatus.Cancelled))
                .CountAsync();

            var filteredQuery = baseQuery;

            if (!string.IsNullOrWhiteSpace(assignment))
            {
                var a = assignment.Trim().ToLowerInvariant();
                if (a == "assigned")
                {
                    filteredQuery = filteredQuery.Where(u =>
                        _context.Jobs.Any(j =>
                            j.DriverId == u.Id &&
                            j.Status != JobStatus.Completed &&
                            j.Status != JobStatus.Cancelled));
                }
                else if (a == "unassigned")
                {
                    filteredQuery = filteredQuery.Where(u =>
                        !_context.Jobs.Any(j =>
                            j.DriverId == u.Id &&
                            j.Status != JobStatus.Completed &&
                            j.Status != JobStatus.Cancelled));
                }
            }

            var totalCount = await filteredQuery.CountAsync();

            var users = await filteredQuery
                .OrderBy(u => u.FullName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to DTOs
            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    Role = UserRoles.Driver, // Always Driver role
                    RoleId = user.RoleId,
                    IsActive = user.IsActive,
                    HasChangedPassword = user.HasChangedPassword,
                    PasswordChangedAt = user.PasswordChangedAt,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    IsAvailableForDispatch = user.IsAvailableForDispatch
                });
            }

            var response = new DriversPagedResponse
            {
                Data = userDtos,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                DriversWithActiveJobCount = withActiveJobCount,
                DriversWithoutActiveJobCount = withoutActiveJobCount
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting drivers");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while retrieving drivers." });
        }
    }

    /// <summary>
    /// Get User by ID (Admin only)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUserById(string id)
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

            // Check if user is Admin or SuperAdmin
            if (!await IsAdminOrSuperAdminAsync(currentUser))
            {
                return StatusCode(403, new { error = "Forbidden", message = "Only Administrators can access this endpoint" });
            }

            // Get requested user
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { error = "Not Found", message = "User not found" });
            }

            var roleName = await GetRoleNameFromRoleIdAsync(user.RoleId);
            var userDto = new UserDto
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

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while retrieving user" });
        }
    }

    /// <summary>
    /// Create User (Admin only)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CreateUserResponse>> CreateUser([FromBody] RegisterRequest request)
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

            // Check if user is Admin or SuperAdmin
            if (!await IsAdminOrSuperAdminAsync(currentUser))
            {
                return StatusCode(403, new { error = "Forbidden", message = "Only Administrators can create users" });
            }

            // Validate requested role
            var requestedRole = request.Role.Trim();
            if (!UserRoles.All.Contains(requestedRole, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "Bad Request", message = $"Invalid role. Valid roles are: {string.Join(", ", UserRoles.All)}" });
            }

            // Get current user's role
            var currentUserRole = await GetRoleNameFromRoleIdAsync(currentUser.RoleId);

            // Check permissions based on role hierarchy
            bool canCreate = false;
            if (currentUserRole.Equals(UserRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            {
                // SuperAdmin can create all roles
                canCreate = UserRoles.All.Contains(requestedRole, StringComparer.OrdinalIgnoreCase);
            }
            else if (currentUserRole.Equals(UserRoles.Administrator, StringComparison.OrdinalIgnoreCase))
            {
                // Admin can create Admin, Dispatcher, Driver, and User
                canCreate = requestedRole.Equals(UserRoles.Administrator, StringComparison.OrdinalIgnoreCase) ||
                           requestedRole.Equals(UserRoles.Dispatcher, StringComparison.OrdinalIgnoreCase) ||
                           requestedRole.Equals(UserRoles.Driver, StringComparison.OrdinalIgnoreCase) ||
                           requestedRole.Equals(UserRoles.User, StringComparison.OrdinalIgnoreCase);
            }

            // SuperAdmin assignment is restricted to SuperAdmin creators
            if (requestedRole.Equals(UserRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase) &&
                !currentUserRole.Equals(UserRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            {
                canCreate = false;
            }

            if (!canCreate)
            {
                return StatusCode(403, new { error = "Forbidden", message = "You do not have permission to create users with this role." });
            }
            
            // Normalize requested role casing to canonical role name
            requestedRole = UserRoles.All.First(r => r.Equals(requestedRole, StringComparison.OrdinalIgnoreCase));

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { error = "Bad Request", message = "User with this email already exists" });
            }

            // Get role ID
            var roleId = UserRoles.GetRoleId(requestedRole);
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return StatusCode(500, new { error = "Server Error", message = "Role not found" });
            }

            // Store temporary password (in-memory only, never stored in DB)
            var temporaryPassword = request.Password;

            // Create new user
            var newUser = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                HasChangedPassword = false, // New users haven't changed password yet
                PasswordChangedAt = null,
                RoleId = roleId
            };

            var createResult = await _userManager.CreateAsync(newUser, temporaryPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return BadRequest(new { error = "Bad Request", message = $"Failed to create user: {errors}" });
            }

            // Add to Identity role system
            var roleResult = await _userManager.AddToRoleAsync(newUser, requestedRole);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(newUser);
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                return BadRequest(new { error = "Bad Request", message = $"Failed to assign role: {errors}" });
            }

            var finalRoleName = await GetRoleNameFromRoleIdAsync(newUser.RoleId);
            var response = new CreateUserResponse
            {
                Id = newUser.Id,
                Email = newUser.Email ?? string.Empty,
                FullName = newUser.FullName,
                PhoneNumber = newUser.PhoneNumber,
                Role = finalRoleName,
                RoleId = newUser.RoleId,
                IsActive = newUser.IsActive,
                HasChangedPassword = newUser.HasChangedPassword,
                PasswordChangedAt = newUser.PasswordChangedAt,
                CreatedAt = newUser.CreatedAt,
                UpdatedAt = null,
                TemporaryPassword = temporaryPassword // Return temporary password only on creation
            };

            return CreatedAtAction(nameof(GetUserById), new { id = newUser.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while creating user" });
        }
    }

    /// <summary>
    /// Update User (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> UpdateUser(string id, [FromBody] UpdateUserRequest request)
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

            // Check if user is Admin or SuperAdmin
            if (!await IsAdminOrSuperAdminAsync(currentUser))
            {
                return StatusCode(403, new { error = "Forbidden", message = "Only Administrators can update users" });
            }

            // Get user to update
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { error = "Not Found", message = "User not found" });
            }

            // Update user properties
            if (!string.IsNullOrEmpty(request.FullName))
            {
                user.FullName = request.FullName;
            }

            if (request.PhoneNumber != null)
            {
                user.PhoneNumber = request.PhoneNumber;
            }

            if (request.IsActive.HasValue)
            {
                user.IsActive = request.IsActive.Value;
            }

            // Role update flow
            var roleChanged = false;
            var requestedRole = request.Role?.Trim();
            var currentUserRole = await GetRoleNameFromRoleIdAsync(currentUser.RoleId);
            var targetUserCurrentRole = await GetRoleNameFromRoleIdAsync(user.RoleId);
            var originalRoleId = user.RoleId;
            var targetUserIdentityRoles = await _userManager.GetRolesAsync(user);

            if (!string.IsNullOrEmpty(requestedRole))
            {
                if (!UserRoles.All.Contains(requestedRole, StringComparer.OrdinalIgnoreCase))
                {
                    return BadRequest(new { error = "Bad Request", message = $"Invalid role. Valid roles are: {string.Join(", ", UserRoles.All)}" });
                }

                // Normalize requested role casing to canonical role name
                requestedRole = UserRoles.All.First(r => r.Equals(requestedRole, StringComparison.OrdinalIgnoreCase));

                // Enforce hierarchy rules
                bool canAssignRole = false;
                if (currentUserRole.Equals(UserRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
                {
                    canAssignRole = true;
                }
                else if (currentUserRole.Equals(UserRoles.Administrator, StringComparison.OrdinalIgnoreCase))
                {
                    canAssignRole = requestedRole.Equals(UserRoles.Administrator, StringComparison.OrdinalIgnoreCase) ||
                                    requestedRole.Equals(UserRoles.Dispatcher, StringComparison.OrdinalIgnoreCase) ||
                                    requestedRole.Equals(UserRoles.Driver, StringComparison.OrdinalIgnoreCase) ||
                                    requestedRole.Equals(UserRoles.User, StringComparison.OrdinalIgnoreCase);
                }

                if (requestedRole.Equals(UserRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase) &&
                    !currentUserRole.Equals(UserRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
                {
                    canAssignRole = false;
                }

                if (!canAssignRole)
                {
                    return StatusCode(403, new { error = "Forbidden", message = "You do not have permission to assign this role." });
                }

                if (!targetUserCurrentRole.Equals(requestedRole, StringComparison.OrdinalIgnoreCase))
                {
                    var newRoleId = UserRoles.GetRoleId(requestedRole);
                    if (string.IsNullOrEmpty(newRoleId))
                    {
                        return BadRequest(new { error = "Bad Request", message = "Role not configured properly." });
                    }

                    user.RoleId = newRoleId;
                    roleChanged = true;
                }
            }

            user.UpdatedAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                return BadRequest(new { error = "Bad Request", message = $"Failed to update user: {errors}" });
            }

            // Keep Identity roles in sync with RoleId
            if (roleChanged && !string.IsNullOrEmpty(requestedRole))
            {
                if (targetUserIdentityRoles.Any())
                {
                    var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, targetUserIdentityRoles);
                    if (!removeRolesResult.Succeeded)
                    {
                        // Best-effort rollback RoleId
                        user.RoleId = originalRoleId;
                        user.UpdatedAt = DateTime.UtcNow;
                        await _userManager.UpdateAsync(user);

                        var errors = string.Join(", ", removeRolesResult.Errors.Select(e => e.Description));
                        return BadRequest(new { error = "Bad Request", message = $"Failed to remove old role assignments: {errors}" });
                    }
                }

                var addRoleResult = await _userManager.AddToRoleAsync(user, requestedRole);
                if (!addRoleResult.Succeeded)
                {
                    // Best-effort rollback role membership
                    var currentRolesAfterFailure = await _userManager.GetRolesAsync(user);
                    if (currentRolesAfterFailure.Any())
                    {
                        await _userManager.RemoveFromRolesAsync(user, currentRolesAfterFailure);
                    }
                    if (targetUserIdentityRoles.Any())
                    {
                        await _userManager.AddToRolesAsync(user, targetUserIdentityRoles);
                    }

                    // Best-effort rollback RoleId
                    user.RoleId = originalRoleId;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    var errors = string.Join(", ", addRoleResult.Errors.Select(e => e.Description));
                    return BadRequest(new { error = "Bad Request", message = $"Failed to assign new role: {errors}" });
                }
            }

            var roleName = await GetRoleNameFromRoleIdAsync(user.RoleId);
            var userDto = new UserDto
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

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", id);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while updating user" });
        }
    }

    /// <summary>
    /// Permanently delete user (Admin only). Related jobs are unassigned where applicable.
    /// Blocked while the user owns vehicles or has cash-collection / payroll records tied as driver.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUser(string id)
    {
        try
        {
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

            if (!await IsAdminOrSuperAdminAsync(currentUser))
            {
                return StatusCode(403, new { error = "Forbidden", message = "Only Administrators can delete users" });
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { error = "Not Found", message = "User not found" });
            }

            if (user.Id == currentUserId)
            {
                return BadRequest(new { error = "Bad Request", message = "You cannot delete your own account" });
            }

            if (await _context.Vehicles.AnyAsync(v => v.OwnerId == id))
            {
                return Conflict(new
                {
                    error = "Conflict",
                    message = "Cannot delete this user while they still own vehicles. Transfer ownership or delete those vehicles first."
                });
            }

            if (await _context.CashCollections.AnyAsync(c => c.DriverId == id))
            {
                return Conflict(new
                {
                    error = "Conflict",
                    message = "Cannot delete this driver while cash collection records reference them."
                });
            }

            if (await _context.DriverPayrolls.AnyAsync(p => p.DriverId == id))
            {
                return Conflict(new
                {
                    error = "Conflict",
                    message = "Cannot delete this driver while payroll records reference them."
                });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            await _context.Jobs
                .Where(j => j.DriverId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(j => j.DriverId, (string?)null));

            await _context.Jobs
                .Where(j => j.StatusUpdatedById == id)
                .ExecuteUpdateAsync(s => s.SetProperty(j => j.StatusUpdatedById, (string?)null));

            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                await transaction.RollbackAsync();
                var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
                return BadRequest(new { error = "Bad Request", message = $"Failed to delete user: {errors}" });
            }

            await transaction.CommitAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {UserId}", id);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while deleting user" });
        }
    }
}

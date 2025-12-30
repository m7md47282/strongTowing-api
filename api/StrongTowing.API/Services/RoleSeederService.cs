using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Core.Constants;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.API.Services;

/// <summary>
/// Service to seed roles with specific IDs
/// </summary>
public class RoleSeederService
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RoleSeederService> _logger;

    public RoleSeederService(
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        ILogger<RoleSeederService> logger)
    {
        _roleManager = roleManager;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Ensures all roles exist with their specific IDs
    /// </summary>
    public async Task SeedRolesAsync()
    {
        try
        {
            foreach (var roleName in UserRoles.All)
            {
                var expectedRoleId = UserRoles.GetRoleId(roleName);
                
                // Check if role exists by name
                var existingRole = await _roleManager.FindByNameAsync(roleName);
                
                if (existingRole == null)
                {
                    // Check if a role with the expected ID already exists
                    var roleWithId = await _context.Roles.FirstOrDefaultAsync(r => r.Id == expectedRoleId);
                    
                    if (roleWithId != null)
                    {
                        // Role with this ID exists but has different name - update it
                        _logger.LogWarning("Role with ID {RoleId} exists with name {ExistingName}, updating to {NewName}", 
                            expectedRoleId, roleWithId.Name, roleName);
                        
                        roleWithId.Name = roleName;
                        roleWithId.NormalizedName = roleName.ToUpper();
                        roleWithId.ConcurrencyStamp = Guid.NewGuid().ToString();
                        await _roleManager.UpdateAsync(roleWithId);
                    }
                    else
                    {
                        // Create new role with specific ID using raw SQL (since Identity doesn't allow setting ID directly)
                        var concurrencyStamp = Guid.NewGuid().ToString();
                        var normalizedName = roleName.ToUpper();
                        
                        await _context.Database.ExecuteSqlRawAsync(
                            "INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES ({0}, {1}, {2}, {3})",
                            expectedRoleId, roleName, normalizedName, concurrencyStamp);
                        
                        _logger.LogInformation("Created role {RoleName} with ID {RoleId}", roleName, expectedRoleId);
                    }
                }
                else if (existingRole.Id != expectedRoleId)
                {
                    // Role exists but with wrong ID - need to update
                    _logger.LogWarning("Role {RoleName} exists with ID {ExistingId}, updating to {ExpectedId}", 
                        roleName, existingRole.Id, expectedRoleId);
                    
                    // Check if target ID is already taken
                    var roleWithTargetId = await _context.Roles.FirstOrDefaultAsync(r => r.Id == expectedRoleId);
                    if (roleWithTargetId != null && roleWithTargetId.Name != roleName)
                    {
                        _logger.LogError("Cannot update role {RoleName} to ID {ExpectedId} - ID already taken by role {OtherRole}", 
                            roleName, expectedRoleId, roleWithTargetId.Name);
                        continue;
                    }
                    
                    // Update the role ID using raw SQL
                    var oldId = existingRole.Id;
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE AspNetRoles SET Id = {0} WHERE Id = {1}",
                        expectedRoleId, oldId);
                    
                    // Also update any foreign key references
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE AspNetUsers SET RoleId = {0} WHERE RoleId = {1}",
                        expectedRoleId, oldId);
                    
                    _logger.LogInformation("Updated role {RoleName} ID from {OldId} to {NewId}", 
                        roleName, oldId, expectedRoleId);
                }
                else
                {
                    _logger.LogDebug("Role {RoleName} already exists with correct ID {RoleId}", roleName, expectedRoleId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding roles");
            throw;
        }
    }
}


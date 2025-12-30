namespace StrongTowing.Core.Constants;

/// <summary>
/// User role constants - similar to frontend constants
/// </summary>
public static class UserRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Administrator = "Administrator";
    public const string Dispatcher = "Dispatcher";
    public const string Driver = "Driver";
    public const string User = "User";
    
    /// <summary>
    /// Array of all valid role names
    /// </summary>
    public static readonly string[] All = new[]
    {
        SuperAdmin,
        Administrator,
        Dispatcher,
        Driver,
        User
    };
    
    /// <summary>
    /// Role ID mappings - maps role names to their specific IDs
    /// </summary>
    public static readonly Dictionary<string, string> RoleIds = new()
    {
        { SuperAdmin, "69" },
        { Administrator, "1" },
        { Dispatcher, "2" },
        { Driver, "3" },
        { User, "4" }
    };
    
    /// <summary>
    /// Gets the role ID for a given role name
    /// </summary>
    public static string GetRoleId(string roleName)
    {
        return RoleIds.TryGetValue(roleName, out var roleId) ? roleId : string.Empty;
    }
}


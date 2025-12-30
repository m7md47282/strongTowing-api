using StrongTowing.Core.Enums;

namespace StrongTowing.Core.Helpers;

/// <summary>
/// Helper class for converting between UserRole enum and string values
/// </summary>
public static class UserRoleHelper
{
    /// <summary>
    /// Converts UserRole enum to string (for Identity which uses strings)
    /// </summary>
    public static string ToString(UserRole role)
    {
        return role.ToString();
    }

    /// <summary>
    /// Converts string to UserRole enum
    /// </summary>
    public static bool TryParse(string roleString, out UserRole role)
    {
        return Enum.TryParse<UserRole>(roleString, true, out role);
    }

    /// <summary>
    /// Gets all valid role names as array
    /// </summary>
    public static string[] GetAllRoleNames()
    {
        return Enum.GetNames(typeof(UserRole));
    }

    /// <summary>
    /// Checks if a string is a valid role
    /// </summary>
    public static bool IsValidRole(string roleString)
    {
        return Enum.TryParse<UserRole>(roleString, true, out _);
    }
}


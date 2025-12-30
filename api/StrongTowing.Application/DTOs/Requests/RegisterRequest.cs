using StrongTowing.Core.Constants;

namespace StrongTowing.Application.DTOs.Requests;

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    /// <summary>
    /// User role. Valid values: UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Dispatcher, UserRoles.Driver
    /// </summary>
    public string Role { get; set; } = string.Empty; // Use UserRoles constants
}


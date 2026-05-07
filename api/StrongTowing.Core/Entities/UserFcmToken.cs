using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Core.Entities;

/// <summary>
/// FCM device tokens for a user. Only the API may send notifications; the client registers tokens here.
/// </summary>
public class UserFcmToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    /// <summary>FCM registration token (per browser/device). Stored as nvarchar(max) for SQL Server.</summary>
    [Required]
    public string Token { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

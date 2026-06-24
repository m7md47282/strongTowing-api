namespace StrongTowing.Core.Entities;

/// <summary>
/// Holds driver self-signup data until the user verifies their email with an OTP.
/// </summary>
public class PendingDriverSignup
{
    /// <summary>Normalized email (Identity normalization).</summary>
    public string NormalizedEmail { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>Data-protection payload for the password (not plain text).</summary>
    public string ProtectedPassword { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    /// <summary>AspNetRoles.Id for the Driver role.</summary>
    public string RoleId { get; set; } = string.Empty;

    /// <summary>
    /// Captured at signup-form submit so we can carry the SMS opt-in choice onto the new
    /// ApplicationUser after the user verifies their email OTP.
    /// </summary>
    public bool SmsOptIn { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

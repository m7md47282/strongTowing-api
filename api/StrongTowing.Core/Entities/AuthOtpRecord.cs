using StrongTowing.Core.Enums;

namespace StrongTowing.Core.Entities;

public class AuthOtpRecord
{
    public int Id { get; set; }

    public string NormalizedEmail { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public AuthOtpPurpose Purpose { get; set; }

    public string OtpHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public bool Used { get; set; }

    public int FailedAttemptCount { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

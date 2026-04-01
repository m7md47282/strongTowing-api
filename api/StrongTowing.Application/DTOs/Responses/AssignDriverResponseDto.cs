namespace StrongTowing.Application.DTOs.Responses;

/// <summary>
/// Result of assigning a driver, including push notification outcome for the UI.
/// </summary>
public class AssignDriverResponseDto
{
    public JobDto Job { get; set; } = null!;

    /// <summary>True if at least one FCM message was accepted for delivery.</summary>
    public bool NotificationSent { get; set; }

    /// <summary>Set when <see cref="NotificationSent"/> is false — explains missing push (config, no tokens, or provider error).</summary>
    public string? NotificationMessage { get; set; }
}

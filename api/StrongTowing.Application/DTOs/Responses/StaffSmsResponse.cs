namespace StrongTowing.Application.DTOs.Responses;

public sealed class StaffSmsResponse
{
    public bool Success { get; set; }
    public string? ToE164 { get; set; }
    public string? TwilioMessageSid { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>Provider error code (e.g. Twilio code such as 30032). Null when no error code was returned.</summary>
    public int? ErrorCode { get; set; }

    /// <summary>Last-known provider status (e.g. "queued", "failed", "delivered").</summary>
    public string? Status { get; set; }
}

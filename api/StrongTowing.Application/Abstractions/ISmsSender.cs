namespace StrongTowing.Application.Abstractions;

/// <summary>
/// Low-level SMS send (e.g. Twilio). Credentials are supplied per call from database-backed system settings.
/// </summary>
public interface ISmsSender
{
    Task<SmsSendResult> SendAsync(
        string accountSid,
        string authToken,
        string? fromE164,
        string? messagingServiceSid,
        string toE164,
        string body,
        CancellationToken cancellationToken = default);
}

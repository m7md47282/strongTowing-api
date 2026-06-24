namespace StrongTowing.Application.Abstractions;

/// <summary>
/// Low-level SMS send (e.g. Twilio). Credentials are supplied per call from database-backed system settings.
/// </summary>
public interface ISmsSender
{
    /// <param name="waitForDeliveryAttempt">
    /// When true, the sender briefly polls the provider after creating the message so it can surface
    /// terminal failures that happen shortly after the API accepts the request (e.g. Twilio error 30032
    /// for unverified toll-free numbers). Use this from interactive paths like the test-SMS endpoint.
    /// Background notification paths should leave it false to avoid added latency.
    /// </param>
    Task<SmsSendResult> SendAsync(
        string accountSid,
        string authToken,
        string? fromE164,
        string? messagingServiceSid,
        string toE164,
        string body,
        CancellationToken cancellationToken = default,
        bool waitForDeliveryAttempt = false);
}

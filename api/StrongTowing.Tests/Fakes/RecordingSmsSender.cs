using StrongTowing.Application.Abstractions;

namespace StrongTowing.Tests.Fakes;

/// <summary>
/// Test double for ISmsSender — records outbound calls without calling Twilio.
/// </summary>
public sealed class RecordingSmsSender : ISmsSender
{
    public List<(string ToE164, string Body)> Calls { get; } = new();

    public Task<SmsSendResult> SendAsync(
        string accountSid,
        string authToken,
        string? fromE164,
        string? messagingServiceSid,
        string toE164,
        string body,
        CancellationToken cancellationToken = default)
    {
        Calls.Add((toE164, body));
        return Task.FromResult(new SmsSendResult(true, null, "SM_test"));
    }
}

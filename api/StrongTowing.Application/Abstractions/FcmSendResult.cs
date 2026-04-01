namespace StrongTowing.Application.Abstractions;

/// <summary>
/// Outcome of attempting to send a push notification to a user's registered FCM tokens.
/// </summary>
public sealed class FcmSendResult
{
    public bool Sent { get; init; }

    /// <summary>Human-readable explanation when <see cref="Sent"/> is false (misconfiguration, no tokens, or send failure).</summary>
    public string? Message { get; init; }

    public static FcmSendResult Ok() => new() { Sent = true };

    public static FcmSendResult Failed(string message) => new() { Sent = false, Message = message };
}

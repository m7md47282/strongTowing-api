namespace StrongTowing.Application.Abstractions;

/// <summary>
/// Outcome of an SMS send attempt.
/// </summary>
/// <param name="Success">True only when the carrier accepted the message and no terminal failure status was observed.</param>
/// <param name="ErrorMessage">Friendly, actionable error description when <see cref="Success"/> is false.</param>
/// <param name="TwilioMessageSid">Provider message SID (always set on a successful create call, even on failed delivery).</param>
/// <param name="ErrorCode">Provider error code (e.g. Twilio code such as 30032). Null when no error code was returned.</param>
/// <param name="Status">Provider status string at the time the call returned (e.g. "queued", "failed", "delivered").</param>
public sealed record SmsSendResult(
    bool Success,
    string? ErrorMessage = null,
    string? TwilioMessageSid = null,
    int? ErrorCode = null,
    string? Status = null);

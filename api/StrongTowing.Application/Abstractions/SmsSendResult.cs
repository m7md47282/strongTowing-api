namespace StrongTowing.Application.Abstractions;

public sealed record SmsSendResult(bool Success, string? ErrorMessage = null, string? TwilioMessageSid = null);

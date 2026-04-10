namespace StrongTowing.Application.Abstractions;

public sealed record EmailSendResult(bool Success, string? ErrorMessage = null, string? PostmarkMessageId = null);

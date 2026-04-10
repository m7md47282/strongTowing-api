namespace StrongTowing.Application.Abstractions;

/// <summary>
/// Low-level transactional email send (e.g. Postmark). Server token supplied per call from database-backed system settings.
/// </summary>
public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(
        string serverToken,
        string fromEmail,
        string? messageStream,
        string toEmail,
        string subject,
        string htmlBody,
        string? textBody,
        CancellationToken cancellationToken = default);
}

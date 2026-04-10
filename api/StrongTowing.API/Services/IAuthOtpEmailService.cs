namespace StrongTowing.API.Services;

/// <summary>
/// Sends transactional auth emails (OTP) using Postmark settings from SystemSettings.
/// </summary>
public interface IAuthOtpEmailService
{
    Task<(bool Success, string? ErrorMessage)> SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string plainTextBody,
        CancellationToken cancellationToken = default);
}

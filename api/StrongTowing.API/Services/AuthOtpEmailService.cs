using System.Net;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.Abstractions;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.API.Services;

public sealed class AuthOtpEmailService : IAuthOtpEmailService
{
    private readonly ApplicationDbContext _db;
    private readonly IEncryptionService _encryption;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AuthOtpEmailService> _logger;

    public AuthOtpEmailService(
        ApplicationDbContext db,
        IEncryptionService encryption,
        IEmailSender emailSender,
        ILogger<AuthOtpEmailService> logger)
    {
        _db = db;
        _encryption = encryption;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<(bool Success, string? ErrorMessage)> SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string plainTextBody,
        CancellationToken cancellationToken = default)
    {
        var settings = await _db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (settings is not { EmailEnabled: true })
        {
            _logger.LogWarning("Auth OTP email skipped: Email is disabled in system settings.");
            return (false, "Email is not configured. Ask an administrator to enable Postmark in System Settings.");
        }

        if (string.IsNullOrWhiteSpace(settings.PostmarkServerToken) || string.IsNullOrWhiteSpace(settings.PostmarkDefaultFromEmail))
        {
            _logger.LogWarning("Auth OTP email skipped: Postmark is not configured.");
            return (false, "Email provider is not configured. Ask an administrator to add Postmark credentials in System Settings.");
        }

        string token;
        try
        {
            token = _encryption.Decrypt(settings.PostmarkServerToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Auth OTP email: could not decrypt Postmark token.");
            return (false, "Email configuration error. Please contact support.");
        }

        if (string.IsNullOrWhiteSpace(token))
            return (false, "Email configuration error.");

        var stream = string.IsNullOrWhiteSpace(settings.PostmarkMessageStream) ? null : settings.PostmarkMessageStream;

        var result = await _emailSender.SendAsync(
            token,
            settings.PostmarkDefaultFromEmail.Trim(),
            stream,
            toEmail.Trim(),
            subject.Trim(),
            htmlBody,
            plainTextBody,
            cancellationToken).ConfigureAwait(false);

        if (!result.Success)
            _logger.LogWarning("Auth OTP email send failed: {Msg}", result.ErrorMessage);

        return (result.Success, result.ErrorMessage);
    }

    public static string ToSimpleHtml(string title, string plainLines)
    {
        var esc = WebUtility.HtmlEncode(plainLines);
        var withBreaks = esc.Replace("\n", "<br/>", StringComparison.Ordinal);
        return $"<html><body style=\"font-family:sans-serif;font-size:15px;line-height:1.5;\"><p style=\"font-weight:600;\">{WebUtility.HtmlEncode(title)}</p><p>{withBreaks}</p><p style=\"color:#666;font-size:13px;\">If you did not request this, you can ignore this message.</p></body></html>";
    }
}

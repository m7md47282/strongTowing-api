using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.Abstractions;
using StrongTowing.Core.Entities;
using StrongTowing.Core.Enums;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.API.Services;

public sealed class EmailNotificationService : IEmailNotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly IEncryptionService _encryption;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        IEncryptionService encryption,
        ILogger<EmailNotificationService> logger)
    {
        _db = db;
        _userManager = userManager;
        _emailSender = emailSender;
        _encryption = encryption;
        _logger = logger;
    }

    public Task NotifyDriverJobAssignedAsync(int jobId, string pickupSummary, string driverUserId, CancellationToken cancellationToken = default) =>
        SendDriverAsync(
            driverUserId,
            s => s.EmailDriverJobAssigned,
            $"Strong Towing: New job #{jobId} — {pickupSummary}. Open the driver app for details.",
            $"Job #{jobId} update",
            cancellationToken);

    public Task NotifyDriverJobCompletedAsync(int jobId, string driverUserId, CancellationToken cancellationToken = default) =>
        SendDriverAsync(
            driverUserId,
            s => s.EmailDriverJobCompleted,
            $"Strong Towing: Job #{jobId} marked completed.",
            $"Job #{jobId} completed",
            cancellationToken);

    public Task NotifyDriverPayrollPaidAsync(
        string driverUserId,
        DateTime payPeriodStart,
        DateTime payPeriodEnd,
        decimal netPay,
        CancellationToken cancellationToken = default) =>
        SendDriverAsync(
            driverUserId,
            s => s.EmailDriverPayrollPaid,
            $"Strong Towing: Payroll {payPeriodStart:MM/dd/yyyy}–{payPeriodEnd:MM/dd/yyyy} marked paid. Net: ${netPay:0.00}.",
            "Payroll notification",
            cancellationToken);

    public Task NotifyClientJobCreatedAsync(int jobId, string? contactEmail, string? ownerEmail, CancellationToken cancellationToken = default) =>
        SendClientAsync(
            contactEmail,
            ownerEmail,
            s => s.EmailClientJobCreated,
            $"Strong Towing: We received your request. Job #{jobId}. We'll update you shortly.",
            $"Job #{jobId} received",
            cancellationToken);

    public Task NotifyClientFraudUnderReviewAsync(int jobId, string? contactEmail, string? ownerEmail, CancellationToken cancellationToken = default) =>
        SendClientAsync(
            contactEmail,
            ownerEmail,
            s => s.EmailClientFraudUnderReview,
            $"Strong Towing: Job #{jobId} is under review. We'll contact you shortly.",
            $"Job #{jobId} under review",
            cancellationToken);

    public Task NotifyClientDriverAssignedAsync(int jobId, string? contactEmail, string? ownerEmail, CancellationToken cancellationToken = default) =>
        SendClientAsync(
            contactEmail,
            ownerEmail,
            s => s.EmailClientDriverAssigned,
            $"Strong Towing: A driver is assigned to job #{jobId}.",
            $"Driver assigned — job #{jobId}",
            cancellationToken);

    public Task NotifyClientJobStatusAsync(
        int jobId,
        JobStatus status,
        string? contactEmail,
        string? ownerEmail,
        CancellationToken cancellationToken = default)
    {
        if (status is not (JobStatus.OnRoute or JobStatus.OnScene or JobStatus.Loaded))
            return Task.CompletedTask;

        Func<SystemSettings, bool> toggle = status switch
        {
            JobStatus.OnRoute => s => s.EmailClientStatusOnRoute,
            JobStatus.OnScene => s => s.EmailClientStatusOnScene,
            JobStatus.Loaded => s => s.EmailClientStatusLoaded,
            _ => _ => false
        };

        var msg = status switch
        {
            JobStatus.OnRoute => $"Strong Towing: Driver en route for job #{jobId}.",
            JobStatus.OnScene => $"Strong Towing: Driver on scene for job #{jobId}.",
            JobStatus.Loaded => $"Strong Towing: Vehicle loaded for job #{jobId}.",
            _ => ""
        };

        var subj = status switch
        {
            JobStatus.OnRoute => $"Job #{jobId} — driver en route",
            JobStatus.OnScene => $"Job #{jobId} — on scene",
            JobStatus.Loaded => $"Job #{jobId} — vehicle loaded",
            _ => $"Job #{jobId} update"
        };

        return SendClientAsync(contactEmail, ownerEmail, toggle, msg, subj, cancellationToken);
    }

    public Task NotifyClientPaymentLinkCreatedAsync(
        int jobId,
        decimal amount,
        string linkUrl,
        string? contactEmail,
        string? ownerEmail,
        CancellationToken cancellationToken = default) =>
        SendClientAsync(
            contactEmail,
            ownerEmail,
            s => s.EmailClientPaymentLinkCreated,
            $"Strong Towing: Pay ${amount:0.00} for job #{jobId}: {linkUrl}",
            $"Pay for job #{jobId}",
            cancellationToken);

    public Task NotifyClientPaymentSucceededAsync(
        int jobId,
        decimal? amount,
        string? contactEmail,
        string? ownerEmail,
        CancellationToken cancellationToken = default)
    {
        var amt = amount.HasValue ? $" ${amount:0.00}" : "";
        return SendClientAsync(
            contactEmail,
            ownerEmail,
            s => s.EmailClientPaymentSucceeded,
            $"Strong Towing: Payment{amt} received for job #{jobId}. Thank you.",
            $"Payment received — job #{jobId}",
            cancellationToken);
    }

    public Task NotifyClientPaymentFailedAsync(int jobId, string? contactEmail, string? ownerEmail, CancellationToken cancellationToken = default) =>
        SendClientAsync(
            contactEmail,
            ownerEmail,
            s => s.EmailClientPaymentFailed,
            $"Strong Towing: Payment could not be completed for job #{jobId}. Please try again or contact us.",
            $"Payment issue — job #{jobId}",
            cancellationToken);

    public Task NotifyClientJobCancelledAsync(
        int jobId,
        decimal? feeAmount,
        string? contactEmail,
        string? ownerEmail,
        CancellationToken cancellationToken = default)
    {
        var fee = feeAmount is > 0
            ? $" A cancellation fee of ${feeAmount:0.00} may apply."
            : "";
        return SendClientAsync(
            contactEmail,
            ownerEmail,
            s => s.EmailClientJobCancelled,
            $"Strong Towing: Job #{jobId} was cancelled.{fee}",
            $"Job #{jobId} cancelled",
            cancellationToken);
    }

    public Task NotifyClientJobCompletedAsync(int jobId, string? contactEmail, string? ownerEmail, CancellationToken cancellationToken = default) =>
        SendClientAsync(
            contactEmail,
            ownerEmail,
            s => s.EmailClientJobCompleted,
            $"Strong Towing: Job #{jobId} is completed. Thank you.",
            $"Job #{jobId} completed",
            cancellationToken);

    private async Task SendDriverAsync(
        string driverUserId,
        Func<SystemSettings, bool> toggle,
        string body,
        string subject,
        CancellationToken cancellationToken)
    {
        var settings = await _db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (settings is not { EmailEnabled: true } || !toggle(settings))
            return;

        if (!CanSend(settings))
            return;

        var user = await _userManager.FindByIdAsync(driverUserId).ConfigureAwait(false);
        var email = NormalizeEmail(user?.Email);
        if (email == null)
        {
            _logger.LogDebug("Email skipped: no driver email for user {UserId}", driverUserId);
            return;
        }

        await DispatchSendAsync(settings, email, subject, body, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendClientAsync(
        string? contactEmail,
        string? ownerEmail,
        Func<SystemSettings, bool> toggle,
        string body,
        string subject,
        CancellationToken cancellationToken)
    {
        var settings = await _db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (settings is not { EmailEnabled: true } || !toggle(settings))
            return;

        if (!CanSend(settings))
            return;

        var email = ResolveClientEmail(contactEmail, ownerEmail);
        if (email == null)
        {
            _logger.LogDebug("Email skipped: no client email for message.");
            return;
        }

        await DispatchSendAsync(settings, email, subject, body, cancellationToken).ConfigureAwait(false);
    }

    private static bool CanSend(SystemSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.PostmarkServerToken) || string.IsNullOrWhiteSpace(settings.PostmarkDefaultFromEmail))
            return false;
        return true;
    }

    private static string? ResolveClientEmail(string? contactEmail, string? ownerEmail)
    {
        var c = NormalizeEmail(contactEmail);
        if (c != null)
            return c;
        return NormalizeEmail(ownerEmail);
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;
        var t = email.Trim();
        return t.Contains('@', StringComparison.Ordinal) ? t : null;
    }

    private async Task DispatchSendAsync(SystemSettings settings, string toEmail, string subject, string plainBody, CancellationToken cancellationToken)
    {
        string token;
        try
        {
            token = _encryption.Decrypt(settings.PostmarkServerToken!);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email skipped: could not decrypt Postmark server token.");
            return;
        }

        if (string.IsNullOrWhiteSpace(token))
            return;

        var html = ToHtmlEmail(plainBody);
        var stream = string.IsNullOrWhiteSpace(settings.PostmarkMessageStream) ? null : settings.PostmarkMessageStream;

        var result = await _emailSender.SendAsync(
            token,
            settings.PostmarkDefaultFromEmail!,
            stream,
            toEmail,
            subject,
            html,
            plainBody,
            cancellationToken).ConfigureAwait(false);

        if (!result.Success)
            _logger.LogWarning("Email send failed: {Message}", result.ErrorMessage);
        else
            _logger.LogInformation("Email sent to {To} PostmarkId={Id}", toEmail, result.PostmarkMessageId);
    }

    private static string ToHtmlEmail(string plain)
    {
        var esc = WebUtility.HtmlEncode(plain);
        return $"<html><body><p style=\"font-family:sans-serif;font-size:14px;line-height:1.5;\">{esc.Replace("\n", "<br/>", StringComparison.Ordinal)}</p></body></html>";
    }
}

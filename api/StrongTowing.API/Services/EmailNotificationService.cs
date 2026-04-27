using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.Abstractions;
using StrongTowing.Application.EmailTemplates;
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
    private readonly IEmailEventTemplateService _emailEventTemplateService;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        IEncryptionService encryption,
        IEmailEventTemplateService emailEventTemplateService,
        ILogger<EmailNotificationService> logger)
    {
        _db = db;
        _userManager = userManager;
        _emailSender = emailSender;
        _encryption = encryption;
        _emailEventTemplateService = emailEventTemplateService;
        _logger = logger;
    }

    public Task NotifyDriverJobAssignedAsync(int jobId, string pickupSummary, string driverUserId, CancellationToken cancellationToken = default) =>
        SendDriverAsync(
            driverUserId,
            s => s.EmailDriverJobAssigned,
            EmailEventKeys.DriverJobAssigned,
            new Dictionary<string, string>
            {
                ["JobId"] = jobId.ToString(),
                ["PickupSummary"] = pickupSummary
            },
            cancellationToken);

    public Task NotifyDriverJobCompletedAsync(int jobId, string driverUserId, CancellationToken cancellationToken = default) =>
        SendDriverAsync(
            driverUserId,
            s => s.EmailDriverJobCompleted,
            EmailEventKeys.DriverJobCompleted,
            new Dictionary<string, string> { ["JobId"] = jobId.ToString() },
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
            EmailEventKeys.DriverPayrollPaid,
            new Dictionary<string, string>
            {
                ["PayPeriodStart"] = payPeriodStart.ToString("MM/dd/yyyy"),
                ["PayPeriodEnd"] = payPeriodEnd.ToString("MM/dd/yyyy"),
                ["NetPay"] = netPay.ToString("0.00")
            },
            cancellationToken);

    public Task NotifyClientJobCreatedAsync(int jobId, string? contactEmail, string? ownerEmail, CancellationToken cancellationToken = default) =>
        SendClientAsync(
            contactEmail,
            ownerEmail,
            s => s.EmailClientJobCreated,
            EmailEventKeys.ClientJobCreated,
            new Dictionary<string, string> { ["JobId"] = jobId.ToString() },
            cancellationToken);

    public Task NotifyClientFraudUnderReviewAsync(int jobId, string? contactEmail, string? ownerEmail, CancellationToken cancellationToken = default) =>
        SendClientAsync(
            contactEmail,
            ownerEmail,
            s => s.EmailClientFraudUnderReview,
            EmailEventKeys.ClientFraudUnderReview,
            new Dictionary<string, string> { ["JobId"] = jobId.ToString() },
            cancellationToken);

    public Task NotifyClientDriverAssignedAsync(int jobId, string? contactEmail, string? ownerEmail, CancellationToken cancellationToken = default) =>
        SendClientAsync(
            contactEmail,
            ownerEmail,
            s => s.EmailClientDriverAssigned,
            EmailEventKeys.ClientDriverAssigned,
            new Dictionary<string, string> { ["JobId"] = jobId.ToString() },
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

        (Func<SystemSettings, bool> toggle, string eventKey) = status switch
        {
            JobStatus.OnRoute => ((Func<SystemSettings, bool>)(s => s.EmailClientStatusOnRoute), EmailEventKeys.ClientStatusOnRoute),
            JobStatus.OnScene => (s => s.EmailClientStatusOnScene, EmailEventKeys.ClientStatusOnScene),
            JobStatus.Loaded => (s => s.EmailClientStatusLoaded, EmailEventKeys.ClientStatusLoaded),
            _ => ((Func<SystemSettings, bool>)(_ => false), EmailEventKeys.ClientStatusOnRoute)
        };

        return SendClientAsync(
            contactEmail,
            ownerEmail,
            toggle,
            eventKey,
            new Dictionary<string, string> { ["JobId"] = jobId.ToString() },
            cancellationToken);
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
            EmailEventKeys.ClientPaymentLinkCreated,
            new Dictionary<string, string>
            {
                ["JobId"] = jobId.ToString(),
                ["Amount"] = $"${amount:0.00}",
                ["LinkUrl"] = linkUrl
            },
            cancellationToken);

    public Task NotifyClientPaymentSucceededAsync(
        int jobId,
        decimal? amount,
        string? contactEmail,
        string? ownerEmail,
        CancellationToken cancellationToken = default)
    {
        var amountLine = amount.HasValue ? $" of <strong>${amount.Value:0.00}</strong>" : string.Empty;
        return SendClientAsync(
            contactEmail,
            ownerEmail,
            s => s.EmailClientPaymentSucceeded,
            EmailEventKeys.ClientPaymentSucceeded,
            new Dictionary<string, string>
            {
                ["JobId"] = jobId.ToString(),
                ["AmountLine"] = amountLine
            },
            cancellationToken);
    }

    public Task NotifyClientPaymentFailedAsync(int jobId, string? contactEmail, string? ownerEmail, CancellationToken cancellationToken = default) =>
        SendClientAsync(
            contactEmail,
            ownerEmail,
            s => s.EmailClientPaymentFailed,
            EmailEventKeys.ClientPaymentFailed,
            new Dictionary<string, string> { ["JobId"] = jobId.ToString() },
            cancellationToken);

    public Task NotifyClientJobCancelledAsync(
        int jobId,
        decimal? feeAmount,
        string? contactEmail,
        string? ownerEmail,
        CancellationToken cancellationToken = default)
    {
        var feeSentence = feeAmount is > 0
            ? $" A cancellation fee of <strong>${feeAmount:0.00}</strong> may apply."
            : string.Empty;
        return SendClientAsync(
            contactEmail,
            ownerEmail,
            s => s.EmailClientJobCancelled,
            EmailEventKeys.ClientJobCancelled,
            new Dictionary<string, string>
            {
                ["JobId"] = jobId.ToString(),
                ["FeeSentence"] = feeSentence
            },
            cancellationToken);
    }

    public Task NotifyClientJobCompletedAsync(int jobId, string? contactEmail, string? ownerEmail, CancellationToken cancellationToken = default) =>
        SendClientAsync(
            contactEmail,
            ownerEmail,
            s => s.EmailClientJobCompleted,
            EmailEventKeys.ClientJobCompleted,
            new Dictionary<string, string> { ["JobId"] = jobId.ToString() },
            cancellationToken);

    private async Task SendDriverAsync(
        string driverUserId,
        Func<SystemSettings, bool> toggle,
        string eventKey,
        IReadOnlyDictionary<string, string> merge,
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

        var rendered = await _emailEventTemplateService.RenderAsync(eventKey, merge, cancellationToken).ConfigureAwait(false);
        await DispatchSendAsync(settings, email, rendered, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendClientAsync(
        string? contactEmail,
        string? ownerEmail,
        Func<SystemSettings, bool> toggle,
        string eventKey,
        IReadOnlyDictionary<string, string> merge,
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

        var rendered = await _emailEventTemplateService.RenderAsync(eventKey, merge, cancellationToken).ConfigureAwait(false);
        await DispatchSendAsync(settings, email, rendered, cancellationToken).ConfigureAwait(false);
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

    private async Task DispatchSendAsync(SystemSettings settings, string toEmail, EmailRenderResult rendered, CancellationToken cancellationToken)
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

        var stream = string.IsNullOrWhiteSpace(settings.PostmarkMessageStream) ? null : settings.PostmarkMessageStream;

        var result = await _emailSender.SendAsync(
            token,
            settings.PostmarkDefaultFromEmail!,
            stream,
            toEmail,
            rendered.Subject,
            rendered.HtmlBody,
            rendered.PlainTextBody,
            cancellationToken).ConfigureAwait(false);

        if (!result.Success)
            _logger.LogWarning("Email send failed: {Message}", result.ErrorMessage);
        else
            _logger.LogInformation("Email sent to {To} PostmarkId={Id}", toEmail, result.PostmarkMessageId);
    }
}

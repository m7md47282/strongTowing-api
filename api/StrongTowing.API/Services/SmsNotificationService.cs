using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.Abstractions;
using StrongTowing.Core.Entities;
using StrongTowing.Core.Enums;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.API.Services;

public sealed class SmsNotificationService : ISmsNotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISmsSender _smsSender;
    private readonly IEncryptionService _encryption;
    private readonly ILogger<SmsNotificationService> _logger;

    public SmsNotificationService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ISmsSender smsSender,
        IEncryptionService encryption,
        ILogger<SmsNotificationService> logger)
    {
        _db = db;
        _userManager = userManager;
        _smsSender = smsSender;
        _encryption = encryption;
        _logger = logger;
    }

    public Task NotifyDriverJobAssignedAsync(int jobId, string pickupSummary, string driverUserId, CancellationToken cancellationToken = default) =>
        SendDriverAsync(
            driverUserId,
            s => s.SmsDriverJobAssigned,
            $"Strong Towing: New job #{jobId} — {pickupSummary}. Open the driver app for details.",
            cancellationToken);

    public Task NotifyDriverJobCompletedAsync(int jobId, string driverUserId, CancellationToken cancellationToken = default) =>
        SendDriverAsync(
            driverUserId,
            s => s.SmsDriverJobCompleted,
            $"Strong Towing: Job #{jobId} marked completed.",
            cancellationToken);

    public Task NotifyDriverPayrollPaidAsync(
        string driverUserId,
        DateTime payPeriodStart,
        DateTime payPeriodEnd,
        decimal netPay,
        CancellationToken cancellationToken = default) =>
        SendDriverAsync(
            driverUserId,
            s => s.SmsDriverPayrollPaid,
            $"Strong Towing: Payroll {payPeriodStart:MM/dd/yyyy}–{payPeriodEnd:MM/dd/yyyy} marked paid. Net: ${netPay:0.00}.",
            cancellationToken);

    public Task NotifyClientJobCreatedAsync(int jobId, string? contactPhone, string? ownerPhone, CancellationToken cancellationToken = default) =>
        SendClientAsync(
            contactPhone,
            ownerPhone,
            s => s.SmsClientJobCreated,
            $"Strong Towing: We received your request. Job #{jobId}. We'll update you shortly.",
            cancellationToken);

    public Task NotifyClientFraudUnderReviewAsync(int jobId, string? contactPhone, string? ownerPhone, CancellationToken cancellationToken = default) =>
        SendClientAsync(
            contactPhone,
            ownerPhone,
            s => s.SmsClientFraudUnderReview,
            $"Strong Towing: Job #{jobId} is under review. We'll contact you shortly.",
            cancellationToken);

    public Task NotifyClientDriverAssignedAsync(int jobId, string? contactPhone, string? ownerPhone, CancellationToken cancellationToken = default) =>
        SendClientAsync(
            contactPhone,
            ownerPhone,
            s => s.SmsClientDriverAssigned,
            $"Strong Towing: A driver is assigned to job #{jobId}.",
            cancellationToken);

    public Task NotifyClientJobStatusAsync(
        int jobId,
        JobStatus status,
        string? contactPhone,
        string? ownerPhone,
        CancellationToken cancellationToken = default)
    {
        if (status is not (JobStatus.OnRoute or JobStatus.OnScene or JobStatus.Loaded))
            return Task.CompletedTask;

        Func<SystemSettings, bool> toggle = status switch
        {
            JobStatus.OnRoute => s => s.SmsClientStatusOnRoute,
            JobStatus.OnScene => s => s.SmsClientStatusOnScene,
            JobStatus.Loaded => s => s.SmsClientStatusLoaded,
            _ => s => false
        };

        var msg = status switch
        {
            JobStatus.OnRoute => $"Strong Towing: Driver en route for job #{jobId}.",
            JobStatus.OnScene => $"Strong Towing: Driver on scene for job #{jobId}.",
            JobStatus.Loaded => $"Strong Towing: Vehicle loaded for job #{jobId}.",
            _ => ""
        };

        return SendClientAsync(contactPhone, ownerPhone, toggle, msg, cancellationToken);
    }

    public Task NotifyClientPaymentLinkCreatedAsync(
        int jobId,
        decimal amount,
        string linkUrl,
        string? contactPhone,
        string? ownerPhone,
        CancellationToken cancellationToken = default) =>
        SendClientAsync(
            contactPhone,
            ownerPhone,
            s => s.SmsClientPaymentLinkCreated,
            $"Strong Towing: Pay ${amount:0.00} for job #{jobId}: {linkUrl}",
            cancellationToken);

    public Task NotifyClientPaymentSucceededAsync(
        int jobId,
        decimal? amount,
        string? contactPhone,
        string? ownerPhone,
        CancellationToken cancellationToken = default)
    {
        var amt = amount.HasValue ? $" ${amount:0.00}" : "";
        return SendClientAsync(
            contactPhone,
            ownerPhone,
            s => s.SmsClientPaymentSucceeded,
            $"Strong Towing: Payment{amt} received for job #{jobId}. Thank you.",
            cancellationToken);
    }

    public Task NotifyClientPaymentFailedAsync(int jobId, string? contactPhone, string? ownerPhone, CancellationToken cancellationToken = default) =>
        SendClientAsync(
            contactPhone,
            ownerPhone,
            s => s.SmsClientPaymentFailed,
            $"Strong Towing: Payment could not be completed for job #{jobId}. Please try again or contact us.",
            cancellationToken);

    public Task NotifyClientJobCancelledAsync(
        int jobId,
        decimal? feeAmount,
        string? contactPhone,
        string? ownerPhone,
        CancellationToken cancellationToken = default)
    {
        var fee = feeAmount is > 0
            ? $" A cancellation fee of ${feeAmount:0.00} may apply."
            : "";
        return SendClientAsync(
            contactPhone,
            ownerPhone,
            s => s.SmsClientJobCancelled,
            $"Strong Towing: Job #{jobId} was cancelled.{fee}",
            cancellationToken);
    }

    public Task NotifyClientJobCompletedAsync(int jobId, string? contactPhone, string? ownerPhone, CancellationToken cancellationToken = default) =>
        SendClientAsync(
            contactPhone,
            ownerPhone,
            s => s.SmsClientJobCompleted,
            $"Strong Towing: Job #{jobId} is completed. Thank you.",
            cancellationToken);

    private async Task SendDriverAsync(
        string driverUserId,
        Func<SystemSettings, bool> toggle,
        string body,
        CancellationToken cancellationToken)
    {
        var settings = await _db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (settings is not { SmsEnabled: true } || !toggle(settings))
            return;

        if (!CanSend(settings))
            return;

        var user = await _userManager.FindByIdAsync(driverUserId).ConfigureAwait(false);
        var phone = SmsPhoneNormalizer.ToE164Us(user?.PhoneNumber);
        if (phone == null)
        {
            _logger.LogDebug("SMS skipped: no driver phone for user {UserId}", driverUserId);
            return;
        }

        await DispatchSendAsync(settings, phone, body, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendClientAsync(
        string? contactPhone,
        string? ownerPhone,
        Func<SystemSettings, bool> toggle,
        string body,
        CancellationToken cancellationToken)
    {
        var settings = await _db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (settings is not { SmsEnabled: true } || !toggle(settings))
            return;

        if (!CanSend(settings))
            return;

        var phone = ResolveClientPhone(contactPhone, ownerPhone);
        if (phone == null)
        {
            _logger.LogDebug("SMS skipped: no client phone for message.");
            return;
        }

        await DispatchSendAsync(settings, phone, body, cancellationToken).ConfigureAwait(false);
    }

    private static bool CanSend(SystemSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.SmsTwilioAccountSid) || string.IsNullOrWhiteSpace(settings.SmsTwilioAuthToken))
            return false;
        var hasFrom = !string.IsNullOrWhiteSpace(settings.SmsTwilioFromNumber);
        var hasMs = !string.IsNullOrWhiteSpace(settings.SmsTwilioMessagingServiceSid);
        return hasFrom || hasMs;
    }

    private static string? ResolveClientPhone(string? contactPhone, string? ownerPhone)
    {
        var p = SmsPhoneNormalizer.ToE164Us(contactPhone);
        if (p != null)
            return p;
        return SmsPhoneNormalizer.ToE164Us(ownerPhone);
    }

    private async Task DispatchSendAsync(SystemSettings settings, string toE164, string body, CancellationToken cancellationToken)
    {
        string authToken;
        try
        {
            authToken = _encryption.Decrypt(settings.SmsTwilioAuthToken!);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMS skipped: could not decrypt Twilio auth token.");
            return;
        }

        if (string.IsNullOrWhiteSpace(authToken))
            return;

        var result = await _smsSender.SendAsync(
            settings.SmsTwilioAccountSid!,
            authToken,
            settings.SmsTwilioFromNumber,
            settings.SmsTwilioMessagingServiceSid,
            toE164,
            body,
            cancellationToken).ConfigureAwait(false);

        if (!result.Success)
            _logger.LogWarning("SMS send failed: {Message}", result.ErrorMessage);
        else
            _logger.LogInformation("SMS sent to {To} TwilioSid={Sid}", toE164, result.TwilioMessageSid);
    }
}

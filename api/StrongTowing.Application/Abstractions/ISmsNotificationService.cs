using StrongTowing.Core.Enums;

namespace StrongTowing.Application.Abstractions;

/// <summary>
/// Twilio SMS orchestration: respects system settings toggles and templates.
/// </summary>
public interface ISmsNotificationService
{
    Task NotifyDriverJobAssignedAsync(int jobId, string pickupSummary, string driverUserId, CancellationToken cancellationToken = default);

    Task NotifyDriverJobCompletedAsync(int jobId, string driverUserId, CancellationToken cancellationToken = default);

    Task NotifyDriverPayrollPaidAsync(
        string driverUserId,
        DateTime payPeriodStart,
        DateTime payPeriodEnd,
        decimal netPay,
        CancellationToken cancellationToken = default);

    Task NotifyClientJobCreatedAsync(int jobId, string? contactPhone, string? ownerPhone, CancellationToken cancellationToken = default);

    Task NotifyClientFraudUnderReviewAsync(int jobId, string? contactPhone, string? ownerPhone, CancellationToken cancellationToken = default);

    Task NotifyClientDriverAssignedAsync(int jobId, string? contactPhone, string? ownerPhone, CancellationToken cancellationToken = default);

    Task NotifyClientJobStatusAsync(
        int jobId,
        JobStatus status,
        string? contactPhone,
        string? ownerPhone,
        CancellationToken cancellationToken = default);

    Task NotifyClientPaymentLinkCreatedAsync(
        int jobId,
        decimal amount,
        string linkUrl,
        string? contactPhone,
        string? ownerPhone,
        CancellationToken cancellationToken = default);

    Task NotifyClientPaymentSucceededAsync(
        int jobId,
        decimal? amount,
        string? contactPhone,
        string? ownerPhone,
        CancellationToken cancellationToken = default);

    Task NotifyClientPaymentFailedAsync(int jobId, string? contactPhone, string? ownerPhone, CancellationToken cancellationToken = default);

    Task NotifyClientJobCancelledAsync(
        int jobId,
        decimal? feeAmount,
        string? contactPhone,
        string? ownerPhone,
        CancellationToken cancellationToken = default);

    Task NotifyClientJobCompletedAsync(int jobId, string? contactPhone, string? ownerPhone, CancellationToken cancellationToken = default);
}

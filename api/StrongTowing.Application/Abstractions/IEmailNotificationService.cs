using StrongTowing.Core.Enums;

namespace StrongTowing.Application.Abstractions;

/// <summary>
/// Postmark email orchestration: respects system settings toggles (parallel to SMS).
/// </summary>
public interface IEmailNotificationService
{
    Task NotifyDriverJobAssignedAsync(int jobId, string pickupSummary, string driverUserId, CancellationToken cancellationToken = default);

    Task NotifyDriverJobCompletedAsync(int jobId, string driverUserId, CancellationToken cancellationToken = default);

    Task NotifyDriverPayrollPaidAsync(
        string driverUserId,
        DateTime payPeriodStart,
        DateTime payPeriodEnd,
        decimal netPay,
        CancellationToken cancellationToken = default);

    Task NotifyClientJobCreatedAsync(int jobId, string? contactEmail, string? ownerEmail, CancellationToken cancellationToken = default);

    Task NotifyClientFraudUnderReviewAsync(int jobId, string? contactEmail, string? ownerEmail, CancellationToken cancellationToken = default);

    Task NotifyClientDriverAssignedAsync(int jobId, string? contactEmail, string? ownerEmail, CancellationToken cancellationToken = default);

    Task NotifyClientJobStatusAsync(
        int jobId,
        JobStatus status,
        string? contactEmail,
        string? ownerEmail,
        CancellationToken cancellationToken = default);

    Task NotifyClientPaymentLinkCreatedAsync(
        int jobId,
        decimal amount,
        string linkUrl,
        string? contactEmail,
        string? ownerEmail,
        CancellationToken cancellationToken = default);

    Task NotifyClientPaymentSucceededAsync(
        int jobId,
        decimal? amount,
        string? contactEmail,
        string? ownerEmail,
        CancellationToken cancellationToken = default);

    Task NotifyClientPaymentFailedAsync(int jobId, string? contactEmail, string? ownerEmail, CancellationToken cancellationToken = default);

    Task NotifyClientJobCancelledAsync(
        int jobId,
        decimal? feeAmount,
        string? contactEmail,
        string? ownerEmail,
        CancellationToken cancellationToken = default);

    Task NotifyClientJobCompletedAsync(int jobId, string? contactEmail, string? ownerEmail, CancellationToken cancellationToken = default);
}

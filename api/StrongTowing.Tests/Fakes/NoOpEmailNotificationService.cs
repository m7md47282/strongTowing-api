using StrongTowing.Application.Abstractions;
using StrongTowing.Core.Enums;

namespace StrongTowing.Tests.Fakes;

public sealed class NoOpEmailNotificationService : IEmailNotificationService
{
    public Task NotifyDriverJobAssignedAsync(int jobId, string pickupSummary, string driverUserId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyDriverJobCompletedAsync(int jobId, string driverUserId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyDriverPayrollPaidAsync(
        string driverUserId,
        DateTime payPeriodStart,
        DateTime payPeriodEnd,
        decimal netPay,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyClientJobCreatedAsync(int jobId, string? contactEmail, string? ownerEmail, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyClientFraudUnderReviewAsync(int jobId, string? contactEmail, string? ownerEmail, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyClientDriverAssignedAsync(int jobId, string? contactEmail, string? ownerEmail, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyClientJobStatusAsync(
        int jobId,
        JobStatus status,
        string? contactEmail,
        string? ownerEmail,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyClientPaymentLinkCreatedAsync(
        int jobId,
        decimal amount,
        string linkUrl,
        string? contactEmail,
        string? ownerEmail,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyClientPaymentSucceededAsync(
        int jobId,
        decimal? amount,
        string? contactEmail,
        string? ownerEmail,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyClientPaymentFailedAsync(int jobId, string? contactEmail, string? ownerEmail, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyClientJobCancelledAsync(
        int jobId,
        decimal? feeAmount,
        string? contactEmail,
        string? ownerEmail,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyClientJobCompletedAsync(int jobId, string? contactEmail, string? ownerEmail, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

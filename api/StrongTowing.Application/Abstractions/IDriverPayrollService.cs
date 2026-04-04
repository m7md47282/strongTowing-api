using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;

namespace StrongTowing.Application.Abstractions;

/// <summary>
/// Driver payroll snapshot generation and lifecycle. See Core constants PayrollBusinessRules for formulas.
/// </summary>
public interface IDriverPayrollService
{
    Task<GenerateDriverPayrollResponseDto> GenerateOrRefreshAsync(
        DateTime payPeriodStart,
        DateTime payPeriodEnd,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DriverPayrollAdminDto>> ListAsync(
        DateTime? filterPeriodStart,
        DateTime? filterPeriodEnd,
        string? status,
        CancellationToken cancellationToken = default);

    Task<DriverPayrollAdminDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<DriverPayrollAdminDto> FinalizeAsync(int id, string adminUserId, CancellationToken cancellationToken = default);

    Task<DriverPayrollAdminDto> MarkPaidAsync(int id, string adminUserId, CancellationToken cancellationToken = default);
}

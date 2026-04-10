using System;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.Abstractions;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Core.Constants;
using StrongTowing.Core.Entities;
using StrongTowing.Core.Enums;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.API.Services;

public class DriverPayrollService : IDriverPayrollService
{
    private readonly ApplicationDbContext _context;
    private readonly ISmsNotificationService _smsNotificationService;

    public DriverPayrollService(ApplicationDbContext context, ISmsNotificationService smsNotificationService)
    {
        _context = context;
        _smsNotificationService = smsNotificationService;
    }

    public async Task<GenerateDriverPayrollResponseDto> GenerateOrRefreshAsync(
        DateTime payPeriodStart,
        DateTime payPeriodEnd,
        CancellationToken cancellationToken = default)
    {
        var (periodStartNorm, periodEndNorm, endExclusive) = NormalizePeriod(payPeriodStart, payPeriodEnd);

        var settings = await _context.SystemSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken)
            ?? new SystemSettings();
        var commissionPct = settings.DriverCommissionPercentage;

        var jobs = await _context.Jobs
            .AsNoTracking()
            .Where(j => j.Status == JobStatus.Completed)
            .Where(j => j.DriverId != null && j.CompletedAt != null)
            .Where(j => j.CompletedAt >= periodStartNorm && j.CompletedAt < endExclusive)
            .ToListAsync(cancellationToken);

        var aggregates = jobs
            .GroupBy(j => j.DriverId!)
            .Select(g => new
            {
                DriverId = g.Key,
                TotalJobs = g.Count(),
                TotalJobRevenue = g.Sum(x => x.Cost),
                TotalJobMinutes = g.Sum(x => JobMinutes(x)),
                CashCollections = g.Sum(CashDeductionForJob)
            })
            .ToList();

        var upserted = 0;

        foreach (var agg in aggregates)
        {
            var existing = await _context.DriverPayrolls
                .FirstOrDefaultAsync(
                    d => d.DriverId == agg.DriverId
                         && d.PayPeriodStart == periodStartNorm
                         && d.PayPeriodEnd == periodEndNorm,
                    cancellationToken);

            if (existing != null && existing.Status != DriverPayrollStatuses.Draft)
            {
                continue;
            }

            var gross = RoundMoney(agg.TotalJobRevenue * (commissionPct / 100m));
            var net = RoundMoney(gross - agg.CashCollections);

            if (existing == null)
            {
                existing = new DriverPayroll
                {
                    DriverId = agg.DriverId,
                    PayPeriodStart = periodStartNorm,
                    PayPeriodEnd = periodEndNorm,
                    Status = DriverPayrollStatuses.Draft,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.DriverPayrolls.Add(existing);
            }

            existing.TotalJobs = agg.TotalJobs;
            existing.TotalJobRevenue = agg.TotalJobRevenue;
            existing.TotalJobMinutes = agg.TotalJobMinutes;
            existing.CommissionPercentage = commissionPct;
            existing.GrossEarnings = gross;
            existing.CashCollections = agg.CashCollections;
            existing.NetPay = net;
            existing.UpdatedAt = DateTime.UtcNow;

            upserted++;
        }

        // Remove draft rows for this period with no completed jobs (driver dropped out of aggregate)
        var driverIds = aggregates.Select(a => a.DriverId).ToHashSet(StringComparer.Ordinal);
        var orphanDrafts = await _context.DriverPayrolls
            .Where(d =>
                d.PayPeriodStart == periodStartNorm
                && d.PayPeriodEnd == periodEndNorm
                && d.Status == DriverPayrollStatuses.Draft
                && !driverIds.Contains(d.DriverId))
            .ToListAsync(cancellationToken);
        if (orphanDrafts.Count > 0)
        {
            _context.DriverPayrolls.RemoveRange(orphanDrafts);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var allRows = await QueryForPeriod(periodStartNorm, periodEndNorm, cancellationToken);
        return new GenerateDriverPayrollResponseDto
        {
            PayPeriodStart = periodStartNorm,
            PayPeriodEnd = periodEndNorm,
            RowsUpserted = upserted,
            Rows = allRows
        };
    }

    public async Task<IReadOnlyList<DriverPayrollAdminDto>> ListAsync(
        DateTime? filterPeriodStart,
        DateTime? filterPeriodEnd,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DriverPayrolls
            .AsNoTracking()
            .Include(d => d.Driver)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(d => d.Status == status);
        }

        if (filterPeriodStart.HasValue && filterPeriodEnd.HasValue)
        {
            var fs = DateTime.SpecifyKind(filterPeriodStart.Value.Date, DateTimeKind.Utc);
            var fe = DateTime.SpecifyKind(filterPeriodEnd.Value.Date, DateTimeKind.Utc);
            // Overlap: payroll [ps, pe] vs filter [fs, fe]
            query = query.Where(d => d.PayPeriodStart <= fe && d.PayPeriodEnd >= fs);
        }
        else if (filterPeriodStart.HasValue)
        {
            var fs = DateTime.SpecifyKind(filterPeriodStart.Value.Date, DateTimeKind.Utc);
            query = query.Where(d => d.PayPeriodEnd >= fs);
        }
        else if (filterPeriodEnd.HasValue)
        {
            var fe = DateTime.SpecifyKind(filterPeriodEnd.Value.Date, DateTimeKind.Utc);
            query = query.Where(d => d.PayPeriodStart <= fe);
        }

        // Order only by columns on DriverPayrolls (+ DriverId) so SQL does not depend on JOIN sort by User.FullName
        // (avoids provider-specific failures; display order by name applied after map).
        var list = await query
            .OrderByDescending(d => d.PayPeriodEnd)
            .ThenBy(d => d.DriverId)
            .Take(500)
            .ToListAsync(cancellationToken);

        return list
            .Select(Map)
            .OrderByDescending(x => x.PayPeriodEnd)
            .ThenBy(x => x.DriverName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<DriverPayrollAdminDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var row = await _context.DriverPayrolls
            .AsNoTracking()
            .Include(d => d.Driver)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        return row == null ? null : Map(row);
    }

    public async Task<DriverPayrollAdminDto> FinalizeAsync(int id, string adminUserId, CancellationToken cancellationToken)
    {
        var row = await _context.DriverPayrolls.FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Payroll record not found.");

        if (row.Status != DriverPayrollStatuses.Draft)
        {
            throw new InvalidOperationException("Only draft payroll can be finalized.");
        }

        row.Status = DriverPayrollStatuses.Finalized;
        row.FinalizedAt = DateTime.UtcNow;
        row.FinalizedBy = adminUserId;
        row.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        row = await _context.DriverPayrolls
            .AsNoTracking()
            .Include(d => d.Driver)
            .FirstAsync(d => d.Id == id, cancellationToken);
        return Map(row);
    }

    public async Task<DriverPayrollAdminDto> MarkPaidAsync(int id, string adminUserId, CancellationToken cancellationToken)
    {
        var row = await _context.DriverPayrolls.FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Payroll record not found.");

        if (row.Status != DriverPayrollStatuses.Finalized)
        {
            throw new InvalidOperationException("Only finalized payroll can be marked paid.");
        }

        row.Status = DriverPayrollStatuses.Paid;
        row.PaidAt = DateTime.UtcNow;
        row.PaidBy = adminUserId;
        row.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _smsNotificationService.NotifyDriverPayrollPaidAsync(
            row.DriverId,
            row.PayPeriodStart,
            row.PayPeriodEnd,
            row.NetPay,
            cancellationToken);

        row = await _context.DriverPayrolls
            .AsNoTracking()
            .Include(d => d.Driver)
            .FirstAsync(d => d.Id == id, cancellationToken);
        return Map(row);
    }

    private async Task<List<DriverPayrollAdminDto>> QueryForPeriod(
        DateTime periodStartNorm,
        DateTime periodEndNorm,
        CancellationToken cancellationToken)
    {
        var list = await _context.DriverPayrolls
            .AsNoTracking()
            .Include(d => d.Driver)
            .Where(d => d.PayPeriodStart == periodStartNorm && d.PayPeriodEnd == periodEndNorm)
            .OrderBy(d => d.Driver!.FullName)
            .ToListAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    private static (DateTime periodStartNorm, DateTime periodEndNorm, DateTime endExclusive) NormalizePeriod(
        DateTime payPeriodStart,
        DateTime payPeriodEnd)
    {
        var ps = payPeriodStart.Date;
        var pe = payPeriodEnd.Date;
        if (pe < ps)
        {
            throw new ArgumentException("PayPeriodEnd must be on or after PayPeriodStart.");
        }

        var periodStartNorm = DateTime.SpecifyKind(ps, DateTimeKind.Utc);
        var periodEndNorm = DateTime.SpecifyKind(pe, DateTimeKind.Utc);
        var endExclusive = periodEndNorm.AddDays(1);
        return (periodStartNorm, periodEndNorm, endExclusive);
    }

    private static int JobMinutes(Job j)
    {
        if (j.CompletedAt is not { } completed || completed < j.CreatedAt)
        {
            return 0;
        }

        var minutes = (int)Math.Round((completed - j.CreatedAt).TotalMinutes);
        return minutes < 0 ? 0 : minutes;
    }

    private static decimal CashDeductionForJob(Job j)
    {
        if (j.BillingPaymentMode != JobBillingModes.CashToDriverPayroll)
        {
            return 0m;
        }

        return j.PayrollDeductionAmount ?? j.DriverCashCollectedAmount ?? 0m;
    }

    private static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static DriverPayrollAdminDto Map(DriverPayroll d)
    {
        var driver = d.Driver;
        var name = driver?.FullName;
        if (string.IsNullOrWhiteSpace(name))
        {
            name = driver?.Email ?? d.DriverId;
        }

        return new DriverPayrollAdminDto
        {
            Id = d.Id,
            DriverId = d.DriverId,
            DriverName = name ?? d.DriverId,
            DriverEmail = driver?.Email,
            PayPeriodStart = d.PayPeriodStart,
            PayPeriodEnd = d.PayPeriodEnd,
            TotalJobs = d.TotalJobs,
            TotalJobMinutes = d.TotalJobMinutes,
            TotalJobRevenue = d.TotalJobRevenue,
            CommissionPercentage = d.CommissionPercentage,
            GrossEarnings = d.GrossEarnings,
            CashCollections = d.CashCollections,
            NetPay = d.NetPay,
            Status = d.Status,
            FinalizedAt = d.FinalizedAt,
            FinalizedBy = d.FinalizedBy,
            PaidAt = d.PaidAt,
            PaidBy = d.PaidBy,
            Notes = d.Notes,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        };
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Core.Constants;
using StrongTowing.Infrastructure.Data;
using System.Text;

namespace StrongTowing.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(ApplicationDbContext context, ILogger<ReportsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get financial summary report (Admin only)
    /// </summary>
    [HttpGet("financial")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
    public async Task<ActionResult<FinancialSummaryResponse>> GetFinancialSummary(
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        try
        {
            var summary = await BuildFinancialSummaryAsync(startDate, endDate);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating financial report");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while generating financial report." });
        }
    }

    /// <summary>
    /// Export financial report to CSV (Admin only)
    /// </summary>
    [HttpGet("export")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
    public async Task<IActionResult> ExportReport(
        [FromQuery] string type,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        if (!string.Equals(type, "csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                error = "Bad Request",
                message = "Only CSV export is supported in this version. Use type=csv."
            });
        }

        try
        {
            var summary = await BuildFinancialSummaryAsync(startDate, endDate);
            var csv = BuildFinancialCsv(summary);
            var bytes = Encoding.UTF8.GetBytes(csv);
            var fileName = $"financial-report-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return File(bytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting financial report");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while exporting financial report." });
        }
    }

    private async Task<FinancialSummaryResponse> BuildFinancialSummaryAsync(string? startDate, string? endDate)
    {
        DateTime? parsedStartDate = null;
        DateTime? parsedEndDate = null;

        var query = _context.Payments
            .Include(p => p.Job)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out var start))
        {
            parsedStartDate = start.Date;
            query = query.Where(p => p.CreatedAt >= parsedStartDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out var end))
        {
            parsedEndDate = end.Date;
            query = query.Where(p => p.CreatedAt < parsedEndDate.Value.AddDays(1));
        }

        var payments = await query.ToListAsync();
        var paidPayments = payments.Where(p => p.PaymentStatus == "Paid").ToList();
        var refundedPayments = payments.Where(p => p.PaymentStatus == "Refunded" || p.PaymentStatus == "PartiallyRefunded").ToList();

        var totalRevenue = paidPayments.Sum(p => p.Amount);
        var totalRefunded = refundedPayments.Sum(p => p.RefundAmount ?? 0m);
        var netRevenue = totalRevenue - totalRefunded;
        var cancellationFeeRevenue = payments
            .Where(p => p.IsCancellationFeePayment && p.PaymentStatus == "Paid")
            .Sum(p => p.CancellationFeeAmount ?? 0m);

        var paidMethodGroup = paidPayments
            .GroupBy(p => p.PaymentMethod)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount), StringComparer.OrdinalIgnoreCase);

        var relatedJobs = payments
            .Where(p => p.Job != null)
            .GroupBy(p => p.Job.Id)
            .Select(g => g.First().Job)
            .ToList();

        return new FinancialSummaryResponse
        {
            TotalRevenue = totalRevenue,
            TotalRefunded = totalRefunded,
            NetRevenue = netRevenue,
            TotalPayments = payments.Count,
            TotalJobs = relatedJobs.Count,
            AverageJobCost = relatedJobs.Count == 0 ? 0m : relatedJobs.Average(j => j.Cost),
            CancellationFeeRevenue = cancellationFeeRevenue,
            RevenueByMethod = new FinancialRevenueByMethodResponse
            {
                Card = paidMethodGroup.GetValueOrDefault("Card"),
                PaymentLink = paidMethodGroup.GetValueOrDefault("PaymentLink"),
                Cash = paidMethodGroup.GetValueOrDefault("Cash")
            },
            StatusCounts = new FinancialStatusCountsResponse
            {
                Pending = payments.Count(p => p.PaymentStatus == "Pending"),
                Paid = payments.Count(p => p.PaymentStatus == "Paid"),
                Failed = payments.Count(p => p.PaymentStatus == "Failed"),
                Refunded = payments.Count(p => p.PaymentStatus == "Refunded"),
                PartiallyRefunded = payments.Count(p => p.PaymentStatus == "PartiallyRefunded"),
                UnderReview = payments.Count(p => p.PaymentStatus == "UnderReview"),
                Authorized = payments.Count(p => p.PaymentStatus == "Authorized" || p.PaymentStatus == "CapturePending"),
                Cancelled = payments.Count(p => p.PaymentStatus == "Cancelled")
            },
            Period = new FinancialReportPeriodResponse
            {
                StartDate = parsedStartDate,
                EndDate = parsedEndDate
            }
        };
    }

    private static string BuildFinancialCsv(FinancialSummaryResponse summary)
    {
        var sb = new StringBuilder();

        sb.AppendLine("metric,value");
        sb.AppendLine($"period_start,{FormatDate(summary.Period.StartDate)}");
        sb.AppendLine($"period_end,{FormatDate(summary.Period.EndDate)}");
        sb.AppendLine($"total_revenue,{summary.TotalRevenue}");
        sb.AppendLine($"total_refunded,{summary.TotalRefunded}");
        sb.AppendLine($"net_revenue,{summary.NetRevenue}");
        sb.AppendLine($"total_payments,{summary.TotalPayments}");
        sb.AppendLine($"total_jobs,{summary.TotalJobs}");
        sb.AppendLine($"average_job_cost,{summary.AverageJobCost}");
        sb.AppendLine($"cancellation_fee_revenue,{summary.CancellationFeeRevenue}");
        sb.AppendLine($"revenue_card,{summary.RevenueByMethod.Card}");
        sb.AppendLine($"revenue_payment_link,{summary.RevenueByMethod.PaymentLink}");
        sb.AppendLine($"revenue_cash,{summary.RevenueByMethod.Cash}");
        sb.AppendLine($"status_pending,{summary.StatusCounts.Pending}");
        sb.AppendLine($"status_paid,{summary.StatusCounts.Paid}");
        sb.AppendLine($"status_failed,{summary.StatusCounts.Failed}");
        sb.AppendLine($"status_refunded,{summary.StatusCounts.Refunded}");
        sb.AppendLine($"status_partially_refunded,{summary.StatusCounts.PartiallyRefunded}");
        sb.AppendLine($"status_under_review,{summary.StatusCounts.UnderReview}");
        sb.AppendLine($"status_authorized,{summary.StatusCounts.Authorized}");
        sb.AppendLine($"status_cancelled,{summary.StatusCounts.Cancelled}");

        return sb.ToString();
    }

    private static string FormatDate(DateTime? value)
    {
        return value.HasValue ? value.Value.ToString("yyyy-MM-dd") : string.Empty;
    }
}

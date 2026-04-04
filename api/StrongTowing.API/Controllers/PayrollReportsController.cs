using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StrongTowing.Application.Abstractions;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Core.Constants;
using StrongTowing.Core.Entities;
using System.Text;

namespace StrongTowing.API.Controllers;

/// <summary>
/// Admin driver payroll snapshots. Formulas: PayrollBusinessRules (commission on sum of Job.Cost, gross, cash withheld, net pay).
/// </summary>
[ApiController]
[Route("api/reports/payroll")]
[Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
public class PayrollReportsController : ControllerBase
{
    private readonly IDriverPayrollService _payrollService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PayrollReportsController> _logger;

    public PayrollReportsController(
        IDriverPayrollService payrollService,
        UserManager<ApplicationUser> userManager,
        ILogger<PayrollReportsController> logger)
    {
        _payrollService = payrollService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>List payroll snapshots (optional period overlap filter and status).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DriverPayrollAdminDto>>> List(
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        [FromQuery] string? status = null)
    {
        DateTime? fs = null;
        DateTime? fe = null;
        if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out var s))
        {
            fs = s;
        }

        if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out var e))
        {
            fe = e;
        }

        var list = await _payrollService.ListAsync(fs, fe, status);
        return Ok(list);
    }

    /// <summary>Get one payroll snapshot by id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DriverPayrollAdminDto>> GetById([FromRoute] int id)
    {
        var row = await _payrollService.GetByIdAsync(id);
        if (row == null)
        {
            return NotFound(new { error = "Not Found", message = "Payroll record not found." });
        }

        return Ok(row);
    }

    /// <summary>
    /// Generate or refresh <strong>Draft</strong> payroll rows for all drivers with completed jobs in the period.
    /// Finalized/Paid rows for a driver are not modified.
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult<GenerateDriverPayrollResponseDto>> Generate([FromBody] GenerateDriverPayrollRequest request)
    {
        try
        {
            var result = await _payrollService.GenerateOrRefreshAsync(request.PayPeriodStart, request.PayPeriodEnd);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "Bad Request", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payroll");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while generating payroll." });
        }
    }

    /// <summary>Finalize a draft payroll (locks numbers for the pay run).</summary>
    [HttpPost("{id:int}/finalize")]
    public async Task<ActionResult<DriverPayrollAdminDto>> Finalize([FromRoute] int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var row = await _payrollService.FinalizeAsync(id, userId);
            return Ok(row);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = "Bad Request", message = ex.Message });
        }
    }

    /// <summary>Mark a finalized payroll as paid.</summary>
    [HttpPost("{id:int}/mark-paid")]
    public async Task<ActionResult<DriverPayrollAdminDto>> MarkPaid([FromRoute] int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var row = await _payrollService.MarkPaidAsync(id, userId);
            return Ok(row);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = "Bad Request", message = ex.Message });
        }
    }

    /// <summary>Export payroll rows as CSV for the same filters as list.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        [FromQuery] string? status = null)
    {
        DateTime? fs = null;
        DateTime? fe = null;
        if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out var s))
        {
            fs = s;
        }

        if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out var e))
        {
            fe = e;
        }

        var list = await _payrollService.ListAsync(fs, fe, status);
        var csv = BuildPayrollCsv(list);
        var bytes = Encoding.UTF8.GetBytes(csv);
        var fileName = $"driver-payroll-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return File(bytes, "text/csv", fileName);
    }

    private static string BuildPayrollCsv(IReadOnlyList<DriverPayrollAdminDto> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "id,driver_id,driver_name,driver_email,pay_period_start,pay_period_end,total_jobs,total_job_minutes,total_job_revenue,commission_percent,gross_earnings,cash_collections,net_pay,status,finalized_at,paid_at");
        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(',',
                Csv(r.Id),
                Csv(r.DriverId),
                Csv(r.DriverName),
                Csv(r.DriverEmail ?? ""),
                Csv(r.PayPeriodStart.ToString("yyyy-MM-dd")),
                Csv(r.PayPeriodEnd.ToString("yyyy-MM-dd")),
                Csv(r.TotalJobs),
                Csv(r.TotalJobMinutes),
                Csv(r.TotalJobRevenue),
                Csv(r.CommissionPercentage),
                Csv(r.GrossEarnings),
                Csv(r.CashCollections),
                Csv(r.NetPay),
                Csv(r.Status),
                Csv(r.FinalizedAt?.ToString("o") ?? ""),
                Csv(r.PaidAt?.ToString("o") ?? "")
            ));
        }

        return sb.ToString();
    }

    private static string Csv(object? value)
    {
        var s = value?.ToString() ?? "";
        if (s.Contains('"') || s.Contains(',') || s.Contains('\n'))
        {
            return "\"" + s.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        return s;
    }
}

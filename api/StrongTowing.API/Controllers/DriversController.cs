using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Core.Constants;
using StrongTowing.Core.Entities;
using StrongTowing.Core.Enums;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DriversController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DriversController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    /// <summary>
    /// Completed-job totals and payroll history for the signed-in driver.
    /// </summary>
    [HttpGet("me/earnings")]
    [Authorize(Roles = UserRoles.Driver)]
    public async Task<ActionResult<DriverEarningsSummaryDto>> GetMyEarnings()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "Unauthorized", message = "User not authenticated." });
        }

        var completedJobs = await _context.Jobs
            .AsNoTracking()
            .Where(j => j.DriverId == userId && j.Status == JobStatus.Completed)
            .ToListAsync();

        var completedCount = completedJobs.Count;
        var totalRevenue = completedJobs.Sum(j => j.Cost);

        var payrolls = await _context.DriverPayrolls
            .AsNoTracking()
            .Where(p => p.DriverId == userId)
            .OrderByDescending(p => p.PayPeriodEnd)
            .Take(36)
            .Select(p => new DriverPayrollListItemDto
            {
                Id = p.Id,
                PayPeriodStart = p.PayPeriodStart,
                PayPeriodEnd = p.PayPeriodEnd,
                TotalJobs = p.TotalJobs,
                TotalJobRevenue = p.TotalJobRevenue,
                CommissionPercentage = p.CommissionPercentage,
                GrossEarnings = p.GrossEarnings,
                NetPay = p.NetPay,
                Status = p.Status,
                PaidAt = p.PaidAt
            })
            .ToListAsync();

        return Ok(new DriverEarningsSummaryDto
        {
            CompletedJobsCount = completedCount,
            CompletedJobsTotalRevenue = totalRevenue,
            Payrolls = payrolls
        });
    }
}

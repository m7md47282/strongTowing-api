using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTowing.API.Mapping;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Core.Constants;
using StrongTowing.Core.Enums;
using StrongTowing.Infrastructure.Data;
using System.Globalization;

namespace StrongTowing.API.Controllers;

/// <summary>
/// Aggregated admin dashboard metrics (bounded queries — avoids loading full job history).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Summary counts and lists for the admin dashboard. Pass date boundaries from the client for aligned local-day semantics.
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<AdminDashboardSummaryResponse>> GetSummary(
        [FromQuery] DateTime? monthStart = null,
        [FromQuery] DateTime? monthEnd = null,
        [FromQuery] DateTime? dayStart = null,
        [FromQuery] DateTime? dayEnd = null,
        [FromQuery] DateTime? chartPeriodStart = null,
        [FromQuery] DateTime? chartPeriodEnd = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var utcNow = DateTime.UtcNow;

            var ms = monthStart ?? new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var me = monthEnd ?? utcNow;

            var ds = dayStart ?? utcNow.Date;
            var de = dayEnd ?? utcNow.Date.AddDays(1).AddTicks(-1);

            var chartStart = chartPeriodStart?.ToUniversalTime() ?? utcNow.Date.AddDays(-6);
            var chartEnd = chartPeriodEnd?.ToUniversalTime() ?? utcNow;

            var activeRequests = await _context.Jobs.AsNoTracking()
                .CountAsync(j => j.Status != JobStatus.Completed, cancellationToken);

            var completedToday = await _context.Jobs.AsNoTracking()
                .CountAsync(j =>
                        j.Status == JobStatus.Completed &&
                        j.CompletedAt != null &&
                        j.CompletedAt >= ds &&
                        j.CompletedAt <= de,
                    cancellationToken);

            var recentEntities = await _context.Jobs.AsNoTracking()
                .Where(j => j.Status != JobStatus.Completed)
                .OrderByDescending(j => j.CreatedAt)
                .Take(10)
                .Include(j => j.Vehicle)
                    .ThenInclude(v => v.Owner)
                .Include(j => j.Driver)
                .Include(j => j.Truck)
                    .ThenInclude(t => t!.TruckType)
                .Include(j => j.StatusUpdatedBy)
                .AsSplitQuery()
                .ToListAsync(cancellationToken);

            var recentActiveJobs = recentEntities.Select(j => JobEntityMapper.MapToDto(j)).ToList();

            var monthServiceTypes = await _context.Jobs.AsNoTracking()
                .Where(j => j.CreatedAt >= ms && j.CreatedAt <= me)
                .Select(j => j.ServiceType)
                .ToListAsync(cancellationToken);

            var breakdown = CategorizeServiceTypes(monthServiceTypes);

            var chartDays = BuildDailyBuckets(chartStart, chartEnd);
            var chartFrom = chartDays.Count > 0 ? chartDays[0] : chartStart.Date;
            var chartToExclusive = chartDays.Count > 0 ? chartDays[^1].AddDays(1) : chartEnd.Date.AddDays(1);

            var countsByDay = await _context.Jobs.AsNoTracking()
                .Where(j => j.CreatedAt >= chartFrom && j.CreatedAt < chartToExclusive)
                .GroupBy(j => j.CreatedAt.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var countLookup = countsByDay.ToDictionary(
                x => x.Day.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                x => x.Count);

            var jobsCreatedLast7Days = chartDays.Select(day => new DailyJobCountDto
            {
                DateLabel = day.ToString("MMM d", CultureInfo.InvariantCulture),
                DateKey = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Count = countLookup.TryGetValue(day.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), out var cnt)
                    ? cnt
                    : 0
            }).ToList();

            return Ok(new AdminDashboardSummaryResponse
            {
                ActiveRequests = activeRequests,
                CompletedToday = completedToday,
                RecentActiveJobs = recentActiveJobs,
                ServiceBreakdown = breakdown,
                JobsCreatedLast7Days = jobsCreatedLast7Days
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building dashboard summary");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while loading dashboard summary." });
        }
    }

    private static List<DateTime> BuildDailyBuckets(DateTime chartPeriodStart, DateTime chartPeriodEnd)
    {
        var start = chartPeriodStart.Date;
        var end = chartPeriodEnd.Date;
        if (end < start)
            (start, end) = (end, start);

        var days = new List<DateTime>();
        for (var d = start; d <= end; d = d.AddDays(1))
            days.Add(d);

        return days;
    }

    /// <summary>Matches legacy Angular dashboard categorization.</summary>
    private static ServiceTypeBreakdownDto CategorizeServiceTypes(List<string?> types)
    {
        var dto = new ServiceTypeBreakdownDto { Total = types.Count };

        foreach (var raw in types)
        {
            var serviceType = (raw ?? string.Empty).ToLowerInvariant().Trim();
            if (string.IsNullOrEmpty(serviceType))
            {
                dto.Other++;
                continue;
            }

            if (serviceType.Contains("tow", StringComparison.Ordinal) || serviceType.Contains("towing", StringComparison.Ordinal))
                dto.Towing++;
            else if (serviceType.Contains("roadside", StringComparison.Ordinal)
                     || serviceType.Contains("assistance", StringComparison.Ordinal)
                     || serviceType.Contains("road side", StringComparison.Ordinal))
                dto.Roadside++;
            else if (serviceType.Contains("jump", StringComparison.Ordinal)
                     || serviceType.Contains("start", StringComparison.Ordinal)
                     || serviceType.Contains("battery", StringComparison.Ordinal))
                dto.JumpStart++;
            else if (serviceType.Contains("tire", StringComparison.Ordinal) || serviceType.Contains("flat", StringComparison.Ordinal))
                dto.TireChange++;
            else
                dto.Other++;
        }

        return dto;
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Core.Constants;
using StrongTowing.Core.Entities;
using StrongTowing.Infrastructure.Data;
using System.Security.Claims;

namespace StrongTowing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        ApplicationDbContext context,
        ILogger<SettingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get system settings (Admin/SuperAdmin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.SuperAdmin}")]
    public async Task<ActionResult<SystemSettingsDto>> GetSettings()
    {
        try
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                // Create default settings if none exist
                settings = new SystemSettings
                {
                    DriverCommissionPercentage = 30.00m,
                    PayPeriodType = "BiWeekly",
                    StripeEnabled = false,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System"
                };
                _context.SystemSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return Ok(MapToSystemSettingsDto(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settings");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while retrieving settings." });
        }
    }

    /// <summary>
    /// Update system settings (SuperAdmin only)
    /// </summary>
    [HttpPut]
    [Authorize(Roles = UserRoles.SuperAdmin)]
    public async Task<ActionResult<SystemSettingsDto>> UpdateSettings([FromBody] UpdateSystemSettingsRequest request)
    {
        try
        {
            if (request.DriverCommissionPercentage < 0 || request.DriverCommissionPercentage > 100)
            {
                return BadRequest(new { error = "Bad Request", message = "Driver commission percentage must be between 0 and 100." });
            }

            var settings = await _context.SystemSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new SystemSettings
                {
                    DriverCommissionPercentage = request.DriverCommissionPercentage,
                    PayPeriodType = "BiWeekly",
                    StripePublicKey = request.StripePublicKey,
                    StripeEnabled = request.StripeEnabled,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System"
                };
                _context.SystemSettings.Add(settings);
            }
            else
            {
                settings.DriverCommissionPercentage = request.DriverCommissionPercentage;
                settings.StripePublicKey = request.StripePublicKey;
                settings.StripeEnabled = request.StripeEnabled;
                settings.UpdatedAt = DateTime.UtcNow;
                settings.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
            }

            await _context.SaveChangesAsync();

            return Ok(MapToSystemSettingsDto(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while updating settings." });
        }
    }

    private SystemSettingsDto MapToSystemSettingsDto(SystemSettings settings)
    {
        return new SystemSettingsDto
        {
            Id = settings.Id,
            DriverCommissionPercentage = settings.DriverCommissionPercentage,
            PayPeriodType = settings.PayPeriodType,
            StripePublicKey = settings.StripePublicKey,
            StripeEnabled = settings.StripeEnabled,
            UpdatedAt = settings.UpdatedAt,
            UpdatedBy = settings.UpdatedBy
        };
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Core.Constants;
using StrongTowing.Core.Entities;
using StrongTowing.Infrastructure.Data;
using StrongTowing.API.Services;
using System.Security.Claims;

namespace StrongTowing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        ILogger<SettingsController> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
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
                settings = new SystemSettings
                {
                    DriverCommissionPercentage = 30.00m,
                    PayPeriodType = "BiWeekly",
                    StripeEnabled = false,
                    StripeMode = "test",
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

            if (!string.IsNullOrWhiteSpace(request.StripeMode) &&
                !request.StripeMode.Equals("test", StringComparison.OrdinalIgnoreCase) &&
                !request.StripeMode.Equals("live", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "Bad Request", message = "Stripe mode must be either 'test' or 'live'." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new SystemSettings
                {
                    PayPeriodType = "BiWeekly",
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = userId
                };
                _context.SystemSettings.Add(settings);
            }

            settings.DriverCommissionPercentage = request.DriverCommissionPercentage;
            settings.StripePublicKey = request.StripePublicKey;
            settings.StripeEnabled = request.StripeEnabled;
            settings.StripeMode = request.StripeMode?.Equals("live", StringComparison.OrdinalIgnoreCase) == true ? "live" : "test";
            settings.UpdatedAt = DateTime.UtcNow;
            settings.UpdatedBy = userId;

            // Legacy single-key support
            if (!string.IsNullOrWhiteSpace(request.StripeSecretKey))
            {
                settings.StripeSecretKey = _encryptionService.Encrypt(request.StripeSecretKey);
            }

            if (!string.IsNullOrWhiteSpace(request.StripeWebhookSecret))
            {
                settings.StripeWebhookSecret = _encryptionService.Encrypt(request.StripeWebhookSecret);
            }

            // Test-mode keys
            if (request.StripeTestPublicKey != null)
            {
                settings.StripeTestPublicKey = request.StripeTestPublicKey;
            }

            if (!string.IsNullOrWhiteSpace(request.StripeTestSecretKey))
            {
                settings.StripeTestSecretKey = _encryptionService.Encrypt(request.StripeTestSecretKey);
            }

            if (!string.IsNullOrWhiteSpace(request.StripeTestWebhookSecret))
            {
                settings.StripeTestWebhookSecret = _encryptionService.Encrypt(request.StripeTestWebhookSecret);
            }

            // Live-mode keys
            if (request.StripeLivePublicKey != null)
            {
                settings.StripeLivePublicKey = request.StripeLivePublicKey;
            }

            if (!string.IsNullOrWhiteSpace(request.StripeLiveSecretKey))
            {
                settings.StripeLiveSecretKey = _encryptionService.Encrypt(request.StripeLiveSecretKey);
            }

            if (!string.IsNullOrWhiteSpace(request.StripeLiveWebhookSecret))
            {
                settings.StripeLiveWebhookSecret = _encryptionService.Encrypt(request.StripeLiveWebhookSecret);
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
            StripeSecretKeyConfigured = !string.IsNullOrEmpty(settings.StripeSecretKey),
            StripeWebhookConfigured = !string.IsNullOrEmpty(settings.StripeWebhookSecret),
            StripeTestPublicKey = settings.StripeTestPublicKey,
            StripeTestSecretKeyConfigured = !string.IsNullOrEmpty(settings.StripeTestSecretKey),
            StripeTestWebhookConfigured = !string.IsNullOrEmpty(settings.StripeTestWebhookSecret),
            StripeLivePublicKey = settings.StripeLivePublicKey,
            StripeLiveSecretKeyConfigured = !string.IsNullOrEmpty(settings.StripeLiveSecretKey),
            StripeLiveWebhookConfigured = !string.IsNullOrEmpty(settings.StripeLiveWebhookSecret),
            StripeMode = settings.StripeMode,
            UpdatedAt = settings.UpdatedAt,
            UpdatedBy = settings.UpdatedBy
        };
    }
}

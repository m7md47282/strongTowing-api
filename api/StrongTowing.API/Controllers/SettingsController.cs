using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Core.Constants;
using StrongTowing.Core.Entities;
using StrongTowing.Infrastructure.Data;
using StrongTowing.API.Services;
using StrongTowing.API.Options;
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
    /// Dispatch phone number for driver apps (from appsettings DispatchContact).
    /// </summary>
    [HttpGet("dispatch-contact")]
    [AllowAnonymous]
    public ActionResult<DispatchContactDto> GetDispatchContact([FromServices] IOptions<DispatchContactOptions> options)
    {
        var o = options.Value;
        return Ok(new DispatchContactDto
        {
            DisplayName = string.IsNullOrWhiteSpace(o.DisplayName) ? "Dispatch" : o.DisplayName,
            Phone = o.Phone
        });
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
                    PreAuthorizationEnabled = true,
                    PreAuthorizationMinAmount = 150.00m,
                    PreAuthorizationMaxAmount = 300.00m,
                    FraudReviewScoreThreshold = 60,
                    DuplicateRequestWindowMinutes = 30,
                    CancelFeeBeforeDispatchPercent = 0.00m,
                    CancelFeeAfterDispatchPercent = 30.00m,
                    CancelFeeAfterArrivalPercent = 50.00m,
                    DefaultPricingTaxPercent = 10.00m,
                    DefaultPricingServiceChargePercent = 0.00m,
                    DefaultPricingHookupFee = 75.00m,
                    MaxDiscountPercent = 100.00m,
                    AllowManualTotalOverride = false,
                    ManualOverrideRequiresReason = true,
                    PricingMismatchTolerance = 1.00m,
                    PricingRoundingMode = "AwayFromZero",
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

            if (request.DefaultPricingTaxPercent < 0 || request.DefaultPricingTaxPercent > 100 ||
                request.DefaultPricingServiceChargePercent < 0 || request.DefaultPricingServiceChargePercent > 100 ||
                request.MaxDiscountPercent < 0 || request.MaxDiscountPercent > 100)
            {
                return BadRequest(new { error = "Bad Request", message = "Pricing percentages must be between 0 and 100." });
            }

            if (request.DefaultPricingHookupFee < 0 || request.PricingMismatchTolerance < 0)
            {
                return BadRequest(new { error = "Bad Request", message = "Hookup fee and mismatch tolerance cannot be negative." });
            }

            if (!string.IsNullOrWhiteSpace(request.PricingRoundingMode) &&
                !string.Equals(request.PricingRoundingMode, "AwayFromZero", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.PricingRoundingMode, "ToEven", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "Bad Request", message = "Pricing rounding mode must be AwayFromZero or ToEven." });
            }

            if (!string.IsNullOrWhiteSpace(request.StripeMode) &&
                !request.StripeMode.Equals("test", StringComparison.OrdinalIgnoreCase) &&
                !request.StripeMode.Equals("live", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "Bad Request", message = "Stripe mode must be either 'test' or 'live'." });
            }

            var hasLat = request.OfficeLatitude.HasValue;
            var hasLng = request.OfficeLongitude.HasValue;
            if (hasLat != hasLng)
            {
                return BadRequest(new { error = "Bad Request", message = "Office latitude and longitude must both be set or both be cleared." });
            }

            if (hasLat && (request.OfficeLatitude is < -90 or > 90 || request.OfficeLongitude is < -180 or > 180))
            {
                return BadRequest(new { error = "Bad Request", message = "Invalid office coordinates." });
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
            settings.PreAuthorizationEnabled = request.PreAuthorizationEnabled;
            settings.PreAuthorizationMinAmount = request.PreAuthorizationMinAmount;
            settings.PreAuthorizationMaxAmount = request.PreAuthorizationMaxAmount;
            settings.FraudReviewScoreThreshold = request.FraudReviewScoreThreshold;
            settings.DuplicateRequestWindowMinutes = request.DuplicateRequestWindowMinutes;
            settings.CancelFeeBeforeDispatchPercent = request.CancelFeeBeforeDispatchPercent;
            settings.CancelFeeAfterDispatchPercent = request.CancelFeeAfterDispatchPercent;
            settings.CancelFeeAfterArrivalPercent = request.CancelFeeAfterArrivalPercent;
            settings.DefaultPricingTaxPercent = request.DefaultPricingTaxPercent;
            settings.DefaultPricingServiceChargePercent = request.DefaultPricingServiceChargePercent;
            settings.DefaultPricingHookupFee = request.DefaultPricingHookupFee;
            settings.MaxDiscountPercent = request.MaxDiscountPercent;
            settings.AllowManualTotalOverride = request.AllowManualTotalOverride;
            settings.ManualOverrideRequiresReason = request.ManualOverrideRequiresReason;
            settings.PricingMismatchTolerance = request.PricingMismatchTolerance;
            settings.PricingRoundingMode = string.IsNullOrWhiteSpace(request.PricingRoundingMode)
                ? "AwayFromZero"
                : request.PricingRoundingMode;
            settings.OfficeLatitude = request.OfficeLatitude;
            settings.OfficeLongitude = request.OfficeLongitude;
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

            settings.SmsEnabled = request.SmsEnabled;
            settings.SmsTwilioAccountSid = request.SmsTwilioAccountSid;
            if (!string.IsNullOrWhiteSpace(request.SmsTwilioAuthToken))
            {
                settings.SmsTwilioAuthToken = _encryptionService.Encrypt(request.SmsTwilioAuthToken);
            }

            settings.SmsTwilioFromNumber = request.SmsTwilioFromNumber;
            settings.SmsTwilioMessagingServiceSid = request.SmsTwilioMessagingServiceSid;

            settings.SmsDriverJobAssigned = request.SmsDriverJobAssigned;
            settings.SmsDriverJobCompleted = request.SmsDriverJobCompleted;
            settings.SmsDriverPayrollPaid = request.SmsDriverPayrollPaid;
            settings.SmsClientJobCreated = request.SmsClientJobCreated;
            settings.SmsClientFraudUnderReview = request.SmsClientFraudUnderReview;
            settings.SmsClientDriverAssigned = request.SmsClientDriverAssigned;
            settings.SmsClientStatusOnRoute = request.SmsClientStatusOnRoute;
            settings.SmsClientStatusOnScene = request.SmsClientStatusOnScene;
            settings.SmsClientStatusLoaded = request.SmsClientStatusLoaded;
            settings.SmsClientPaymentLinkCreated = request.SmsClientPaymentLinkCreated;
            settings.SmsClientPaymentSucceeded = request.SmsClientPaymentSucceeded;
            settings.SmsClientPaymentFailed = request.SmsClientPaymentFailed;
            settings.SmsClientJobCancelled = request.SmsClientJobCancelled;
            settings.SmsClientJobCompleted = request.SmsClientJobCompleted;

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
            PreAuthorizationEnabled = settings.PreAuthorizationEnabled,
            PreAuthorizationMinAmount = settings.PreAuthorizationMinAmount,
            PreAuthorizationMaxAmount = settings.PreAuthorizationMaxAmount,
            FraudReviewScoreThreshold = settings.FraudReviewScoreThreshold,
            DuplicateRequestWindowMinutes = settings.DuplicateRequestWindowMinutes,
            CancelFeeBeforeDispatchPercent = settings.CancelFeeBeforeDispatchPercent,
            CancelFeeAfterDispatchPercent = settings.CancelFeeAfterDispatchPercent,
            CancelFeeAfterArrivalPercent = settings.CancelFeeAfterArrivalPercent,
            DefaultPricingTaxPercent = settings.DefaultPricingTaxPercent,
            DefaultPricingServiceChargePercent = settings.DefaultPricingServiceChargePercent,
            DefaultPricingHookupFee = settings.DefaultPricingHookupFee,
            MaxDiscountPercent = settings.MaxDiscountPercent,
            AllowManualTotalOverride = settings.AllowManualTotalOverride,
            ManualOverrideRequiresReason = settings.ManualOverrideRequiresReason,
            PricingMismatchTolerance = settings.PricingMismatchTolerance,
            PricingRoundingMode = settings.PricingRoundingMode,
            OfficeLatitude = settings.OfficeLatitude,
            OfficeLongitude = settings.OfficeLongitude,
            SmsEnabled = settings.SmsEnabled,
            SmsTwilioAccountSid = settings.SmsTwilioAccountSid,
            SmsTwilioAuthTokenConfigured = !string.IsNullOrEmpty(settings.SmsTwilioAuthToken),
            SmsTwilioFromNumber = settings.SmsTwilioFromNumber,
            SmsTwilioMessagingServiceSid = settings.SmsTwilioMessagingServiceSid,
            SmsDriverJobAssigned = settings.SmsDriverJobAssigned,
            SmsDriverJobCompleted = settings.SmsDriverJobCompleted,
            SmsDriverPayrollPaid = settings.SmsDriverPayrollPaid,
            SmsClientJobCreated = settings.SmsClientJobCreated,
            SmsClientFraudUnderReview = settings.SmsClientFraudUnderReview,
            SmsClientDriverAssigned = settings.SmsClientDriverAssigned,
            SmsClientStatusOnRoute = settings.SmsClientStatusOnRoute,
            SmsClientStatusOnScene = settings.SmsClientStatusOnScene,
            SmsClientStatusLoaded = settings.SmsClientStatusLoaded,
            SmsClientPaymentLinkCreated = settings.SmsClientPaymentLinkCreated,
            SmsClientPaymentSucceeded = settings.SmsClientPaymentSucceeded,
            SmsClientPaymentFailed = settings.SmsClientPaymentFailed,
            SmsClientJobCancelled = settings.SmsClientJobCancelled,
            SmsClientJobCompleted = settings.SmsClientJobCompleted,
            UpdatedAt = settings.UpdatedAt,
            UpdatedBy = settings.UpdatedBy
        };
    }
}

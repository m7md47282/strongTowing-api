using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StrongTowing.API.Infrastructure;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Application.Abstractions;
using StrongTowing.Core.Constants;
using StrongTowing.Core.Entities;
using StrongTowing.Application.EmailTemplates;
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
    private readonly ISmsSender _smsSender;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<SettingsController> _logger;
    private readonly IWebHostEnvironment _environment;

    public SettingsController(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        ISmsSender smsSender,
        IEmailSender emailSender,
        ILogger<SettingsController> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _encryptionService = encryptionService;
        _smsSender = smsSender;
        _emailSender = emailSender;
        _logger = logger;
        _environment = environment;
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
                    PricingFreeMiles = 0m,
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

            if (request.DefaultPricingHookupFee < 0 || request.PricingMismatchTolerance < 0 || request.PricingFreeMiles < 0)
            {
                return BadRequest(new { error = "Bad Request", message = "Hookup fee, free miles, and mismatch tolerance cannot be negative." });
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
            settings.PricingFreeMiles = request.PricingFreeMiles;
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

            settings.EmailEnabled = request.EmailEnabled;
            if (!string.IsNullOrWhiteSpace(request.PostmarkServerToken))
            {
                settings.PostmarkServerToken = _encryptionService.Encrypt(request.PostmarkServerToken);
            }

            settings.PostmarkDefaultFromEmail = request.PostmarkDefaultFromEmail;
            settings.PostmarkMessageStream = request.PostmarkMessageStream;

            settings.EmailDriverJobAssigned = request.EmailDriverJobAssigned;
            settings.EmailDriverJobCompleted = request.EmailDriverJobCompleted;
            settings.EmailDriverPayrollPaid = request.EmailDriverPayrollPaid;
            settings.EmailClientJobCreated = request.EmailClientJobCreated;
            settings.EmailClientFraudUnderReview = request.EmailClientFraudUnderReview;
            settings.EmailClientDriverAssigned = request.EmailClientDriverAssigned;
            settings.EmailClientStatusOnRoute = request.EmailClientStatusOnRoute;
            settings.EmailClientStatusOnScene = request.EmailClientStatusOnScene;
            settings.EmailClientStatusLoaded = request.EmailClientStatusLoaded;
            settings.EmailClientPaymentLinkCreated = request.EmailClientPaymentLinkCreated;
            settings.EmailClientPaymentSucceeded = request.EmailClientPaymentSucceeded;
            settings.EmailClientPaymentFailed = request.EmailClientPaymentFailed;
            settings.EmailClientJobCancelled = request.EmailClientJobCancelled;
            settings.EmailClientJobCompleted = request.EmailClientJobCompleted;

            await _context.SaveChangesAsync();

            return Ok(MapToSystemSettingsDto(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while updating settings." });
        }
    }

    /// <summary>
    /// Send a one-off test SMS using Twilio credentials stored in system settings (SuperAdmin only).
    /// </summary>
    [HttpPost("test-sms")]
    [Authorize(Roles = UserRoles.SuperAdmin)]
    public async Task<ActionResult<TestSmsResponse>> TestSms([FromBody] TestSmsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ToPhone))
        {
            return BadRequest(new TestSmsResponse
            {
                Success = false,
                ErrorMessage = "Phone number is required."
            });
        }

        var settings = await _context.SystemSettings.AsNoTracking().FirstOrDefaultAsync();
        if (settings == null)
        {
            return BadRequest(new TestSmsResponse
            {
                Success = false,
                ErrorMessage = "System settings not found. Save Twilio credentials first."
            });
        }

        if (!settings.SmsEnabled)
        {
            return BadRequest(new TestSmsResponse
            {
                Success = false,
                ErrorMessage = "SMS is disabled in settings. Enable SMS or use this check after enabling."
            });
        }

        if (string.IsNullOrWhiteSpace(settings.SmsTwilioAccountSid) || string.IsNullOrWhiteSpace(settings.SmsTwilioAuthToken))
        {
            return BadRequest(new TestSmsResponse
            {
                Success = false,
                ErrorMessage = "Twilio Account SID and Auth Token must be saved in settings."
            });
        }

        var hasFrom = !string.IsNullOrWhiteSpace(settings.SmsTwilioFromNumber);
        var hasMs = !string.IsNullOrWhiteSpace(settings.SmsTwilioMessagingServiceSid);
        if (!hasFrom && !hasMs)
        {
            return BadRequest(new TestSmsResponse
            {
                Success = false,
                ErrorMessage = "Configure either From number (E.164) or Messaging Service SID."
            });
        }

        var toE164 = SmsPhoneNormalizer.ToE164Us(request.ToPhone.Trim());
        if (string.IsNullOrEmpty(toE164))
        {
            return BadRequest(new TestSmsResponse
            {
                Success = false,
                ErrorMessage = "Could not normalize phone number. Use 10 digits or E.164 (+1...)."
            });
        }

        string authToken;
        try
        {
            authToken = _encryptionService.Decrypt(settings.SmsTwilioAuthToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Test SMS: failed to decrypt Twilio auth token.");
            return BadRequest(new TestSmsResponse
            {
                Success = false,
                ErrorMessage = "Could not read stored Auth Token. Re-save the token in settings."
            });
        }

        if (string.IsNullOrWhiteSpace(authToken))
        {
            return BadRequest(new TestSmsResponse
            {
                Success = false,
                ErrorMessage = "Twilio Auth Token is empty after decrypt."
            });
        }

        var body = string.IsNullOrWhiteSpace(request.Message)
            ? "Strong Towing: This is a test SMS from System Settings."
            : request.Message!.Trim();
        if (body.Length > 1600)
        {
            return BadRequest(new TestSmsResponse
            {
                Success = false,
                ErrorMessage = "Message is too long (max 1600 characters)."
            });
        }

        var result = await _smsSender.SendAsync(
            settings.SmsTwilioAccountSid,
            authToken,
            settings.SmsTwilioFromNumber,
            settings.SmsTwilioMessagingServiceSid,
            toE164,
            body,
            HttpContext.RequestAborted,
            waitForDeliveryAttempt: true);

        if (!result.Success)
        {
            _logger.LogWarning(
                "Test SMS failed: Code={Code} Status={Status} Error={Error}",
                result.ErrorCode, result.Status, result.ErrorMessage);
            return Ok(new TestSmsResponse
            {
                Success = false,
                ToE164 = toE164,
                TwilioMessageSid = result.TwilioMessageSid,
                ErrorMessage = result.ErrorMessage,
                ErrorCode = result.ErrorCode,
                Status = result.Status
            });
        }

        _logger.LogInformation(
            "Test SMS sent to {To} Sid={Sid} Status={Status}",
            toE164, result.TwilioMessageSid, result.Status);
        return Ok(new TestSmsResponse
        {
            Success = true,
            ToE164 = toE164,
            TwilioMessageSid = result.TwilioMessageSid,
            Status = result.Status
        });
    }

    /// <summary>
    /// Send a one-off test email using Postmark credentials stored in system settings (SuperAdmin only).
    /// </summary>
    [HttpPost("test-email")]
    [Authorize(Roles = UserRoles.SuperAdmin)]
    public async Task<ActionResult<TestEmailResponse>> TestEmail([FromBody] TestEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ToEmail))
        {
            return BadRequest(new TestEmailResponse
            {
                Success = false,
                ErrorMessage = "Email address is required."
            });
        }

        var settings = await _context.SystemSettings.AsNoTracking().FirstOrDefaultAsync();
        if (settings == null)
        {
            return BadRequest(new TestEmailResponse
            {
                Success = false,
                ErrorMessage = "System settings not found. Save Postmark credentials first."
            });
        }

        if (!settings.EmailEnabled)
        {
            return BadRequest(new TestEmailResponse
            {
                Success = false,
                ErrorMessage = "Email is disabled in settings. Enable email or use this check after enabling."
            });
        }

        if (string.IsNullOrWhiteSpace(settings.PostmarkServerToken))
        {
            return BadRequest(new TestEmailResponse
            {
                Success = false,
                ErrorMessage = "Postmark Server API token must be saved in settings."
            });
        }

        if (string.IsNullOrWhiteSpace(settings.PostmarkDefaultFromEmail))
        {
            return BadRequest(new TestEmailResponse
            {
                Success = false,
                ErrorMessage = "Default From email must be saved in settings (verified sender in Postmark)."
            });
        }

        string serverToken;
        try
        {
            serverToken = _encryptionService.Decrypt(settings.PostmarkServerToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Test email: failed to decrypt Postmark server token.");
            return BadRequest(new TestEmailResponse
            {
                Success = false,
                ErrorMessage = "Could not read stored Postmark token. Re-save the token in settings."
            });
        }

        if (string.IsNullOrWhiteSpace(serverToken))
        {
            return BadRequest(new TestEmailResponse
            {
                Success = false,
                ErrorMessage = "Postmark token is empty after decrypt."
            });
        }

        var to = request.ToEmail.Trim();
        var subject = string.IsNullOrWhiteSpace(request.Subject)
            ? "Strong Towing: Test email from System Settings"
            : request.Subject!.Trim();
        var html = string.IsNullOrWhiteSpace(request.HtmlBody)
            ? "<html><body><p>This is a test email from Strong Towing System Settings.</p></body></html>"
            : request.HtmlBody!.Trim();

        var stream = string.IsNullOrWhiteSpace(settings.PostmarkMessageStream) ? null : settings.PostmarkMessageStream;

        var result = await _emailSender.SendAsync(
            serverToken,
            settings.PostmarkDefaultFromEmail.Trim(),
            stream,
            to,
            subject,
            html,
            null,
            HttpContext.RequestAborted);

        if (!result.Success)
        {
            _logger.LogWarning("Test email failed: {Error}", result.ErrorMessage);
            return Ok(new TestEmailResponse
            {
                Success = false,
                ToEmail = to,
                ErrorMessage = result.ErrorMessage
            });
        }

        _logger.LogInformation("Test email sent to {To} MessageId={Id}", to, result.PostmarkMessageId);
        return Ok(new TestEmailResponse
        {
            Success = true,
            ToEmail = to,
            PostmarkMessageId = result.PostmarkMessageId
        });
    }

    /// <summary>Lists all transactional email templates (defaults merged with any custom DB rows).</summary>
    [HttpGet("email-templates")]
    [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.SuperAdmin}")]
    public async Task<IActionResult> GetEmailTemplates()
    {
        try
        {
            var rows = await _context.SystemEmailTemplates.AsNoTracking().ToListAsync(HttpContext.RequestAborted);
            var byKey = rows
                .GroupBy(r => r.EventKey, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            var list = new List<EmailTemplateItemDto>(EmailTemplateDefinitions.All.Count);
            foreach (var def in EmailTemplateDefinitions.All)
            {
                byKey.TryGetValue(def.EventKey, out var row);
                var placeholders = def.Placeholders.ToList();
                if (!placeholders.Contains("LogoUrl", StringComparer.Ordinal))
                    placeholders.Add("LogoUrl");

                list.Add(new EmailTemplateItemDto
                {
                    EventKey = def.EventKey,
                    DisplayName = def.DisplayName,
                    Description = def.Description,
                    Placeholders = placeholders,
                    Subject = row is { Subject: { Length: > 0 } s } ? s : def.DefaultSubject,
                    HtmlBody = row is { HtmlBody: { Length: > 0 } h } ? h : def.DefaultInnerHtml,
                    TextBody = row?.TextBody,
                    IsCustom = row != null
                });
            }

            return Ok(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetEmailTemplates failed");
            var code = ApiErrorFormatter.StatusCodeFor(ex);
            return StatusCode(code, ApiErrorFormatter.Build(HttpContext, _environment, ex, nameof(GetEmailTemplates)));
        }
    }

    /// <summary>Creates or updates custom email template rows (inner HTML + subject; same placeholders as code defaults).</summary>
    [HttpPut("email-templates")]
    [Authorize(Roles = UserRoles.SuperAdmin)]
    public async Task<IActionResult> UpdateEmailTemplates([FromBody] UpdateEmailTemplatesRequest? request)
    {
        if (request?.Items is not { Count: > 0 })
        {
            return BadRequest(new { error = "Bad Request", message = "At least one template item is required." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
        var valid = EmailEventKeys.All.ToHashSet(StringComparer.Ordinal);
        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.EventKey) || !valid.Contains(item.EventKey!))
            {
                return BadRequest(new { error = "Bad Request", message = $"Unknown or invalid event key: {item.EventKey}" });
            }

            if (EmailTemplateDefinitions.ByKey(item.EventKey) is null)
            {
                return BadRequest(new { error = "Bad Request", message = $"No template definition for: {item.EventKey}" });
            }

            if (string.IsNullOrWhiteSpace(item.Subject) || string.IsNullOrWhiteSpace(item.HtmlBody))
            {
                return BadRequest(new { error = "Bad Request", message = "Subject and htmlBody are required for each item." });
            }
        }

        try
        {
            foreach (var item in request.Items)
            {
                var row = await _context.SystemEmailTemplates
                    .FirstOrDefaultAsync(t => t.EventKey == item.EventKey, HttpContext.RequestAborted);
                if (row == null)
                {
                    _context.SystemEmailTemplates.Add(new SystemEmailTemplate
                    {
                        EventKey = item.EventKey.Trim(),
                        Subject = item.Subject.Trim(),
                        HtmlBody = item.HtmlBody,
                        TextBody = string.IsNullOrWhiteSpace(item.TextBody) ? null : item.TextBody
                    });
                }
                else
                {
                    row.Subject = item.Subject.Trim();
                    row.HtmlBody = item.HtmlBody;
                    row.TextBody = string.IsNullOrWhiteSpace(item.TextBody) ? null : item.TextBody;
                }
            }

            var st = await _context.SystemSettings.FirstOrDefaultAsync(HttpContext.RequestAborted);
            if (st != null)
            {
                st.UpdatedAt = DateTime.UtcNow;
                st.UpdatedBy = userId;
            }

            await _context.SaveChangesAsync(HttpContext.RequestAborted);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateEmailTemplates failed");
            var code = ApiErrorFormatter.StatusCodeFor(ex);
            return StatusCode(code, ApiErrorFormatter.Build(HttpContext, _environment, ex, nameof(UpdateEmailTemplates)));
        }
    }

    /// <summary>Removes the custom row for an event so code defaults are used again.</summary>
    [HttpPost("email-templates/reset")]
    [Authorize(Roles = UserRoles.SuperAdmin)]
    public async Task<IActionResult> ResetEmailTemplate([FromBody] ResetEmailTemplateRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.EventKey))
        {
            return BadRequest(new { error = "Bad Request", message = "eventKey is required." });
        }

        if (!EmailEventKeys.All.Any(x => string.Equals(x, request.EventKey, StringComparison.Ordinal)) ||
            EmailTemplateDefinitions.ByKey(request.EventKey) is null)
        {
            return BadRequest(new { error = "Bad Request", message = "Unknown event key." });
        }

        try
        {
            var row = await _context.SystemEmailTemplates
                .FirstOrDefaultAsync(t => t.EventKey == request.EventKey, HttpContext.RequestAborted);
            if (row == null)
                return NoContent();

            _context.SystemEmailTemplates.Remove(row);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
            var st = await _context.SystemSettings.FirstOrDefaultAsync(HttpContext.RequestAborted);
            if (st != null)
            {
                st.UpdatedAt = DateTime.UtcNow;
                st.UpdatedBy = userId;
            }

            await _context.SaveChangesAsync(HttpContext.RequestAborted);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ResetEmailTemplate failed");
            var code = ApiErrorFormatter.StatusCodeFor(ex);
            return StatusCode(code, ApiErrorFormatter.Build(HttpContext, _environment, ex, nameof(ResetEmailTemplate)));
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
            PricingFreeMiles = settings.PricingFreeMiles,
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
            EmailEnabled = settings.EmailEnabled,
            PostmarkServerTokenConfigured = !string.IsNullOrEmpty(settings.PostmarkServerToken),
            PostmarkDefaultFromEmail = settings.PostmarkDefaultFromEmail,
            PostmarkMessageStream = settings.PostmarkMessageStream,
            EmailDriverJobAssigned = settings.EmailDriverJobAssigned,
            EmailDriverJobCompleted = settings.EmailDriverJobCompleted,
            EmailDriverPayrollPaid = settings.EmailDriverPayrollPaid,
            EmailClientJobCreated = settings.EmailClientJobCreated,
            EmailClientFraudUnderReview = settings.EmailClientFraudUnderReview,
            EmailClientDriverAssigned = settings.EmailClientDriverAssigned,
            EmailClientStatusOnRoute = settings.EmailClientStatusOnRoute,
            EmailClientStatusOnScene = settings.EmailClientStatusOnScene,
            EmailClientStatusLoaded = settings.EmailClientStatusLoaded,
            EmailClientPaymentLinkCreated = settings.EmailClientPaymentLinkCreated,
            EmailClientPaymentSucceeded = settings.EmailClientPaymentSucceeded,
            EmailClientPaymentFailed = settings.EmailClientPaymentFailed,
            EmailClientJobCancelled = settings.EmailClientJobCancelled,
            EmailClientJobCompleted = settings.EmailClientJobCompleted,
            UpdatedAt = settings.UpdatedAt,
            UpdatedBy = settings.UpdatedBy
        };
    }
}

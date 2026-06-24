using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.Abstractions;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.API.Services;

public sealed class StaffSmsService : IStaffSmsService
{
    private const int MaxBodyLength = 1600;

    private readonly ApplicationDbContext _db;
    private readonly ISmsSender _smsSender;
    private readonly IEncryptionService _encryption;
    private readonly ILogger<StaffSmsService> _logger;

    public StaffSmsService(
        ApplicationDbContext db,
        ISmsSender smsSender,
        IEncryptionService encryption,
        ILogger<StaffSmsService> logger)
    {
        _db = db;
        _smsSender = smsSender;
        _encryption = encryption;
        _logger = logger;
    }

    public async Task<StaffSmsResponse> SendAsync(string toPhone, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toPhone))
        {
            return Fail("Phone number is required.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return Fail("Message is required.");
        }

        body = body.Trim();
        if (body.Length > MaxBodyLength)
        {
            return Fail($"Message is too long (max {MaxBodyLength} characters).");
        }

        var settings = await _db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            return Fail("System settings not found. Configure Twilio credentials in settings.");
        }

        if (!settings.SmsEnabled)
        {
            return Fail("SMS is disabled in system settings. Enable SMS to send messages.");
        }

        if (string.IsNullOrWhiteSpace(settings.SmsTwilioAccountSid) ||
            string.IsNullOrWhiteSpace(settings.SmsTwilioAuthToken))
        {
            return Fail("Twilio Account SID and Auth Token must be saved in settings.");
        }

        var hasFrom = !string.IsNullOrWhiteSpace(settings.SmsTwilioFromNumber);
        var hasMs = !string.IsNullOrWhiteSpace(settings.SmsTwilioMessagingServiceSid);
        if (!hasFrom && !hasMs)
        {
            return Fail("Configure either Twilio From number (E.164) or Messaging Service SID.");
        }

        var toE164 = SmsPhoneNormalizer.ToE164Us(toPhone.Trim());
        if (string.IsNullOrEmpty(toE164))
        {
            return Fail("Could not normalize phone number. Use 10 digits or E.164 (+1...).");
        }

        string authToken;
        try
        {
            authToken = _encryption.Decrypt(settings.SmsTwilioAuthToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Staff SMS: failed to decrypt Twilio auth token.");
            return Fail("Could not read stored Auth Token. Re-save the token in settings.");
        }

        if (string.IsNullOrWhiteSpace(authToken))
        {
            return Fail("Twilio Auth Token is empty after decrypt.");
        }

        var result = await _smsSender.SendAsync(
            settings.SmsTwilioAccountSid,
            authToken,
            settings.SmsTwilioFromNumber,
            settings.SmsTwilioMessagingServiceSid,
            toE164,
            body,
            cancellationToken,
            waitForDeliveryAttempt: true).ConfigureAwait(false);

        if (!result.Success)
        {
            _logger.LogWarning(
                "Staff SMS failed to {To}: Code={Code} Status={Status} Error={Error}",
                toE164, result.ErrorCode, result.Status, result.ErrorMessage);
            return new StaffSmsResponse
            {
                Success = false,
                ToE164 = toE164,
                TwilioMessageSid = result.TwilioMessageSid,
                ErrorMessage = result.ErrorMessage,
                ErrorCode = result.ErrorCode,
                Status = result.Status
            };
        }

        _logger.LogInformation(
            "Staff SMS sent to {To} Sid={Sid} Status={Status}",
            toE164, result.TwilioMessageSid, result.Status);

        return new StaffSmsResponse
        {
            Success = true,
            ToE164 = toE164,
            TwilioMessageSid = result.TwilioMessageSid,
            Status = result.Status
        };
    }

    private static StaffSmsResponse Fail(string message) => new()
    {
        Success = false,
        ErrorMessage = message
    };
}

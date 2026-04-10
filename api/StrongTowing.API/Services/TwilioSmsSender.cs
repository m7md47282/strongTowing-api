using StrongTowing.Application.Abstractions;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace StrongTowing.API.Services;

public sealed class TwilioSmsSender : ISmsSender
{
    private readonly ILogger<TwilioSmsSender> _logger;

    public TwilioSmsSender(ILogger<TwilioSmsSender> logger)
    {
        _logger = logger;
    }

    public async Task<SmsSendResult> SendAsync(
        string accountSid,
        string authToken,
        string? fromE164,
        string? messagingServiceSid,
        string toE164,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken))
            return new SmsSendResult(false, "Twilio Account SID or Auth Token is not configured.");

        var hasFrom = !string.IsNullOrWhiteSpace(fromE164);
        var hasMs = !string.IsNullOrWhiteSpace(messagingServiceSid);
        if (!hasFrom && !hasMs)
            return new SmsSendResult(false, "Configure either Twilio From number or Messaging Service SID.");

        try
        {
            Twilio.TwilioClient.Init(accountSid, authToken);

            MessageResource? message;
            if (hasMs)
            {
                message = await MessageResource.CreateAsync(
                    body: body,
                    messagingServiceSid: messagingServiceSid,
                    to: new PhoneNumber(toE164)).ConfigureAwait(false);
            }
            else
            {
                message = await MessageResource.CreateAsync(
                    body: body,
                    from: new PhoneNumber(fromE164!),
                    to: new PhoneNumber(toE164)).ConfigureAwait(false);
            }

            return new SmsSendResult(true, null, message?.Sid);
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex, "Twilio API error sending SMS to {To}", toE164);
            return new SmsSendResult(false, ex.Message, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending SMS to {To}", toE164);
            return new SmsSendResult(false, ex.Message, null);
        }
    }
}

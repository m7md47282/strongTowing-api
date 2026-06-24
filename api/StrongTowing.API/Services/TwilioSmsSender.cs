using StrongTowing.Application.Abstractions;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace StrongTowing.API.Services;

public sealed class TwilioSmsSender : ISmsSender
{
    private readonly ILogger<TwilioSmsSender> _logger;

    // Tunables for delivery-attempt polling. Total worst-case wait when caller opts in:
    // MaxPolls * PollDelay ≈ 6 * 750ms = 4.5s. This is enough to catch Twilio rejecting an unverified
    // toll-free number (error 30032), which transitions the message to "failed" within ~1-3s.
    private const int MaxPolls = 6;
    private static readonly TimeSpan PollDelay = TimeSpan.FromMilliseconds(750);

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
        CancellationToken cancellationToken = default,
        bool waitForDeliveryAttempt = false)
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

            MessageResource message;
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

            var initial = EvaluateMessage(message);
            if (initial is not null)
                return initial;

            if (!waitForDeliveryAttempt || string.IsNullOrEmpty(message.Sid))
            {
                return new SmsSendResult(
                    Success: true,
                    ErrorMessage: null,
                    TwilioMessageSid: message.Sid,
                    ErrorCode: null,
                    Status: message.Status?.ToString());
            }

            // Briefly poll so the test-SMS endpoint can surface Twilio's async rejections
            // (e.g. unverified toll-free -> 30032) instead of falsely reporting success.
            for (var attempt = 0; attempt < MaxPolls; attempt++)
            {
                try
                {
                    await Task.Delay(PollDelay, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                MessageResource refreshed;
                try
                {
                    refreshed = await MessageResource.FetchAsync(pathSid: message.Sid).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Twilio poll #{Attempt} for SMS {Sid} failed; returning current state.", attempt + 1, message.Sid);
                    break;
                }

                var evaluated = EvaluateMessage(refreshed);
                if (evaluated is not null)
                    return evaluated;

                if (IsTerminalSuccess(refreshed.Status))
                {
                    return new SmsSendResult(
                        Success: true,
                        ErrorMessage: null,
                        TwilioMessageSid: refreshed.Sid,
                        ErrorCode: null,
                        Status: refreshed.Status?.ToString());
                }
            }

            // Polling ended without a terminal state — treat as queued/sending success but pass the
            // last-known status back so the caller can surface "still in flight" if desired.
            return new SmsSendResult(
                Success: true,
                ErrorMessage: null,
                TwilioMessageSid: message.Sid,
                ErrorCode: null,
                Status: message.Status?.ToString());
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex, "Twilio API error sending SMS to {To}", toE164);
            var friendly = TwilioErrorMessages.Describe(ex.Code, ex.Message);
            return new SmsSendResult(false, friendly, TwilioMessageSid: null, ErrorCode: ex.Code, Status: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending SMS to {To}", toE164);
            return new SmsSendResult(false, ex.Message);
        }
    }

    /// <summary>
    /// Returns a failed <see cref="SmsSendResult"/> when the Twilio message has reached (or already
    /// reports) a terminal failure status, otherwise null.
    /// </summary>
    private static SmsSendResult? EvaluateMessage(MessageResource message)
    {
        var status = message.Status;
        var code = message.ErrorCode;
        var rawMessage = message.ErrorMessage;

        if (IsTerminalFailure(status) || (code is not null && code != 0))
        {
            var friendly = TwilioErrorMessages.Describe(code, rawMessage, status?.ToString());
            return new SmsSendResult(
                Success: false,
                ErrorMessage: friendly,
                TwilioMessageSid: message.Sid,
                ErrorCode: code,
                Status: status?.ToString());
        }

        return null;
    }

    private static bool IsTerminalFailure(MessageResource.StatusEnum? status)
    {
        if (status is null)
            return false;
        return status == MessageResource.StatusEnum.Failed
            || status == MessageResource.StatusEnum.Undelivered
            || status == MessageResource.StatusEnum.Canceled;
    }

    private static bool IsTerminalSuccess(MessageResource.StatusEnum? status)
    {
        if (status is null)
            return false;
        return status == MessageResource.StatusEnum.Delivered
            || status == MessageResource.StatusEnum.Sent
            || status == MessageResource.StatusEnum.Read;
    }
}

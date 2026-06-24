namespace StrongTowing.API.Services;

/// <summary>
/// Maps known Twilio delivery error codes to short, actionable messages we can show to operators.
/// Codes are documented at https://www.twilio.com/docs/api/errors and the Messaging Insights page.
/// </summary>
public static class TwilioErrorMessages
{
    /// <summary>
    /// Returns a friendly explanation for a Twilio error code, falling back to the raw provider message
    /// (or a generic note) when the code is not specifically mapped.
    /// </summary>
    public static string Describe(int? errorCode, string? rawMessage, string? status = null)
    {
        var fallback = !string.IsNullOrWhiteSpace(rawMessage)
            ? rawMessage!
            : (string.IsNullOrWhiteSpace(status) ? "Twilio rejected the message." : $"Twilio status: {status}.");

        if (errorCode is null)
            return fallback;

        return errorCode.Value switch
        {
            20003 => "Twilio authentication failed (error 20003). Re-check the Account SID and Auth Token in Settings.",
            21211 => "Twilio rejected the destination number as invalid (error 21211). Confirm it is a real, dialable number in E.164 format (e.g. +15551234567).",
            21408 => "This Twilio account is not permitted to send SMS to that region (error 21408). Enable the destination country in Twilio Console → Messaging → Geo Permissions.",
            21610 => "The destination number has unsubscribed from this Twilio sender (error 21610). They must reply START before they can receive messages again.",
            21612 => "The 'From' number cannot send SMS to that destination (error 21612). Check that the From number is SMS-capable and that the destination is reachable from your account.",
            21614 => "The destination number is not a valid mobile number (error 21614). SMS can only be sent to mobile-capable numbers.",
            21660 => "The 'From' phone number does not belong to this Twilio account (error 21660). Use a phone number listed in Twilio Console → Phone Numbers, or use a Messaging Service SID.",
            30001 => "Twilio queue overflowed (error 30001). Lower the sending rate and try again.",
            30003 => "The destination handset is unreachable (error 30003). The carrier could not deliver to that device.",
            30004 => "The message was blocked by the destination carrier (error 30004). The recipient may have blocked SMS or filed a spam complaint.",
            30005 => "The destination handset is unknown (error 30005). The number may be disconnected or invalid.",
            30006 => "The destination is a landline or unreachable carrier (error 30006). Use a mobile number.",
            30007 => "The carrier flagged this message as spam (error 30007). Rephrase the content, remove links/promo language, or contact Twilio Trust Hub.",
            30008 => "Unknown carrier-side error (error 30008). Try again later or contact Twilio support if it persists.",
            30032 =>
                "Twilio toll-free verification is required for this 'From' number (error 30032). " +
                "U.S. carriers block SMS from unverified toll-free numbers. " +
                "Either: (1) submit toll-free verification in Twilio Console → Phone Numbers → Toll-Free Verification (24–72h), " +
                "(2) switch the From number to a verified 10DLC long code, or " +
                "(3) configure a Messaging Service SID that uses a verified sender pool.",
            30034 =>
                "Twilio 10DLC A2P registration is required for this 'From' number (error 30034). " +
                "Register the brand and campaign in Twilio Console → Messaging → Regulatory Compliance.",
            30036 => "Message validity period expired before delivery (error 30036). Twilio could not deliver in time.",
            30037 => "Message blocked due to invalid sender configuration (error 30037). Verify the From number is provisioned for SMS.",
            _ => $"Twilio error {errorCode.Value}: {fallback}"
        };
    }
}

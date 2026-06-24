namespace StrongTowing.Application.DTOs.Requests;

/// <summary>Request payload for sending a service quote via Twilio SMS.</summary>
public sealed class QuoteSmsRequest
{
    /// <summary>Recipient phone; normalized to E.164 on the server.</summary>
    public string ToPhone { get; set; } = "";

    /// <summary>SMS body (plain text). Built by the frontend quote summary.</summary>
    public string Message { get; set; } = "";

    /// <summary>Optional quote reference for logging.</summary>
    public string? QuoteRef { get; set; }
}

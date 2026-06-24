namespace StrongTowing.Application.DTOs.Requests;

/// <summary>Request payload for sending an invoice summary via Twilio SMS.</summary>
public sealed class InvoiceSmsRequest
{
    /// <summary>Recipient phone; normalized to E.164 on the server.</summary>
    public string ToPhone { get; set; } = "";

    /// <summary>Optional custom message. When blank, the server builds a summary from the invoice.</summary>
    public string? Message { get; set; }
}

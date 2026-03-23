namespace StrongTowing.Application.DTOs.Responses;

public class CreateOrderResponse
{
    public int JobId { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string PaymentDueMode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public int? PaymentId { get; set; }

    // Present only for PayNow (Stripe intent flow).
    public string? ClientSecret { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? PublishableKey { get; set; }
    public bool IsPreAuthorization { get; set; }
    public decimal? AuthorizedAmount { get; set; }
    public string FraudStatus { get; set; } = "Clear";
    public int? FraudScore { get; set; }
    public string? FraudReasons { get; set; }
}

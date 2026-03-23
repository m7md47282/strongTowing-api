namespace StrongTowing.Application.DTOs.Responses
{
    public class CreatePaymentIntentResponse
    {
        public string ClientSecret { get; set; } = string.Empty;
        public string PaymentIntentId { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public bool ManualCapture { get; set; }
        public decimal CapturableAmount { get; set; }
        public DateTime? AuthorizationExpiresAt { get; set; }
        public string? RiskLevel { get; set; }
    }
}

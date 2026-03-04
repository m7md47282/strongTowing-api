namespace StrongTowing.Application.DTOs.Payments
{
    public class PaymentIntentResult
    {
        /// <summary>Provider-specific transaction ID (e.g. Stripe PaymentIntent ID: pi_xxx).</summary>
        public string IntentId { get; set; } = string.Empty;

        /// <summary>Single-use secret passed to the frontend SDK to confirm the payment.</summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>Publishable/public key for the frontend SDK.</summary>
        public string PublishableKey { get; set; } = string.Empty;

        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;

        /// <summary>Provider-agnostic status: "requires_payment_method", "succeeded", "canceled", etc.</summary>
        public string Status { get; set; } = string.Empty;
    }
}

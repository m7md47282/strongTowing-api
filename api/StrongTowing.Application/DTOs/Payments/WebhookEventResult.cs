namespace StrongTowing.Application.DTOs.Payments
{
    /// <summary>
    /// Provider-agnostic representation of an inbound webhook event.
    /// The provider implementation is responsible for mapping its raw event types
    /// to the normalized <see cref="EventType"/> values below.
    /// </summary>
    public class WebhookEventResult
    {
        /// <summary>
        /// Normalized event type — one of:
        ///   "payment.succeeded", "payment.failed", "payment_link.completed",
        ///   "payment.refunded", "unknown"
        /// </summary>
        public string EventType { get; set; } = "unknown";

        /// <summary>Provider-specific transaction ID (e.g. Stripe PaymentIntent ID).</summary>
        public string? TransactionId { get; set; }

        /// <summary>Provider-specific payment link ID — set for payment_link.completed events.</summary>
        public string? PaymentLinkId { get; set; }

        /// <summary>Session/checkout ID — set for payment_link.completed events.</summary>
        public string? SessionId { get; set; }

        /// <summary>Amount paid in the currency's major unit (e.g. dollars, not cents).</summary>
        public decimal? AmountPaid { get; set; }

        /// <summary>Amount refunded in the currency's major unit.</summary>
        public decimal? AmountRefunded { get; set; }

        public string? CardLast4 { get; set; }
        public string? CardBrand { get; set; }

        /// <summary>Provider-specific charge ID (e.g. Stripe: ch_xxx).</summary>
        public string? ChargeId { get; set; }

        /// <summary>The raw event type string from the provider, for logging/debugging.</summary>
        public string? RawProviderEventType { get; set; }

        // ─── Normalized EventType constants ──────────────────────────────────

        public const string PaymentSucceeded = "payment.succeeded";
        public const string PaymentFailed = "payment.failed";
        public const string PaymentLinkCompleted = "payment_link.completed";
        public const string PaymentRefunded = "payment.refunded";
        public const string Unknown = "unknown";
    }
}

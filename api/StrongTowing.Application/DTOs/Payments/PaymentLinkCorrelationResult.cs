namespace StrongTowing.Application.DTOs.Payments
{
    /// <summary>
    /// Correlation keys resolved from a provider for a payment-intent transaction.
    /// Used to reconcile pending payment-link records when checkout session events are unavailable.
    /// </summary>
    public class PaymentLinkCorrelationResult
    {
        public string? PaymentLinkId { get; set; }
        public string? SessionId { get; set; }
        public int? JobId { get; set; }
    }
}

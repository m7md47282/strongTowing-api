namespace StrongTowing.Application.DTOs.Responses
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty; // 'Card', 'PaymentLink', 'Cash'
        public string PaymentStatus { get; set; } = string.Empty; // 'Pending', 'Paid', 'Failed', 'Refunded'
        
        // Stripe fields
        public string? StripePaymentIntentId { get; set; }
        public string? StripeChargeId { get; set; }
        public string? CardLast4 { get; set; }
        public string? CardBrand { get; set; }
        
        // Payment link
        public int? PaymentLinkId { get; set; }
        public string? StripePaymentLinkId { get; set; }
        public string? StripePaymentLinkUrl { get; set; }
        
        // Cash collection
        public string? CashCollectedBy { get; set; }
        public DateTime? CashCollectedAt { get; set; }
        
        // Processing
        public string? ProcessedBy { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? TransactionId { get; set; }
        public string? PaymentErrorMessage { get; set; }
        
        // Refund
        public DateTime? RefundedAt { get; set; }
        public string? RefundReason { get; set; }
        public decimal? RefundAmount { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }
}

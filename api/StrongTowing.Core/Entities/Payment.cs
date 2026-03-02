using System.ComponentModel.DataAnnotations.Schema;

namespace StrongTowing.Core.Entities
{
    public class Payment
    {
        public int Id { get; set; }
        
        // Job relationship
        public int JobId { get; set; }
        public Job Job { get; set; } = null!;
        
        // Payment details
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        public string PaymentMethod { get; set; } = string.Empty; // 'Card', 'PaymentLink', 'Cash'
        public string PaymentStatus { get; set; } = "Pending"; // 'Pending', 'Paid', 'Failed', 'Refunded'
        
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
        public string? CashCollectedBy { get; set; } // Driver User ID
        public DateTime? CashCollectedAt { get; set; }
        
        // Processing
        public string? ProcessedBy { get; set; } // Dispatcher User ID
        public DateTime? ProcessedAt { get; set; }
        public string? TransactionId { get; set; }
        
        // Refund
        public DateTime? RefundedAt { get; set; }
        public string? RefundReason { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? RefundAmount { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

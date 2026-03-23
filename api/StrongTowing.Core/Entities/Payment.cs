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
        public string PaymentStatus { get; set; } = "Unpaid"; // 'Unpaid', 'Pending', 'PendingCash', 'Paid', 'Failed', 'Cancelled', 'Refunded'
        public string CaptureStatus { get; set; } = "NotApplicable"; // 'NotApplicable', 'PendingAuthorization', 'Authorized', 'Captured', 'PartiallyCaptured', 'Released'
        public bool IsPreAuthorization { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? AuthorizedAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CapturedAmount { get; set; }
        public DateTime? AuthorizationExpiresAt { get; set; }
        public DateTime? CapturedAt { get; set; }
        public DateTime? ReleasedAt { get; set; }
        
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
        public string? PaymentErrorMessage { get; set; }

        // Risk/Fraud review
        public string FraudStatus { get; set; } = "Clear"; // 'Clear', 'UnderReview', 'Approved', 'Rejected'
        public int? FraudScore { get; set; }
        public string? FraudReasons { get; set; }
        public string? FraudReviewedBy { get; set; }
        public DateTime? FraudReviewedAt { get; set; }

        // Cancellation-fee tracking
        public bool IsCancellationFeePayment { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CancellationFeeAmount { get; set; }
        public string? CancellationFeePolicySnapshot { get; set; }
        
        // Refund
        public DateTime? RefundedAt { get; set; }
        public string? RefundReason { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? RefundAmount { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

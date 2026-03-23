using System.ComponentModel.DataAnnotations.Schema;

namespace StrongTowing.Core.Entities
{
    public class SystemSettings
    {
        public int Id { get; set; }
        
        // Driver commission settings
        [Column(TypeName = "decimal(5,2)")]
        public decimal DriverCommissionPercentage { get; set; } = 30.00m;
        
        // Payroll settings
        public string PayPeriodType { get; set; } = "BiWeekly"; // Fixed for now
        
        // Stripe settings
        public string? StripePublicKey { get; set; }
        public string? StripeSecretKey { get; set; } // Should be encrypted
        public string? StripeWebhookSecret { get; set; } // Should be encrypted
        public bool StripeEnabled { get; set; } = false;

        // Stripe test-mode keys
        public string? StripeTestPublicKey { get; set; }
        public string? StripeTestSecretKey { get; set; } // Stored AES-256 encrypted
        public string? StripeTestWebhookSecret { get; set; } // Stored AES-256 encrypted

        // Stripe live-mode keys
        public string? StripeLivePublicKey { get; set; }
        public string? StripeLiveSecretKey { get; set; } // Stored AES-256 encrypted
        public string? StripeLiveWebhookSecret { get; set; } // Stored AES-256 encrypted

        /// <summary>
        /// Active Stripe mode. Valid values: "test" or "live".
        /// </summary>
        public string StripeMode { get; set; } = "test";

        // Pre-authorization hold settings
        public bool PreAuthorizationEnabled { get; set; } = true;
        [Column(TypeName = "decimal(18,2)")]
        public decimal PreAuthorizationMinAmount { get; set; } = 150.00m;
        [Column(TypeName = "decimal(18,2)")]
        public decimal PreAuthorizationMaxAmount { get; set; } = 300.00m;

        // Fraud/risk settings
        public int FraudReviewScoreThreshold { get; set; } = 60;
        public int DuplicateRequestWindowMinutes { get; set; } = 30;

        // Cancellation fee matrix
        [Column(TypeName = "decimal(5,2)")]
        public decimal CancelFeeBeforeDispatchPercent { get; set; } = 0.00m;
        [Column(TypeName = "decimal(5,2)")]
        public decimal CancelFeeAfterDispatchPercent { get; set; } = 30.00m;
        [Column(TypeName = "decimal(5,2)")]
        public decimal CancelFeeAfterArrivalPercent { get; set; } = 50.00m;
        
        // Metadata
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string UpdatedBy { get; set; } = string.Empty; // Admin User ID
    }
}

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
        
        // Metadata
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string UpdatedBy { get; set; } = string.Empty; // Admin User ID
    }
}

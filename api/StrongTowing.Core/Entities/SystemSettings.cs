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
        public bool StripeEnabled { get; set; } = false;
        
        // Metadata
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string UpdatedBy { get; set; } = string.Empty; // Admin User ID
    }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace StrongTowing.Core.Entities
{
    public class DriverPayroll
    {
        public int Id { get; set; }
        
        // Driver relationship
        public string DriverId { get; set; } = string.Empty;
        public ApplicationUser Driver { get; set; } = null!;
        
        // Pay period
        public DateTime PayPeriodStart { get; set; }
        public DateTime PayPeriodEnd { get; set; }
        
        // Calculated values
        public int TotalJobs { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalJobRevenue { get; set; } = 0;
        [Column(TypeName = "decimal(5,2)")]
        public decimal CommissionPercentage { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal GrossEarnings { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal CashCollections { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal NetPay { get; set; } = 0;
        
        // Status
        public string Status { get; set; } = "Draft"; // 'Draft', 'Finalized', 'Paid'
        
        // Metadata
        public DateTime? FinalizedAt { get; set; }
        public string? FinalizedBy { get; set; } // Admin User ID
        public DateTime? PaidAt { get; set; }
        public string? PaidBy { get; set; } // Admin User ID
        public string? Notes { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

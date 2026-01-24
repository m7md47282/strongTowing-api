using System.ComponentModel.DataAnnotations.Schema;

namespace StrongTowing.Core.Entities
{
    public class CashCollection
    {
        public int Id { get; set; }
        
        // Job and Payment relationships
        public int JobId { get; set; }
        public Job Job { get; set; } = null!;
        
        public int PaymentId { get; set; }
        public Payment Payment { get; set; } = null!;
        
        // Driver who collected cash
        public string DriverId { get; set; } = string.Empty;
        public ApplicationUser Driver { get; set; } = null!;
        
        // Collection details
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
        
        // Confirmation
        public string? ConfirmedBy { get; set; } // Dispatcher User ID
        public DateTime? ConfirmedAt { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = "Pending"; // 'Pending', 'Confirmed'
        
        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

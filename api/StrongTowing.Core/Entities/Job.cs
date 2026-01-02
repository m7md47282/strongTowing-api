using System.ComponentModel.DataAnnotations.Schema;
using StrongTowing.Core.Enums;

namespace StrongTowing.Core.Entities
{
    public class Job
    {
        public int Id { get; set; }
        
        public JobStatus Status { get; set; } = JobStatus.Pending;

        // Vehicle Link
        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; } = null!;

        // Financials
        [Column(TypeName = "decimal(18,2)")]
        public decimal Cost { get; set; }
        public string? Notes { get; set; }

        // Dispatching Link
        public string? DriverId { get; set; }
        public ApplicationUser? Driver { get; set; }

        // Photos (Stored as a simple list of URLs for MVP)
        // We use a backing field or separate table usually, 
        // but for MVP, a simple List<string> wrapper or related table works.
        // Let's create a separate JobPhoto entity to be clean.
        public ICollection<JobPhoto> Photos { get; set; } = new List<JobPhoto>();

        // Status auditing
        public string? StatusUpdatedById { get; set; }
        public ApplicationUser? StatusUpdatedBy { get; set; }
        public DateTime? StatusUpdatedAt { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}



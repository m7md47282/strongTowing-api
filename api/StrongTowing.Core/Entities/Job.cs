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

        // Call/Job Type
        public string? CallType { get; set; } // "New Call" | "Completed Call" | "Schedule a call" | "Quote"
        public DateTime? ScheduledDate { get; set; }
        public TimeSpan? ScheduledTime { get; set; }
        
        // Company & Account
        public string? CompanyName { get; set; }
        public string? Account { get; set; }
        public string? CompanyOverride { get; set; }
        
        // Contact Information
        public string? ContactName { get; set; }
        public string? ContactPhoneNumber { get; set; }
        
        // Location
        public string? PickupLocation { get; set; }
        public string? DestinationAddress { get; set; }
        
        // Job Details
        public string? Reason { get; set; }
        public string? Priority { get; set; } // "Normal" | "Emergency" | "Low"
        public string? InvoiceNumber { get; set; }
        public DateTime? ETA { get; set; }
        public string? ServiceType { get; set; }
        
        // Vehicle Details (job-specific, may differ from vehicle record)
        public string? LicensePlate { get; set; }
        public string? LicenseState { get; set; }
        public string? DriveType { get; set; }
        public string? VehicleType { get; set; }
        public int? Odometer { get; set; }
        public string? Drivable { get; set; } // "Yes" | "No"
        public bool HaveKeys { get; set; }
        public string? KeyLocation { get; set; }
        
        // Assignment
        public string? DriverId { get; set; }
        public ApplicationUser? Driver { get; set; }
        public string? TruckId { get; set; }

        // Financials
        [Column(TypeName = "decimal(18,2)")]
        public decimal Cost { get; set; }
        public string? Notes { get; set; }
        public string? BillingNotes { get; set; }
        public bool IncludeBillingNotesOnReceipt { get; set; }
        
        // Invoice Charges (stored as JSON or separate table - using JSON for simplicity)
        [Column(TypeName = "nvarchar(max)")]
        public string? InvoiceChargesJson { get; set; }
        
        // Payment fields
        public string? PaymentMethod { get; set; } // 'Card', 'PaymentLink', 'Cash'
        public string PaymentStatus { get; set; } = "Unpaid"; // 'Unpaid', 'Pending', 'Paid', 'Failed'
        public int? PaymentId { get; set; }
        public Payment? Payment { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? PaidBy { get; set; } // Dispatcher/Driver User ID

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



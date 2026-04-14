using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StrongTowing.Core.Constants;
using StrongTowing.Core.Enums;

namespace StrongTowing.Core.Entities
{
    public class Job
    {
        public int Id { get; set; }
        
        public JobStatus Status { get; set; } = JobStatus.Waiting;

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

        /// <summary>When false, driver API omits commission estimate until an admin approves pricing.</summary>
        public bool CommissionVisibleToDriver { get; set; }
        public string? Notes { get; set; }
        public string? BillingNotes { get; set; }
        public bool IncludeBillingNotesOnReceipt { get; set; }
        
        // Invoice Charges (stored as JSON or separate table - using JSON for simplicity)
        [Column(TypeName = "nvarchar(max)")]
        public string? InvoiceChargesJson { get; set; }
        
        // Payment fields
        public string? PaymentMethod { get; set; } // 'Card', 'PaymentLink', 'Cash', 'Insurance', 'CashToDriverPayroll'
        public string PaymentStatus { get; set; } = "Unpaid"; // 'Unpaid', 'Pending', 'PendingCash', 'Paid', 'Failed', 'Cancelled', 'Refunded'

        /// <summary>See <see cref="JobBillingModes"/>.</summary>
        [MaxLength(64)]
        public string BillingPaymentMode { get; set; } = JobBillingModes.Standard;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? InsuranceCoveredAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ClientCoveredAmount { get; set; }

        /// <summary>Insurance portion billed / confirmed (full or split).</summary>
        public bool InsurancePortionBilled { get; set; }

        /// <summary>Client portion received (split mode).</summary>
        public bool ClientPortionPaid { get; set; }

        /// <summary>Cash the driver collected for the company (cash-to-driver flow).</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? DriverCashCollectedAmount { get; set; }

        /// <summary>Amount to withhold from driver payroll (usually equals driver cash collected).</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PayrollDeductionAmount { get; set; }

        /// <summary>Recorded for payroll processing (deduction queued or applied).</summary>
        public bool PayrollDeductionRecorded { get; set; }
        public int? PaymentId { get; set; }
        public Payment? Payment { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? PaidBy { get; set; } // Dispatcher/Driver User ID
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CancellationFeeAmount { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancelledBy { get; set; }

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



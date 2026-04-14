using StrongTowing.Application.DTOs.Requests;

namespace StrongTowing.Application.DTOs.Responses;

public class JobDto
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public int VehicleId { get; set; }
    public VehicleDto? Vehicle { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public string? ClientPhoneNumber { get; set; }
    
    // Call/Job Type
    public string? CallType { get; set; }
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
    public string? Priority { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? ETA { get; set; }
    public string? ServiceType { get; set; }
    
    // Vehicle Details (job-specific)
    public string? LicensePlate { get; set; }
    public string? LicenseState { get; set; }
    public string? DriveType { get; set; }
    public string? VehicleType { get; set; }
    public int? Odometer { get; set; }
    public string? Drivable { get; set; }
    public bool HaveKeys { get; set; }
    public string? KeyLocation { get; set; }
    
    // Assignment
    public string? DriverId { get; set; }
    public string? DriverName { get; set; }
    public string? TruckId { get; set; }
    
    // Financials
    public decimal Cost { get; set; }

    /// <summary>Populated for driver API responses: current commission rate from system settings.</summary>
    public decimal? DriverCommissionRatePercent { get; set; }

    /// <summary>Estimated commission for this job (Cost × rate / 100). Set with <see cref="DriverCommissionRatePercent"/>.</summary>
    public decimal? DriverCommissionEstimate { get; set; }

    /// <summary>When true, driver API includes commission fields; otherwise withheld until admin approves pricing.</summary>
    public bool CommissionVisibleToDriver { get; set; }

    /// <summary>Job-level payment state (synced when Stripe/cash flows update).</summary>
    public string PaymentStatus { get; set; } = "Unpaid";
    public string? PaymentMethod { get; set; }
    public DateTime? PaidAt { get; set; }

    /// <summary>Standard, InsuranceFull, SplitInsuranceClient, CashToDriverPayroll.</summary>
    public string BillingPaymentMode { get; set; } = "Standard";
    public decimal? InsuranceCoveredAmount { get; set; }
    public decimal? ClientCoveredAmount { get; set; }
    public bool InsurancePortionBilled { get; set; }
    public bool ClientPortionPaid { get; set; }
    public decimal? DriverCashCollectedAmount { get; set; }
    public decimal? PayrollDeductionAmount { get; set; }
    public bool PayrollDeductionRecorded { get; set; }
    public string? Notes { get; set; }
    public string? BillingNotes { get; set; }
    public bool IncludeBillingNotesOnReceipt { get; set; }
    public InvoiceChargesData? InvoiceCharges { get; set; }
    
    public int PhotoCount { get; set; }
    public List<JobPhotoDto> Photos { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? StatusUpdatedById { get; set; }
    public string? StatusUpdatedByName { get; set; }
    public DateTime? StatusUpdatedAt { get; set; }
}


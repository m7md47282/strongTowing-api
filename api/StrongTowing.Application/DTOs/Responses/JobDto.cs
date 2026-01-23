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
    public string? Notes { get; set; }
    public string? BillingNotes { get; set; }
    public bool IncludeBillingNotesOnReceipt { get; set; }
    public InvoiceChargesData? InvoiceCharges { get; set; }
    
    public int PhotoCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? StatusUpdatedById { get; set; }
    public string? StatusUpdatedByName { get; set; }
    public DateTime? StatusUpdatedAt { get; set; }
}


using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests;

public class CreateJobRequest
{
    // Vehicle data (if creating new vehicle)
    public VehicleData? Vehicle { get; set; }
    
    // Or use existing vehicle ID
    public int? VehicleId { get; set; }
    
    // Client data (if creating new client)
    public ClientData? Client { get; set; }
    
    // Or use existing client ID
    public string? ClientId { get; set; }
    
    // Call/Job Type
    public string? CallType { get; set; } // "New Call" | "Completed Call" | "Schedule a call" | "Quote"
    public DateTime? ScheduledDate { get; set; }
    public TimeSpan? ScheduledTime { get; set; }
    
    // Company & Account
    public string? CompanyName { get; set; }
    public string? Account { get; set; } // Account ID or name
    public string? CompanyOverride { get; set; } // Company name to show on invoice
    
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
    
    // Assignment
    public string? DriverId { get; set; }
    public string? TruckId { get; set; }
    
    // Notes
    public string? Notes { get; set; }
    public string? BillingNotes { get; set; }
    public bool IncludeBillingNotesOnReceipt { get; set; }
    
    // Invoice Charges
    public InvoiceChargesData? InvoiceCharges { get; set; }
    
    // Job data (required)
    [Required(ErrorMessage = "Cost is required")]
    [Range(0.01, 999999.99, ErrorMessage = "Cost must be between 0.01 and 999999.99")]
    public decimal Cost { get; set; }
    
    public string? DropoffLocation { get; set; } // Alias for DestinationAddress
    public string? ServiceType { get; set; }
}

public class VehicleData
{
    [Required(ErrorMessage = "VIN is required")]
    public string VIN { get; set; } = string.Empty;

    [Required(ErrorMessage = "Make is required")]
    public string Make { get; set; } = string.Empty;

    [Required(ErrorMessage = "Model is required")]
    public string Model { get; set; } = string.Empty;

    [Required(ErrorMessage = "Year is required")]
    [Range(1900, 2100, ErrorMessage = "Year must be between 1900 and 2100")]
    public int Year { get; set; }

    public string? Color { get; set; }
    
    // Additional vehicle fields
    public string? LicensePlate { get; set; }
    public string? LicenseState { get; set; }
    public string? DriveType { get; set; }
    public string? VehicleType { get; set; }
    public int? Odometer { get; set; }
    public string? Drivable { get; set; } // "Yes" | "No"
    public bool HaveKeys { get; set; }
    public string? KeyLocation { get; set; }
}

public class ClientData
{
    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    public string PhoneNumber { get; set; } = string.Empty; // Phone number is the unique identifier

    [Required(ErrorMessage = "Full name is required")]
    [MinLength(2, ErrorMessage = "Full name must be at least 2 characters")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; } // Email is now optional
    
    public string? ContactName { get; set; }
}

public class InvoiceChargesData
{
    public MileageChargeData? UnloadedEnrouteMileage { get; set; }
    public MileageChargeData? LoadedHookedMileage { get; set; }
    public MileageChargeData? DeadHeadMileage { get; set; }
    public List<ServiceItemData>? ServiceItems { get; set; }
    public decimal? Discount { get; set; }
    public decimal? DiscountPercent { get; set; }
    public bool TaxExempt { get; set; }
    public decimal? HookupFee { get; set; }
    public decimal? ServiceChargePercent { get; set; }
    public decimal? ServiceChargeAmount { get; set; }
    public decimal? TaxPercent { get; set; }
    public decimal? Subtotal { get; set; }
    public decimal? Taxes { get; set; }
    public decimal? GrandTotal { get; set; }
    public decimal? ManualTotalOverride { get; set; }
    public string? ManualOverrideReason { get; set; }
    public string? AdjustedBy { get; set; }
    public DateTime? AdjustedAt { get; set; }
    public string? AdjustmentReason { get; set; }
}

public class MileageChargeData
{
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total { get; set; }
}

public class ServiceItemData
{
    public string? ServiceName { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total { get; set; }
}



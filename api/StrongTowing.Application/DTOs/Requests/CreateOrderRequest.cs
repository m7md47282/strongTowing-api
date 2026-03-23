using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests;

public class CreateOrderRequest
{
    [Required]
    public string ServiceType { get; set; } = string.Empty;

    [Required]
    public string VehicleType { get; set; } = string.Empty;

    [Required]
    public string VehicleMake { get; set; } = string.Empty;

    public string VehicleModel { get; set; } = string.Empty;
    public int VehicleYear { get; set; } = DateTime.UtcNow.Year;
    public string VehicleColor { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public string Vin { get; set; } = string.Empty;

    [Required]
    public string PickupAddress { get; set; } = string.Empty;

    public string PickupCity { get; set; } = string.Empty;
    public string PickupState { get; set; } = string.Empty;
    public string PickupZipCode { get; set; } = string.Empty;

    public string DestinationAddress { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public string DestinationState { get; set; } = string.Empty;
    public string DestinationZipCode { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public string Priority { get; set; } = "Medium";
    public DateTime? ScheduledDate { get; set; }
    public string? Notes { get; set; }

    // Guest contact details.
    [Required]
    [Phone]
    public string ContactPhone { get; set; } = string.Empty;

    public string? ContactName { get; set; }

    [EmailAddress]
    public string? ContactEmail { get; set; }

    // Payment-related fields.
    [Range(0.01, double.MaxValue)]
    public decimal? Amount { get; set; }

    // PayNow or PayLater
    public string PaymentDueMode { get; set; } = "PayLater";

    // Card, PaymentLink, Cash (used mostly for pay-later choice previews).
    public string? PaymentMethod { get; set; }

    public string Currency { get; set; } = "usd";
}

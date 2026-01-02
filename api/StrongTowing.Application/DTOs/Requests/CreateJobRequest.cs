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
    
    // Job data
    [Required(ErrorMessage = "Cost is required")]
    [Range(0.01, 999999.99, ErrorMessage = "Cost must be between 0.01 and 999999.99")]
    public decimal Cost { get; set; }
    
    public string? Notes { get; set; }
    public string? PickupLocation { get; set; }
    public string? DropoffLocation { get; set; }
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
}

public class ClientData
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required")]
    [MinLength(2, ErrorMessage = "Full name must be at least 2 characters")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number format")]
    public string? PhoneNumber { get; set; }
}


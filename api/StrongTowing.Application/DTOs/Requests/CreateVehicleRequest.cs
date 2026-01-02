using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests;

public class CreateVehicleRequest
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
    
    [Required(ErrorMessage = "OwnerId is required")]
    public string OwnerId { get; set; } = string.Empty;
}


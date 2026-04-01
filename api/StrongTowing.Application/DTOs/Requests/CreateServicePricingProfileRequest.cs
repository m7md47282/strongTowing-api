using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests;

public class CreateServicePricingProfileRequest
{
    [Required(ErrorMessage = "Service name is required.")]
    [MaxLength(100, ErrorMessage = "Service name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Range(0, 999999.99, ErrorMessage = "Loaded price must be greater than or equal to zero.")]
    public decimal LoadedPrice { get; set; }

    [Range(0, 999999.99, ErrorMessage = "Deadhead price must be greater than or equal to zero.")]
    public decimal DeadHeadPrice { get; set; }

    public bool IsAvailable { get; set; } = true;
}

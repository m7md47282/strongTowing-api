using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests;

public class CreateServicePricingProfileRequest
{
    [Required(ErrorMessage = "Service name is required.")]
    [MaxLength(100, ErrorMessage = "Service name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Range(0, 999999.99, ErrorMessage = "Base price must be greater than or equal to zero.")]
    public decimal BasePrice { get; set; }

    [Range(0, 999999.99, ErrorMessage = "Price per mile must be greater than or equal to zero.")]
    public decimal PricePerMile { get; set; }

    public bool IsAvailable { get; set; } = true;
}

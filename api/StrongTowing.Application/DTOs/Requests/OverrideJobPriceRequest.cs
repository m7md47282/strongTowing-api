using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests;

public class OverrideJobPriceRequest
{
    [Required(ErrorMessage = "Cost is required")]
    [Range(0.01, 999999.99, ErrorMessage = "Cost must be between 0.01 and 999999.99")]
    public decimal Cost { get; set; }

    [Required(ErrorMessage = "Override reason is required.")]
    [MinLength(5, ErrorMessage = "Override reason must be at least 5 characters.")]
    public string Reason { get; set; } = string.Empty;
}

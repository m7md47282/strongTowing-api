using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests;

public class CreateTruckTypeRequest
{
    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

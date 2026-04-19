using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests;

public class CreateTruckRequest
{
    [Required]
    public int TruckTypeId { get; set; }

    [Required]
    [MaxLength(120)]
    public string UnitLabel { get; set; } = string.Empty;

    [MaxLength(32)]
    public string? LicensePlate { get; set; }

    [MaxLength(17)]
    public string? Vin { get; set; }

    [MaxLength(80)]
    public string? Make { get; set; }

    [MaxLength(80)]
    public string? Model { get; set; }

    [Range(1900, 2100)]
    public int? Year { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsOutOfService { get; set; }
}

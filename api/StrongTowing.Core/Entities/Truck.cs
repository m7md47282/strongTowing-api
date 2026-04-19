using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Core.Entities;

public class Truck
{
    public int Id { get; set; }

    public int TruckTypeId { get; set; }
    public TruckType TruckType { get; set; } = null!;

    /// <summary>Display label, e.g. "Truck A".</summary>
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

    public int? Year { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>Maintenance / broken — still assignable to jobs per business rules.</summary>
    public bool IsOutOfService { get; set; }

    /// <summary>Soft delete: inactive trucks hidden from default lists.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Job> Jobs { get; set; } = new List<Job>();
}

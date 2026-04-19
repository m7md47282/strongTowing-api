using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Core.Entities;

public class TruckType
{
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Truck> Trucks { get; set; } = new List<Truck>();
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrongTowing.Core.Entities;

public class ServicePricingProfile
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Fixed base price for the service (before mileage).</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal BasePrice { get; set; }

    /// <summary>Price per loaded mile (pickup → drop-off).</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerMile { get; set; }

    public bool IsAvailable { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

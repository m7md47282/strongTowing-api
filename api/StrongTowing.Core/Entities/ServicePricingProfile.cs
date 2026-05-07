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

    /// <summary>Price per loaded mile (pickup → drop-off). Legacy fallback when <see cref="LoadedPricePerMile"/> is null.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerMile { get; set; }

    /// <summary>Optional $/mi for office → pickup (unloaded enroute). When null, enroute is not billed from this profile.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? EnroutePricePerMile { get; set; }

    /// <summary>Optional explicit loaded $/mi; when null, <see cref="PricePerMile"/> is used.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? LoadedPricePerMile { get; set; }

    /// <summary>Optional $/mi for drop-off → office (deadhead). When null, deadhead is not billed from this profile.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? DeadheadPricePerMile { get; set; }

    public bool IsAvailable { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrongTowing.Core.Entities;

/// <summary>Per-account overrides for a specific service (motor club / insurance contract pricing).</summary>
public class InsuranceAccountServiceRate
{
    public int Id { get; set; }

    public int InsuranceAccountId { get; set; }
    public InsuranceAccount InsuranceAccount { get; set; } = null!;

    public int ServicePricingProfileId { get; set; }
    public ServicePricingProfile ServicePricingProfile { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal BasePrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerMile { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

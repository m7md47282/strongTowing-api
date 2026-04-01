using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrongTowing.Core.Entities;

public class InsuranceAccount
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? AccountNumber { get; set; }

    [MaxLength(200)]
    public string? ContactName { get; set; }

    [MaxLength(256)]
    public string? BillingEmail { get; set; }

    [MaxLength(30)]
    public string? BillingPhone { get; set; }

    [MaxLength(300)]
    public string? AddressLine1 { get; set; }

    [MaxLength(300)]
    public string? AddressLine2 { get; set; }

    [MaxLength(120)]
    public string? City { get; set; }

    [MaxLength(80)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    [Column(TypeName = "decimal(18,2)")]
    public decimal HookupFee { get; set; } = 0m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal RateAB { get; set; } = 0m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal RateBC { get; set; } = 0m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal RateCA { get; set; } = 0m;

    [Column(TypeName = "decimal(5,2)")]
    public decimal ServiceChargePercent { get; set; } = 0m;

    [Column(TypeName = "decimal(5,2)")]
    public decimal TaxPercent { get; set; } = 0m;

    public bool IsTaxExemptByDefault { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

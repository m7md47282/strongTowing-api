using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests;

public class CreateInsuranceAccountRequest
{
    [Required(ErrorMessage = "Account name is required")]
    [MaxLength(200, ErrorMessage = "Account name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? AccountNumber { get; set; }

    [MaxLength(200)]
    public string? ContactName { get; set; }

    [EmailAddress(ErrorMessage = "Billing email must be a valid email address")]
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

    [Range(0, 999999.99)]
    public decimal HookupFee { get; set; } = 0m;

    [Range(0, 999999.99)]
    public decimal RateAB { get; set; } = 0m;

    [Range(0, 999999.99)]
    public decimal RateBC { get; set; } = 0m;

    [Range(0, 999999.99)]
    public decimal RateCA { get; set; } = 0m;

    [Range(0, 100)]
    public decimal ServiceChargePercent { get; set; } = 0m;

    [Range(0, 100)]
    public decimal TaxPercent { get; set; } = 0m;

    public bool IsTaxExemptByDefault { get; set; }
}

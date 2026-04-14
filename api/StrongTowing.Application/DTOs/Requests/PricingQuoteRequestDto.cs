namespace StrongTowing.Application.DTOs.Requests;

public class PricingQuoteRequestDto
{
    public int? AccountId { get; set; }
    public string? AccountName { get; set; }

    /// <summary>Optional: resolve account-specific service rates when combined with <see cref="AccountId"/>.</summary>
    public int? ServicePricingProfileId { get; set; }

    /// <summary>Optional: match service by name when profile id not set.</summary>
    public string? ServiceName { get; set; }

    public decimal MilesAB { get; set; }
    public decimal MilesBC { get; set; }
    public decimal MilesCA { get; set; }
    public decimal ExtraItemsTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal? DiscountPercent { get; set; }
    public bool? TaxExempt { get; set; }
    public decimal? HookupFee { get; set; }
    public decimal? RateAB { get; set; }
    public decimal? RateBC { get; set; }
    public decimal? RateCA { get; set; }
    public decimal? ServiceChargePercent { get; set; }
    public decimal? TaxPercent { get; set; }
    public decimal? ManualTotalOverride { get; set; }
    public string? ManualOverrideReason { get; set; }
}

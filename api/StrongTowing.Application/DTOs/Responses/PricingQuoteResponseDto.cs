namespace StrongTowing.Application.DTOs.Responses;

public class PricingQuoteResponseDto
{
    public int? AccountId { get; set; }
    public string? AccountName { get; set; }

    public decimal MilesAB { get; set; }
    public decimal MilesBC { get; set; }
    public decimal MilesCA { get; set; }

    public decimal HookupFee { get; set; }
    public decimal RateAB { get; set; }
    public decimal RateBC { get; set; }
    public decimal RateCA { get; set; }

    public decimal ChargeAB { get; set; }
    public decimal ChargeBC { get; set; }
    public decimal ChargeCA { get; set; }

    public decimal ExtraItemsTotal { get; set; }
    public decimal BaseSubtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal AfterDiscount { get; set; }

    public decimal ServiceChargePercent { get; set; }
    public decimal ServiceChargeAmount { get; set; }
    public bool TaxExempt { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }

    public bool ManualTotalOverrideApplied { get; set; }
    public decimal? ManualTotalOverride { get; set; }
    public string? ManualOverrideReason { get; set; }

    public decimal MaxDiscountPercent { get; set; }
    public bool AllowManualTotalOverride { get; set; }
    public bool ManualOverrideRequiresReason { get; set; }
    public decimal PricingMismatchTolerance { get; set; }
    public string PricingRoundingMode { get; set; } = "AwayFromZero";
}

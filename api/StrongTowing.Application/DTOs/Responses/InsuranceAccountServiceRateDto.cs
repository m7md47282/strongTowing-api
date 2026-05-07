namespace StrongTowing.Application.DTOs.Responses;

public class InsuranceAccountServiceRateDto
{
    public int Id { get; set; }
    public int InsuranceAccountId { get; set; }
    public int ServicePricingProfileId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal PricePerMile { get; set; }
    public decimal? EnroutePricePerMile { get; set; }
    public decimal? LoadedPricePerMile { get; set; }
    public decimal? DeadheadPricePerMile { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

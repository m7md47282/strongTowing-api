namespace StrongTowing.Application.DTOs.Responses;

public class ServicePricingProfileDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal PricePerMile { get; set; }
    public decimal? EnroutePricePerMile { get; set; }
    public decimal? LoadedPricePerMile { get; set; }
    public decimal? DeadheadPricePerMile { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

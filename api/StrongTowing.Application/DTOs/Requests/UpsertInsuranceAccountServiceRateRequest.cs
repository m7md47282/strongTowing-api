using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests;

public class UpsertInsuranceAccountServiceRateRequest
{
    [Required]
    public int ServicePricingProfileId { get; set; }

    [Range(0, 999999.99)]
    public decimal BasePrice { get; set; }

    [Range(0, 999999.99)]
    public decimal PricePerMile { get; set; }

    [Range(0, 999999.99)]
    public decimal? EnroutePricePerMile { get; set; }

    [Range(0, 999999.99)]
    public decimal? LoadedPricePerMile { get; set; }

    [Range(0, 999999.99)]
    public decimal? DeadheadPricePerMile { get; set; }
}

namespace StrongTowing.Application.DTOs.Responses;

public class ServicePricingProfileDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal LoadedPrice { get; set; }
    public decimal DeadHeadPrice { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

namespace StrongTowing.Application.DTOs.Responses;

public class TruckListItemDto
{
    public int Id { get; set; }
    public int TruckTypeId { get; set; }
    public string TruckTypeName { get; set; } = string.Empty;
    public string UnitLabel { get; set; } = string.Empty;
    public string? LicensePlate { get; set; }
    public string? Vin { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public string? Notes { get; set; }
    public bool IsOutOfService { get; set; }
    public bool IsActive { get; set; }
    /// <summary>Out of service | Busy | Available</summary>
    public string AvailabilityLabel { get; set; } = string.Empty;
    public List<TruckActiveJobDto> ActiveJobs { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

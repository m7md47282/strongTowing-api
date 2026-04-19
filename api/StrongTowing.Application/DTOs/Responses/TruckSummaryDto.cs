namespace StrongTowing.Application.DTOs.Responses;

public class TruckSummaryDto
{
    public int Id { get; set; }
    public string UnitLabel { get; set; } = string.Empty;
    public int TruckTypeId { get; set; }
    public string TruckTypeName { get; set; } = string.Empty;
}

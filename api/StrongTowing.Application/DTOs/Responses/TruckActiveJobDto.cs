namespace StrongTowing.Application.DTOs.Responses;

public class TruckActiveJobDto
{
    public int JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? DriverId { get; set; }
    public string? DriverName { get; set; }
}

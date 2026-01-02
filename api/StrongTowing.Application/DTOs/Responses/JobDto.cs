namespace StrongTowing.Application.DTOs.Responses;

public class JobDto
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public int VehicleId { get; set; }
    public VehicleDto? Vehicle { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public string? ClientPhoneNumber { get; set; }
    public decimal Cost { get; set; }
    public string? Notes { get; set; }
    public string? DriverId { get; set; }
    public string? DriverName { get; set; }
    public int PhotoCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? StatusUpdatedById { get; set; }
    public string? StatusUpdatedByName { get; set; }
    public DateTime? StatusUpdatedAt { get; set; }
}


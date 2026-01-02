namespace StrongTowing.Application.DTOs.Responses;

public class VehicleDto
{
    public int Id { get; set; }
    public string VIN { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Color { get; set; } = string.Empty;
}


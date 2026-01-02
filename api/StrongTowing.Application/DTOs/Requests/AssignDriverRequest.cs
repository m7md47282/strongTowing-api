using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests;

public class AssignDriverRequest
{
    [Required(ErrorMessage = "DriverId is required")]
    public string DriverId { get; set; } = string.Empty;
}


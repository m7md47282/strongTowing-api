using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests;

public class OverrideJobCompletionRequest
{
    [Required(ErrorMessage = "Override reason is required.")]
    [MinLength(5, ErrorMessage = "Override reason must be at least 5 characters.")]
    public string Reason { get; set; } = string.Empty;
}

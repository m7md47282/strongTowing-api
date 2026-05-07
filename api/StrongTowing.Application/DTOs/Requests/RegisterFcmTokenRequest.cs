using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests;

public class RegisterFcmTokenRequest
{
    [Required]
    [MinLength(20)]
    [MaxLength(8192)]
    public string Token { get; set; } = string.Empty;
}

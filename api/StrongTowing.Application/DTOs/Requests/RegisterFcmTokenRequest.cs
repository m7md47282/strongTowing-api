using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests;

public class RegisterFcmTokenRequest
{
    [Required]
    [MinLength(20)]
    [MaxLength(4096)]
    public string Token { get; set; } = string.Empty;
}

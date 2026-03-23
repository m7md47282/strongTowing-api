using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests;

public class ReviewPaymentRequest
{
    [Required]
    public string Decision { get; set; } = string.Empty; // Approve | Reject

    public string? Notes { get; set; }
}

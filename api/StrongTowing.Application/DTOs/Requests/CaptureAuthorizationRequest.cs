using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests;

public class CaptureAuthorizationRequest
{
    [Range(0.01, double.MaxValue, ErrorMessage = "Capture amount must be greater than zero.")]
    public decimal? Amount { get; set; }
}

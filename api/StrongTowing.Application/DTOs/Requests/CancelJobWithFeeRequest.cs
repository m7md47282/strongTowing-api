using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests;

public class CancelJobWithFeeRequest
{
    [Required(ErrorMessage = "Cancellation reason is required.")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Optional override percent. If provided by admin, this is used instead of policy matrix.
    /// </summary>
    [Range(0, 100)]
    public decimal? OverrideFeePercent { get; set; }
}

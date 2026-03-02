using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests
{
    public class CreateStripePaymentLinkRequest
    {
        [Required]
        public int JobId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        /// <summary>Optional URL to redirect the customer to after a successful payment.</summary>
        public string? SuccessUrl { get; set; }
    }
}

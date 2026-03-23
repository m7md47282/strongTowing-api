using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests
{
    public class CreatePaymentIntentRequest
    {
        [Required]
        public int JobId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        /// <summary>ISO 4217 currency code, defaults to USD.</summary>
        public string Currency { get; set; } = "usd";

        /// <summary>
        /// When true, creates a manual-capture PaymentIntent to authorize funds first.
        /// </summary>
        public bool ManualCapture { get; set; }
    }
}

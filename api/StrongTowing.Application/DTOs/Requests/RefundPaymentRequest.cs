using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests
{
    public class RefundPaymentRequest
    {
        /// <summary>Amount to refund. If omitted the full payment amount is refunded.</summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal? Amount { get; set; }

        /// <summary>Reason for the refund: duplicate, fraudulent, or requested_by_customer.</summary>
        public string? Reason { get; set; }
    }
}

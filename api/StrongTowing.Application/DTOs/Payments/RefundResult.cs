namespace StrongTowing.Application.DTOs.Payments
{
    public class RefundResult
    {
        /// <summary>Provider-specific refund ID (e.g. Stripe: re_xxx).</summary>
        public string RefundId { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        /// <summary>Provider-agnostic status: "succeeded", "pending", "failed".</summary>
        public string Status { get; set; } = string.Empty;
    }
}

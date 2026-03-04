namespace StrongTowing.Application.DTOs.Payments
{
    public class PaymentLinkResult
    {
        /// <summary>Provider-specific payment link ID (e.g. Stripe: plink_xxx).</summary>
        public string LinkId { get; set; } = string.Empty;

        /// <summary>Shareable URL the customer opens to complete payment.</summary>
        public string Url { get; set; } = string.Empty;
    }
}

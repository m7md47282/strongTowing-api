namespace StrongTowing.Application.DTOs.Responses
{
    public class CreateStripePaymentLinkResponse
    {
        public string Url { get; set; } = string.Empty;
        public string StripePaymentLinkId { get; set; } = string.Empty;
        public int PaymentRecordId { get; set; }
        public decimal Amount { get; set; }
    }
}

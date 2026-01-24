namespace StrongTowing.Application.DTOs.Requests
{
    public class ProcessPaymentRequest
    {
        public int JobId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty; // 'Card', 'PaymentLink', 'Cash'
        public string? PaymentIntentId { get; set; } // For Stripe
        public string? TransactionId { get; set; }
    }
}

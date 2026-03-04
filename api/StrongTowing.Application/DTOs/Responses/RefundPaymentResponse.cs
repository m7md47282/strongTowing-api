namespace StrongTowing.Application.DTOs.Responses
{
    public class RefundPaymentResponse
    {
        public string RefundId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public int PaymentId { get; set; }
        public string? Reason { get; set; }
    }
}


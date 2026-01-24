namespace StrongTowing.Application.DTOs.Responses
{
    public class PaymentListItemDto
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public string JobNumber { get; set; } = string.Empty; // Display-friendly job ID
        public string ClientName { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
        public string ProcessedByName { get; set; } = string.Empty;
        
        // Driver info
        public string? DriverId { get; set; }
        public string? DriverName { get; set; }
        public decimal DriverCommission { get; set; }
        
        // Cash collection
        public bool CashCollected { get; set; }
        public string? CashCollectedBy { get; set; }
        public DateTime? CashCollectedAt { get; set; }
        
        // Transaction info
        public string? TransactionId { get; set; }
        public string? CardLast4 { get; set; }
        public string? CardBrand { get; set; }
    }
}

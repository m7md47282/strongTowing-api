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
        public string CaptureStatus { get; set; } = string.Empty;
        public bool IsPreAuthorization { get; set; }
        public decimal? AuthorizedAmount { get; set; }
        public decimal? CapturedAmount { get; set; }
        public DateTime? AuthorizationExpiresAt { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string ProcessedByName { get; set; } = string.Empty;
        
        // Driver info
        public string? DriverId { get; set; }
        public string? DriverName { get; set; }
        public decimal DriverCommission { get; set; }

        /// <summary>System commission rate used for <see cref="DriverCommission"/>.</summary>
        public decimal DriverCommissionRatePercent { get; set; }

        // Cash collection
        public bool CashCollected { get; set; }
        public string? CashCollectedBy { get; set; }
        public DateTime? CashCollectedAt { get; set; }
        
        // Transaction info
        public string? TransactionId { get; set; }
        public string? CardLast4 { get; set; }
        public string? CardBrand { get; set; }
        public string? PaymentErrorMessage { get; set; }
        public string FraudStatus { get; set; } = string.Empty;
        public int? FraudScore { get; set; }
        public bool IsCancellationFeePayment { get; set; }
        public decimal? CancellationFeeAmount { get; set; }
    }
}

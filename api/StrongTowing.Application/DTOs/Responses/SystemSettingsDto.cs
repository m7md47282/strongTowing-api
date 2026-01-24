namespace StrongTowing.Application.DTOs.Responses
{
    public class SystemSettingsDto
    {
        public int Id { get; set; }
        public decimal DriverCommissionPercentage { get; set; }
        public string PayPeriodType { get; set; } = string.Empty;
        public string? StripePublicKey { get; set; }
        public bool StripeEnabled { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
    }
}

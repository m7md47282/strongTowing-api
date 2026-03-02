namespace StrongTowing.Application.DTOs.Responses
{
    public class SystemSettingsDto
    {
        public int Id { get; set; }
        public decimal DriverCommissionPercentage { get; set; }
        public string PayPeriodType { get; set; } = string.Empty;
        public string? StripePublicKey { get; set; }
        public bool StripeEnabled { get; set; }
        public bool StripeSecretKeyConfigured { get; set; }
        public bool StripeWebhookConfigured { get; set; }
        public string? StripeTestPublicKey { get; set; }
        public bool StripeTestSecretKeyConfigured { get; set; }
        public bool StripeTestWebhookConfigured { get; set; }
        public string? StripeLivePublicKey { get; set; }
        public bool StripeLiveSecretKeyConfigured { get; set; }
        public bool StripeLiveWebhookConfigured { get; set; }
        public string StripeMode { get; set; } = "test";
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
    }
}

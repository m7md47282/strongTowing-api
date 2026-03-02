namespace StrongTowing.Application.DTOs.Requests
{
    public class UpdateSystemSettingsRequest
    {
        public decimal DriverCommissionPercentage { get; set; }
        public string? StripePublicKey { get; set; }
        public string? StripeSecretKey { get; set; }
        public string? StripeWebhookSecret { get; set; }
        public bool StripeEnabled { get; set; }
        public string? StripeTestPublicKey { get; set; }
        public string? StripeTestSecretKey { get; set; }
        public string? StripeTestWebhookSecret { get; set; }
        public string? StripeLivePublicKey { get; set; }
        public string? StripeLiveSecretKey { get; set; }
        public string? StripeLiveWebhookSecret { get; set; }
        public string StripeMode { get; set; } = "test";
    }
}

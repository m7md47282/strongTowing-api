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
        public bool PreAuthorizationEnabled { get; set; } = true;
        public decimal PreAuthorizationMinAmount { get; set; } = 150.00m;
        public decimal PreAuthorizationMaxAmount { get; set; } = 300.00m;
        public int FraudReviewScoreThreshold { get; set; } = 60;
        public int DuplicateRequestWindowMinutes { get; set; } = 30;
        public decimal CancelFeeBeforeDispatchPercent { get; set; } = 0.00m;
        public decimal CancelFeeAfterDispatchPercent { get; set; } = 30.00m;
        public decimal CancelFeeAfterArrivalPercent { get; set; } = 50.00m;
    }
}

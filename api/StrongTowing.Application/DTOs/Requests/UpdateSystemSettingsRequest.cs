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
        public decimal DefaultPricingTaxPercent { get; set; } = 10.00m;
        public decimal DefaultPricingServiceChargePercent { get; set; } = 0.00m;
        public decimal DefaultPricingHookupFee { get; set; } = 75.00m;
        public decimal MaxDiscountPercent { get; set; } = 100.00m;
        public bool AllowManualTotalOverride { get; set; } = false;
        public bool ManualOverrideRequiresReason { get; set; } = true;
        public decimal PricingMismatchTolerance { get; set; } = 1.00m;
        public string PricingRoundingMode { get; set; } = "AwayFromZero";

        public double? OfficeLatitude { get; set; }
        public double? OfficeLongitude { get; set; }

        public bool SmsEnabled { get; set; } = true;
        public string? SmsTwilioAccountSid { get; set; }
        public string? SmsTwilioAuthToken { get; set; }
        public string? SmsTwilioFromNumber { get; set; }
        public string? SmsTwilioMessagingServiceSid { get; set; }

        public bool SmsDriverJobAssigned { get; set; } = true;
        public bool SmsDriverJobCompleted { get; set; } = true;
        public bool SmsDriverPayrollPaid { get; set; } = true;

        public bool SmsClientJobCreated { get; set; } = true;
        public bool SmsClientFraudUnderReview { get; set; } = true;
        public bool SmsClientDriverAssigned { get; set; } = true;
        public bool SmsClientStatusOnRoute { get; set; } = true;
        public bool SmsClientStatusOnScene { get; set; } = true;
        public bool SmsClientStatusLoaded { get; set; } = true;
        public bool SmsClientPaymentLinkCreated { get; set; } = true;
        public bool SmsClientPaymentSucceeded { get; set; } = true;
        public bool SmsClientPaymentFailed { get; set; } = true;
        public bool SmsClientJobCancelled { get; set; } = true;
        public bool SmsClientJobCompleted { get; set; } = true;
    }
}

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
        public bool PreAuthorizationEnabled { get; set; }
        public decimal PreAuthorizationMinAmount { get; set; }
        public decimal PreAuthorizationMaxAmount { get; set; }
        public int FraudReviewScoreThreshold { get; set; }
        public int DuplicateRequestWindowMinutes { get; set; }
        public decimal CancelFeeBeforeDispatchPercent { get; set; }
        public decimal CancelFeeAfterDispatchPercent { get; set; }
        public decimal CancelFeeAfterArrivalPercent { get; set; }
        public decimal DefaultPricingTaxPercent { get; set; }
        public decimal DefaultPricingServiceChargePercent { get; set; }
        public decimal DefaultPricingHookupFee { get; set; }
        public decimal MaxDiscountPercent { get; set; }
        public bool AllowManualTotalOverride { get; set; }
        public bool ManualOverrideRequiresReason { get; set; }
        public decimal PricingMismatchTolerance { get; set; }
        public string PricingRoundingMode { get; set; } = "AwayFromZero";
        public double? OfficeLatitude { get; set; }
        public double? OfficeLongitude { get; set; }

        public bool SmsEnabled { get; set; }
        public string? SmsTwilioAccountSid { get; set; }
        public bool SmsTwilioAuthTokenConfigured { get; set; }
        public string? SmsTwilioFromNumber { get; set; }
        public string? SmsTwilioMessagingServiceSid { get; set; }

        public bool SmsDriverJobAssigned { get; set; }
        public bool SmsDriverJobCompleted { get; set; }
        public bool SmsDriverPayrollPaid { get; set; }

        public bool SmsClientJobCreated { get; set; }
        public bool SmsClientFraudUnderReview { get; set; }
        public bool SmsClientDriverAssigned { get; set; }
        public bool SmsClientStatusOnRoute { get; set; }
        public bool SmsClientStatusOnScene { get; set; }
        public bool SmsClientStatusLoaded { get; set; }
        public bool SmsClientPaymentLinkCreated { get; set; }
        public bool SmsClientPaymentSucceeded { get; set; }
        public bool SmsClientPaymentFailed { get; set; }
        public bool SmsClientJobCancelled { get; set; }
        public bool SmsClientJobCompleted { get; set; }

        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
    }
}

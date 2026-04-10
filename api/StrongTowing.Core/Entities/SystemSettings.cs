using System.ComponentModel.DataAnnotations.Schema;

namespace StrongTowing.Core.Entities
{
    public class SystemSettings
    {
        public int Id { get; set; }
        
        // Driver commission settings
        [Column(TypeName = "decimal(5,2)")]
        public decimal DriverCommissionPercentage { get; set; } = 30.00m;
        
        // Payroll settings
        public string PayPeriodType { get; set; } = "BiWeekly"; // Fixed for now
        
        // Stripe settings
        public string? StripePublicKey { get; set; }
        public string? StripeSecretKey { get; set; } // Should be encrypted
        public string? StripeWebhookSecret { get; set; } // Should be encrypted
        public bool StripeEnabled { get; set; } = false;

        // Stripe test-mode keys
        public string? StripeTestPublicKey { get; set; }
        public string? StripeTestSecretKey { get; set; } // Stored AES-256 encrypted
        public string? StripeTestWebhookSecret { get; set; } // Stored AES-256 encrypted

        // Stripe live-mode keys
        public string? StripeLivePublicKey { get; set; }
        public string? StripeLiveSecretKey { get; set; } // Stored AES-256 encrypted
        public string? StripeLiveWebhookSecret { get; set; } // Stored AES-256 encrypted

        /// <summary>
        /// Active Stripe mode. Valid values: "test" or "live".
        /// </summary>
        public string StripeMode { get; set; } = "test";

        // Pre-authorization hold settings
        public bool PreAuthorizationEnabled { get; set; } = true;
        [Column(TypeName = "decimal(18,2)")]
        public decimal PreAuthorizationMinAmount { get; set; } = 150.00m;
        [Column(TypeName = "decimal(18,2)")]
        public decimal PreAuthorizationMaxAmount { get; set; } = 300.00m;

        // Fraud/risk settings
        public int FraudReviewScoreThreshold { get; set; } = 60;
        public int DuplicateRequestWindowMinutes { get; set; } = 30;

        // Cancellation fee matrix
        [Column(TypeName = "decimal(5,2)")]
        public decimal CancelFeeBeforeDispatchPercent { get; set; } = 0.00m;
        [Column(TypeName = "decimal(5,2)")]
        public decimal CancelFeeAfterDispatchPercent { get; set; } = 30.00m;
        [Column(TypeName = "decimal(5,2)")]
        public decimal CancelFeeAfterArrivalPercent { get; set; } = 50.00m;

        // Pricing policy defaults (used when account profile does not set a value)
        [Column(TypeName = "decimal(5,2)")]
        public decimal DefaultPricingTaxPercent { get; set; } = 10.00m;
        [Column(TypeName = "decimal(5,2)")]
        public decimal DefaultPricingServiceChargePercent { get; set; } = 0.00m;
        [Column(TypeName = "decimal(18,2)")]
        public decimal DefaultPricingHookupFee { get; set; } = 75.00m;

        // Pricing policy guardrails
        [Column(TypeName = "decimal(5,2)")]
        public decimal MaxDiscountPercent { get; set; } = 100.00m;
        public bool AllowManualTotalOverride { get; set; } = false;
        public bool ManualOverrideRequiresReason { get; set; } = true;
        [Column(TypeName = "decimal(18,2)")]
        public decimal PricingMismatchTolerance { get; set; } = 1.00m;
        public string PricingRoundingMode { get; set; } = "AwayFromZero";

        /// <summary>Dispatch office latitude (WGS84). When set with OfficeLongitude, overrides appsettings office for maps/routes.</summary>
        public double? OfficeLatitude { get; set; }

        /// <summary>Dispatch office longitude (WGS84).</summary>
        public double? OfficeLongitude { get; set; }

        // Twilio SMS (credentials from Admin Settings; Auth Token encrypted like Stripe)
        public bool SmsEnabled { get; set; } = true;
        public string? SmsTwilioAccountSid { get; set; }
        public string? SmsTwilioAuthToken { get; set; }
        /// <summary>E.164 sender phone, e.g. +15551234567. Use this or MessagingServiceSid.</summary>
        public string? SmsTwilioFromNumber { get; set; }
        /// <summary>Optional Twilio Messaging Service SID (MG...). Use this or FromNumber.</summary>
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

        // Postmark email (Server API token encrypted like Twilio Auth Token)
        public bool EmailEnabled { get; set; } = true;
        public string? PostmarkServerToken { get; set; }
        /// <summary>Must be a verified sender or domain in Postmark.</summary>
        public string? PostmarkDefaultFromEmail { get; set; }
        /// <summary>Optional; default transactional stream is "outbound".</summary>
        public string? PostmarkMessageStream { get; set; }

        public bool EmailDriverJobAssigned { get; set; } = true;
        public bool EmailDriverJobCompleted { get; set; } = true;
        public bool EmailDriverPayrollPaid { get; set; } = true;

        public bool EmailClientJobCreated { get; set; } = true;
        public bool EmailClientFraudUnderReview { get; set; } = true;
        public bool EmailClientDriverAssigned { get; set; } = true;
        public bool EmailClientStatusOnRoute { get; set; } = true;
        public bool EmailClientStatusOnScene { get; set; } = true;
        public bool EmailClientStatusLoaded { get; set; } = true;
        public bool EmailClientPaymentLinkCreated { get; set; } = true;
        public bool EmailClientPaymentSucceeded { get; set; } = true;
        public bool EmailClientPaymentFailed { get; set; } = true;
        public bool EmailClientJobCancelled { get; set; } = true;
        public bool EmailClientJobCompleted { get; set; } = true;
        
        // Metadata
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string UpdatedBy { get; set; } = string.Empty; // Admin User ID
    }
}

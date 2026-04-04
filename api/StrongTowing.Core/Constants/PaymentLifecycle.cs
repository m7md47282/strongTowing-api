namespace StrongTowing.Core.Constants;

public static class PaymentLifecycle
{
    public static class Methods
    {
        public const string Card = "Card";
        public const string PaymentLink = "PaymentLink";
        public const string Cash = "Cash";
        /// <summary>Job paid via insurance billing (see Job.BillingPaymentMode).</summary>
        public const string Insurance = "Insurance";
        /// <summary>Cash collected by driver; payroll deduction (see Job.DriverCashCollectedAmount).</summary>
        public const string CashToDriverPayroll = "CashToDriverPayroll";
        /// <summary>Insurance + client split (see Job.BillingPaymentMode).</summary>
        public const string SplitInsuranceClient = "SplitInsuranceClient";
    }

    public static class Statuses
    {
        public const string Unpaid = "Unpaid";
        public const string Pending = "Pending";
        public const string PendingCash = "PendingCash";
        public const string UnderReview = "UnderReview";
        public const string Authorized = "Authorized";
        public const string CapturePending = "CapturePending";
        public const string Paid = "Paid";
        public const string Failed = "Failed";
        public const string Cancelled = "Cancelled";
        public const string Refunded = "Refunded";
        public const string PartiallyRefunded = "PartiallyRefunded";
    }

    public static class FraudStatuses
    {
        public const string Clear = "Clear";
        public const string UnderReview = "UnderReview";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
    }

    public static class CaptureStatuses
    {
        public const string NotApplicable = "NotApplicable";
        public const string PendingAuthorization = "PendingAuthorization";
        public const string Authorized = "Authorized";
        public const string Captured = "Captured";
        public const string PartiallyCaptured = "PartiallyCaptured";
        public const string Released = "Released";
    }
}

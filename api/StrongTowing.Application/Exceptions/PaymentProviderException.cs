namespace StrongTowing.Application.Exceptions
{
    /// <summary>
    /// Thrown by any IPaymentProvider implementation when a provider-specific error occurs
    /// (e.g. a Stripe API error). Allows the controller to handle payment failures without
    /// importing provider-specific SDKs.
    /// </summary>
    public class PaymentProviderException : Exception
    {
        public string ProviderName { get; }
        public string? ErrorCode { get; }

        public PaymentProviderException(string providerName, string message, Exception? inner = null, string? errorCode = null)
            : base(message, inner)
        {
            ProviderName = providerName;
            ErrorCode = errorCode;
        }
    }
}

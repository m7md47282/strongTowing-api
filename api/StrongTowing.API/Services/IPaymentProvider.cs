using StrongTowing.Application.DTOs.Payments;

namespace StrongTowing.API.Services;

/// <summary>
/// Provider-agnostic payment abstraction. Implement this interface to add support for a new
/// payment provider (e.g. Square, PayPal) without touching the controller or any business logic.
/// Swap the active implementation via a single line in Program.cs.
/// </summary>
public interface IPaymentProvider
{
    /// <summary>Human-readable provider name, e.g. "Stripe".</summary>
    string ProviderName { get; }

    /// <summary>
    /// The HTTP request header name the provider uses to transmit the webhook signature,
    /// e.g. "Stripe-Signature" for Stripe.
    /// </summary>
    string WebhookSignatureHeaderName { get; }

    /// <summary>
    /// Create a payment intent and return the client secret needed by the frontend SDK
    /// to confirm the payment on the customer's device.
    /// </summary>
    Task<PaymentIntentResult> CreatePaymentIntentAsync(decimal amount, string currency, int jobId);

    /// <summary>
    /// Create a hosted payment link that can be shared with a customer.
    /// </summary>
    Task<PaymentLinkResult> CreatePaymentLinkAsync(decimal amount, int jobId, string? successUrl = null);

    /// <summary>
    /// Issue a full or partial refund against an existing transaction.
    /// </summary>
    /// <param name="transactionId">The provider-specific transaction/intent ID.</param>
    Task<RefundResult> RefundAsync(string transactionId, decimal? amount, string? reason);

    /// <summary>
    /// Retrieve the current state of a previously created payment intent.
    /// </summary>
    Task<PaymentIntentResult> GetPaymentIntentAsync(string transactionId);

    /// <summary>
    /// Resolve payment-link correlation information from a provider-specific transaction/intent ID.
    /// Returns null when no payment-link context can be derived.
    /// </summary>
    Task<PaymentLinkCorrelationResult?> ResolvePaymentLinkCorrelationAsync(string transactionId);

    /// <summary>
    /// Verify the webhook signature and parse the inbound event into a normalized
    /// <see cref="WebhookEventResult"/>. Throws <see cref="Application.Exceptions.PaymentProviderException"/>
    /// if the signature is invalid.
    /// </summary>
    WebhookEventResult ParseWebhookEvent(string json, string signature, string webhookSecret);
}

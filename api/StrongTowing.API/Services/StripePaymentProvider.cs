using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using StrongTowing.Application.DTOs.Payments;
using StrongTowing.Application.Exceptions;
using StrongTowing.Infrastructure.Data;
using System.Text.Json;

namespace StrongTowing.API.Services;

public class StripePaymentProvider : IPaymentProvider
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<StripePaymentProvider> _logger;

    public string ProviderName => "Stripe";
    public string WebhookSignatureHeaderName => "Stripe-Signature";

    public StripePaymentProvider(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        ILogger<StripePaymentProvider> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(decimal amount, string currency, int jobId)
    {
        try
        {
            var config = await GetActiveStripeConfigurationAsync();
            StripeConfiguration.ApiKey = config.SecretKey;

            var service = new PaymentIntentService();
            var options = new PaymentIntentCreateOptions
            {
                Amount = ToMinorUnits(amount),
                Currency = NormalizeCurrency(currency),
                Metadata = new Dictionary<string, string>
                {
                    { "jobId", jobId.ToString() }
                },
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                }
            };

            var intent = await service.CreateAsync(options);

            return new PaymentIntentResult
            {
                IntentId = intent.Id,
                ClientSecret = intent.ClientSecret ?? string.Empty,
                PublishableKey = config.PublishableKey,
                Amount = amount,
                Currency = NormalizeCurrency(currency),
                Status = intent.Status ?? string.Empty
            };
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error while creating payment intent.");
            throw new PaymentProviderException(ProviderName, ex.StripeError?.Message ?? ex.Message, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating payment intent.");
            throw new PaymentProviderException(ProviderName, "Unexpected error while creating payment intent.", ex);
        }
    }

    public async Task<PaymentLinkResult> CreatePaymentLinkAsync(decimal amount, int jobId, string? successUrl = null)
    {
        try
        {
            var config = await GetActiveStripeConfigurationAsync();
            StripeConfiguration.ApiKey = config.SecretKey;

            var service = new PaymentLinkService();
            var options = new PaymentLinkCreateOptions
            {
                LineItems = new List<PaymentLinkLineItemOptions>
                {
                    new()
                    {
                        Quantity = 1,
                        PriceData = new PaymentLinkLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            UnitAmount = ToMinorUnits(amount),
                            ProductData = new PaymentLinkLineItemPriceDataProductDataOptions
                            {
                                Name = $"Towing Service - Job #{jobId}"
                            }
                        }
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    { "jobId", jobId.ToString() }
                },
                // Ensure the generated PaymentIntent also carries job metadata so
                // payment_intent.succeeded webhooks can reconcile pending link payments.
                PaymentIntentData = new PaymentLinkPaymentIntentDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        { "jobId", jobId.ToString() }
                    }
                }
            };

            var link = await service.CreateAsync(options);

            return new PaymentLinkResult
            {
                LinkId = link.Id,
                Url = link.Url
            };
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error while creating payment link.");
            throw new PaymentProviderException(ProviderName, ex.StripeError?.Message ?? ex.Message, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating payment link.");
            throw new PaymentProviderException(ProviderName, "Unexpected error while creating payment link.", ex);
        }
    }

    public async Task<RefundResult> RefundAsync(string transactionId, decimal? amount, string? reason)
    {
        try
        {
            var config = await GetActiveStripeConfigurationAsync();
            StripeConfiguration.ApiKey = config.SecretKey;

            var service = new RefundService();
            var options = new RefundCreateOptions
            {
                PaymentIntent = transactionId
            };

            if (amount.HasValue)
            {
                options.Amount = ToMinorUnits(amount.Value);
            }

            var refund = await service.CreateAsync(options);

            return new RefundResult
            {
                RefundId = refund.Id,
                Amount = refund.Amount / 100m,
                Status = refund.Status ?? string.Empty
            };
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error while processing refund.");
            throw new PaymentProviderException(ProviderName, ex.StripeError?.Message ?? ex.Message, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while processing refund.");
            throw new PaymentProviderException(ProviderName, "Unexpected error while processing refund.", ex);
        }
    }

    public async Task<PaymentIntentResult> GetPaymentIntentAsync(string transactionId)
    {
        try
        {
            var config = await GetActiveStripeConfigurationAsync();
            StripeConfiguration.ApiKey = config.SecretKey;

            var service = new PaymentIntentService();
            var intent = await service.GetAsync(transactionId);

            return new PaymentIntentResult
            {
                IntentId = intent.Id,
                ClientSecret = intent.ClientSecret ?? string.Empty,
                PublishableKey = config.PublishableKey,
                Amount = intent.Amount / 100m,
                Currency = intent.Currency ?? "usd",
                Status = intent.Status ?? string.Empty
            };
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error while retrieving payment intent.");
            throw new PaymentProviderException(ProviderName, ex.StripeError?.Message ?? ex.Message, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving payment intent.");
            throw new PaymentProviderException(ProviderName, "Unexpected error while retrieving payment intent.", ex);
        }
    }

    public async Task<PaymentLinkCorrelationResult?> ResolvePaymentLinkCorrelationAsync(string transactionId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                return null;
            }

            var config = await GetActiveStripeConfigurationAsync();
            StripeConfiguration.ApiKey = config.SecretKey;

            var sessionService = new SessionService();
            var sessions = await sessionService.ListAsync(new SessionListOptions
            {
                PaymentIntent = transactionId,
                Limit = 1
            });

            var session = sessions.Data.FirstOrDefault();
            if (session == null)
            {
                return null;
            }

            int? jobId = null;
            if (session.Metadata != null &&
                session.Metadata.TryGetValue("jobId", out var jobIdText) &&
                int.TryParse(jobIdText, out var parsedJobId))
            {
                jobId = parsedJobId;
            }

            return new PaymentLinkCorrelationResult
            {
                PaymentLinkId = session.PaymentLinkId,
                SessionId = session.Id,
                JobId = jobId
            };
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe error while resolving payment link correlation for transaction {TransactionId}.", transactionId);
            throw new PaymentProviderException(ProviderName, ex.StripeError?.Message ?? ex.Message, ex);
        }
    }

    public WebhookEventResult ParseWebhookEvent(string json, string signature, string webhookSecret)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);
            using var jsonDoc = JsonDocument.Parse(json);
            var objectNode = jsonDoc.RootElement
                .GetProperty("data")
                .GetProperty("object");
            var result = new WebhookEventResult
            {
                RawProviderEventType = stripeEvent.Type
            };

            // Fallback extraction directly from raw payload to avoid SDK property variance.
            if (objectNode.TryGetProperty("payment_link", out var paymentLinkProp) && paymentLinkProp.ValueKind == JsonValueKind.String)
            {
                result.PaymentLinkId = paymentLinkProp.GetString();
            }
            if (objectNode.TryGetProperty("payment_intent", out var paymentIntentProp) && paymentIntentProp.ValueKind == JsonValueKind.String)
            {
                result.TransactionId = paymentIntentProp.GetString();
            }
            if (objectNode.TryGetProperty("id", out var objectIdProp) && objectIdProp.ValueKind == JsonValueKind.String)
            {
                result.SessionId = objectIdProp.GetString();
            }
            if (objectNode.TryGetProperty("amount_total", out var amountTotalProp) && amountTotalProp.ValueKind == JsonValueKind.Number)
            {
                result.AmountPaid = amountTotalProp.GetDecimal() / 100m;
            }
            if (objectNode.TryGetProperty("metadata", out var metadataProp) &&
                metadataProp.ValueKind == JsonValueKind.Object &&
                metadataProp.TryGetProperty("jobId", out var jobIdProp))
            {
                if (jobIdProp.ValueKind == JsonValueKind.String && int.TryParse(jobIdProp.GetString(), out var parsedJobId))
                {
                    result.JobId = parsedJobId;
                }
                else if (jobIdProp.ValueKind == JsonValueKind.Number && jobIdProp.TryGetInt32(out var numericJobId))
                {
                    result.JobId = numericJobId;
                }
            }

            if (objectNode.TryGetProperty("last_payment_error", out var lastPaymentErrorProp) &&
                lastPaymentErrorProp.ValueKind == JsonValueKind.Object &&
                lastPaymentErrorProp.TryGetProperty("message", out var paymentErrorMessageProp) &&
                paymentErrorMessageProp.ValueKind == JsonValueKind.String)
            {
                result.ErrorMessage = paymentErrorMessageProp.GetString();
            }

            if (string.IsNullOrWhiteSpace(result.ErrorMessage) &&
                objectNode.TryGetProperty("failure_message", out var failureMessageProp) &&
                failureMessageProp.ValueKind == JsonValueKind.String)
            {
                result.ErrorMessage = failureMessageProp.GetString();
            }

            switch (stripeEvent.Type)
            {
                case "payment_intent.succeeded":
                {
                    var intent = stripeEvent.Data.Object as PaymentIntent;
                    result.EventType = WebhookEventResult.PaymentSucceeded;
                    result.TransactionId = intent?.Id;
                    break;
                }
                case "payment_intent.payment_failed":
                {
                    var intent = stripeEvent.Data.Object as PaymentIntent;
                    result.EventType = WebhookEventResult.PaymentFailed;
                    result.TransactionId = intent?.Id;
                    break;
                }
                case "checkout.session.completed":
                case "checkout.session.async_payment_succeeded":
                {
                    var session = stripeEvent.Data.Object as Session;
                    result.EventType = WebhookEventResult.PaymentLinkCompleted;
                    result.PaymentLinkId ??= session?.PaymentLinkId;
                    result.SessionId ??= session?.Id;
                    result.TransactionId ??= session?.PaymentIntentId;
                    break;
                }
                case "charge.refunded":
                {
                    var charge = stripeEvent.Data.Object as Charge;
                    result.EventType = WebhookEventResult.PaymentRefunded;
                    result.TransactionId = charge?.PaymentIntentId;
                    if (charge?.AmountRefunded != null)
                    {
                        result.AmountRefunded = charge.AmountRefunded / 100m;
                    }
                    break;
                }
                default:
                    result.EventType = WebhookEventResult.Unknown;
                    break;
            }

            return result;
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature verification/parsing failed.");
            throw new PaymentProviderException(ProviderName, ex.StripeError?.Message ?? ex.Message, ex);
        }
    }

    private async Task<(string SecretKey, string PublishableKey, bool IsLive)> GetActiveStripeConfigurationAsync()
    {
        var settings = await _context.SystemSettings.FirstOrDefaultAsync();
        if (settings == null || !settings.StripeEnabled)
        {
            throw new InvalidOperationException("Stripe is disabled. Enable it from system settings.");
        }

        var isLive = string.Equals(settings.StripeMode, "live", StringComparison.OrdinalIgnoreCase);
        var encryptedSecret = isLive
            ? (settings.StripeLiveSecretKey ?? settings.StripeSecretKey)
            : (settings.StripeTestSecretKey ?? settings.StripeSecretKey);
        var publishableKey = isLive
            ? (settings.StripeLivePublicKey ?? settings.StripePublicKey)
            : (settings.StripeTestPublicKey ?? settings.StripePublicKey);

        if (string.IsNullOrWhiteSpace(encryptedSecret))
        {
            throw new InvalidOperationException($"Stripe {(isLive ? "live" : "test")} secret key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(publishableKey))
        {
            throw new InvalidOperationException($"Stripe {(isLive ? "live" : "test")} publishable key is not configured.");
        }

        var secretKey = _encryptionService.Decrypt(encryptedSecret);
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException($"Stripe {(isLive ? "live" : "test")} secret key could not be decrypted.");
        }

        return (secretKey, publishableKey, isLive);
    }

    private static string NormalizeCurrency(string currency)
    {
        return string.IsNullOrWhiteSpace(currency) ? "usd" : currency.Trim().ToLowerInvariant();
    }

    private static long ToMinorUnits(decimal amount)
    {
        return Convert.ToInt64(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));
    }
}

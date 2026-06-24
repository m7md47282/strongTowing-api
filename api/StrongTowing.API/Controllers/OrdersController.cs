using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTowing.API.Services;
using StrongTowing.Application.Abstractions;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Application.Exceptions;
using StrongTowing.Core.Constants;
using StrongTowing.Core.Entities;
using StrongTowing.Core.Enums;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IPaymentProvider _paymentProvider;
    private readonly ISmsNotificationService _smsNotificationService;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IPaymentProvider paymentProvider,
        ISmsNotificationService smsNotificationService,
        IEmailNotificationService emailNotificationService,
        ILogger<OrdersController> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _paymentProvider = paymentProvider;
        _smsNotificationService = smsNotificationService;
        _emailNotificationService = emailNotificationService;
        _logger = logger;
    }

    /// <summary>
    /// Public guest/customer order entry. Creates customer+vehicle+job and initializes payment.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<CreateOrderResponse>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            var paymentDueMode = NormalizePaymentDueMode(request.PaymentDueMode);
            var paymentMethod = NormalizePaymentMethod(request.PaymentMethod, paymentDueMode);
            var amount = request.Amount.GetValueOrDefault(75m);
            var contactName = string.IsNullOrWhiteSpace(request.ContactName)
                ? $"Guest {request.ContactPhone}"
                : request.ContactName.Trim();

            if (amount <= 0)
            {
                return BadRequest(new { error = "Bad Request", message = "Amount must be greater than zero." });
            }

            var customer = await FindOrCreateGuestCustomerAsync(request, contactName);
            var vehicle = await FindOrCreateVehicleAsync(customer, request);

            // Promote consent onto the customer record so future jobs from the same user
            // inherit it. We never downgrade an existing opt-in — only upgrade to true.
            if (request.SmsOptIn && !customer.SmsOptIn)
            {
                customer.SmsOptIn = true;
                customer.SmsOptInUpdatedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            var job = new Job
            {
                VehicleId = vehicle.Id,
                Status = JobStatus.Waiting,
                ServiceType = request.ServiceType,
                Priority = request.Priority,
                Reason = request.Description,
                Notes = request.Notes,
                Cost = amount,
                CallType = "Web App Guest No Login",
                ContactName = contactName,
                ContactPhoneNumber = request.ContactPhone,
                ContactSmsOptIn = request.SmsOptIn,
                PickupLocation = BuildAddress(request.PickupAddress, request.PickupCity, request.PickupState, request.PickupZipCode),
                DestinationAddress = BuildAddress(request.DestinationAddress, request.DestinationCity, request.DestinationState, request.DestinationZipCode),
                LicensePlate = request.LicensePlate,
                VehicleType = request.VehicleType,
                CreatedAt = DateTime.UtcNow,
                PaymentMethod = paymentMethod
            };

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            var fraud = await EvaluateFraudRiskAsync(request, amount, settings);

            // Keep job+payment linked immediately in all flows.
            var payment = new Payment
            {
                JobId = job.Id,
                Amount = amount,
                PaymentMethod = paymentMethod,
                PaymentStatus = paymentDueMode == "PayLater"
                    ? (paymentMethod == PaymentLifecycle.Methods.Cash ? PaymentLifecycle.Statuses.PendingCash : PaymentLifecycle.Statuses.Pending)
                    : PaymentLifecycle.Statuses.CapturePending,
                CaptureStatus = paymentDueMode == "PayNow"
                    ? PaymentLifecycle.CaptureStatuses.PendingAuthorization
                    : PaymentLifecycle.CaptureStatuses.NotApplicable,
                ProcessedBy = User?.Identity?.IsAuthenticated == true ? _userManager.GetUserId(User) : null,
                FraudScore = fraud.Score,
                FraudReasons = fraud.Reasons,
                FraudStatus = fraud.UnderReview ? PaymentLifecycle.FraudStatuses.UnderReview : PaymentLifecycle.FraudStatuses.Clear,
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            job.PaymentId = payment.Id;
            job.PaymentStatus = payment.PaymentStatus;
            await _context.SaveChangesAsync();

            await _context.Entry(job).Reference(j => j.Vehicle).LoadAsync();
            if (job.Vehicle != null)
                await _context.Entry(job.Vehicle).Reference(v => v.Owner).LoadAsync();
            var contactPhone = job.ContactPhoneNumber;
            var ownerPhone = job.Vehicle?.Owner?.PhoneNumber;
            var contactEmail = customer.Email;
            var ownerEmail = job.Vehicle?.Owner?.Email;
            await _smsNotificationService.NotifyClientJobCreatedAsync(job.Id, contactPhone, ownerPhone);
            await _emailNotificationService.NotifyClientJobCreatedAsync(job.Id, contactEmail, ownerEmail);

            // If risk score is too high, queue for review before payment action.
            if (fraud.UnderReview)
            {
                job.PaymentStatus = PaymentLifecycle.Statuses.UnderReview;
                payment.PaymentStatus = PaymentLifecycle.Statuses.UnderReview;
                await _context.SaveChangesAsync();

                await _smsNotificationService.NotifyClientFraudUnderReviewAsync(job.Id, contactPhone, ownerPhone);
                await _emailNotificationService.NotifyClientFraudUnderReviewAsync(job.Id, contactEmail, ownerEmail);

                return Ok(new CreateOrderResponse
                {
                    JobId = job.Id,
                    PaymentId = payment.Id,
                    PaymentStatus = payment.PaymentStatus,
                    PaymentDueMode = paymentDueMode,
                    Amount = amount,
                    Currency = request.Currency,
                    FraudStatus = payment.FraudStatus,
                    FraudScore = payment.FraudScore,
                    FraudReasons = payment.FraudReasons
                });
            }

            if (paymentDueMode == "PayLater")
            {
                return Ok(new CreateOrderResponse
                {
                    JobId = job.Id,
                    PaymentId = payment.Id,
                    PaymentStatus = payment.PaymentStatus,
                    PaymentDueMode = paymentDueMode,
                    Amount = amount,
                    Currency = request.Currency,
                    FraudStatus = payment.FraudStatus,
                    FraudScore = payment.FraudScore,
                    FraudReasons = payment.FraudReasons
                });
            }

            if (settings == null || !settings.StripeEnabled)
            {
                return BadRequest(new
                {
                    error = "Payment Provider Disabled",
                    message = "Online payment is currently unavailable. Please use Pay Later."
                });
            }

            var usePreAuthorization = settings.PreAuthorizationEnabled;
            var preAuthAmount = CalculatePreAuthorizationAmount(amount, settings);
            var intentAmount = usePreAuthorization ? preAuthAmount : amount;
            var paymentIntent = await _paymentProvider.CreatePaymentIntentAsync(
                intentAmount, request.Currency, job.Id, manualCapture: usePreAuthorization);

            payment.StripePaymentIntentId = paymentIntent.IntentId;
            payment.PaymentErrorMessage = null;
            payment.IsPreAuthorization = usePreAuthorization;
            payment.AuthorizedAmount = intentAmount;
            payment.CaptureStatus = usePreAuthorization
                ? PaymentLifecycle.CaptureStatuses.PendingAuthorization
                : PaymentLifecycle.CaptureStatuses.NotApplicable;
            payment.PaymentStatus = usePreAuthorization
                ? PaymentLifecycle.Statuses.CapturePending
                : PaymentLifecycle.Statuses.Pending;
            job.PaymentStatus = payment.PaymentStatus;
            await _context.SaveChangesAsync();

            return Ok(new CreateOrderResponse
            {
                JobId = job.Id,
                PaymentId = payment.Id,
                PaymentStatus = payment.PaymentStatus,
                PaymentDueMode = paymentDueMode,
                Amount = intentAmount,
                Currency = request.Currency,
                ClientSecret = paymentIntent.ClientSecret,
                PaymentIntentId = paymentIntent.IntentId,
                PublishableKey = paymentIntent.PublishableKey,
                IsPreAuthorization = usePreAuthorization,
                AuthorizedAmount = payment.AuthorizedAmount,
                FraudStatus = payment.FraudStatus,
                FraudScore = payment.FraudScore,
                FraudReasons = payment.FraudReasons
            });
        }
        catch (PaymentProviderException ex)
        {
            _logger.LogError(ex, "{Provider} error creating pay-now order.", ex.ProviderName);
            return StatusCode(502, new
            {
                error = "Payment Provider Error",
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, new
            {
                error = "Internal Server Error",
                message = "An error occurred while creating the order."
            });
        }
    }

    private static string NormalizePaymentDueMode(string? paymentDueMode)
    {
        if (string.Equals(paymentDueMode, "PayNow", StringComparison.OrdinalIgnoreCase))
        {
            return "PayNow";
        }

        return "PayLater";
    }

    private static string NormalizePaymentMethod(string? paymentMethod, string paymentDueMode)
    {
        if (string.Equals(paymentDueMode, "PayNow", StringComparison.OrdinalIgnoreCase))
        {
            return "Card";
        }

        if (string.Equals(paymentMethod, "Cash", StringComparison.OrdinalIgnoreCase))
        {
            return "Cash";
        }

        if (string.Equals(paymentMethod, "Card", StringComparison.OrdinalIgnoreCase))
        {
            return "Card";
        }

        return "PaymentLink";
    }

    private static string? BuildAddress(string? line1, string? city, string? state, string? zip)
    {
        var parts = new[] { line1, city, state, zip }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        return parts.Length == 0 ? null : string.Join(", ", parts);
    }

    private async Task<ApplicationUser> FindOrCreateGuestCustomerAsync(CreateOrderRequest request, string contactName)
    {
        ApplicationUser? customer = await _context.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.ContactPhone);

        if (customer == null && !string.IsNullOrWhiteSpace(request.ContactEmail))
        {
            customer = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.ContactEmail);
        }

        if (customer != null)
        {
            return customer;
        }

        var userRoleId = UserRoles.GetRoleId(UserRoles.User);
        var userRole = await _roleManager.FindByIdAsync(userRoleId);
        if (userRole == null)
        {
            throw new InvalidOperationException("User role not found.");
        }

        var safePhone = request.ContactPhone.Replace(" ", "").Replace("-", "");
        var username = safePhone;
        var usernameExists = await _userManager.FindByNameAsync(username);
        if (usernameExists != null)
        {
            username = $"{safePhone}_{Guid.NewGuid():N}".Substring(0, Math.Min(32, safePhone.Length + 8));
        }

        customer = new ApplicationUser
        {
            UserName = username,
            Email = request.ContactEmail,
            FullName = contactName,
            PhoneNumber = request.ContactPhone,
            RoleId = userRoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            SmsOptIn = request.SmsOptIn,
            SmsOptInUpdatedAtUtc = request.SmsOptIn ? DateTime.UtcNow : (DateTime?)null
        };

        var result = await _userManager.CreateAsync(customer, GenerateRandomPassword());
        if (!result.Succeeded)
        {
            var reason = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create guest customer: {reason}");
        }

        return customer;
    }

    private async Task<Vehicle> FindOrCreateVehicleAsync(ApplicationUser customer, CreateOrderRequest request)
    {
        Vehicle? vehicle = null;
        if (!string.IsNullOrWhiteSpace(request.Vin))
        {
            vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.VIN == request.Vin);
        }

        if (vehicle != null)
        {
            return vehicle;
        }

        vehicle = new Vehicle
        {
            OwnerId = customer.Id,
            VIN = string.IsNullOrWhiteSpace(request.Vin) ? $"GUEST-{Guid.NewGuid():N}".Substring(0, 17) : request.Vin,
            Make = string.IsNullOrWhiteSpace(request.VehicleMake) ? "Unknown" : request.VehicleMake,
            Model = string.IsNullOrWhiteSpace(request.VehicleModel) ? "Unknown" : request.VehicleModel,
            Year = request.VehicleYear <= 0 ? DateTime.UtcNow.Year : request.VehicleYear,
            Color = request.VehicleColor ?? string.Empty,
            LicensePlate = request.LicensePlate,
            VehicleType = request.VehicleType
        };

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();
        return vehicle;
    }

    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 16).Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private static decimal CalculatePreAuthorizationAmount(decimal amount, SystemSettings settings)
    {
        if (amount < settings.PreAuthorizationMinAmount)
        {
            return settings.PreAuthorizationMinAmount;
        }

        if (amount > settings.PreAuthorizationMaxAmount)
        {
            return settings.PreAuthorizationMaxAmount;
        }

        return amount;
    }

    private async Task<(bool UnderReview, int Score, string Reasons)> EvaluateFraudRiskAsync(
        CreateOrderRequest request,
        decimal amount,
        SystemSettings? settings)
    {
        var score = 0;
        var reasons = new List<string>();

        var duplicateWindowMinutes = settings?.DuplicateRequestWindowMinutes ?? 30;
        var windowStart = DateTime.UtcNow.AddMinutes(-duplicateWindowMinutes);

        var duplicateCount = await _context.Jobs
            .CountAsync(j =>
                j.ContactPhoneNumber == request.ContactPhone &&
                j.CreatedAt >= windowStart);

        if (duplicateCount >= 2)
        {
            score += 40;
            reasons.Add("Duplicate request pattern detected.");
        }

        if (string.IsNullOrWhiteSpace(request.ContactEmail))
        {
            score += 10;
            reasons.Add("Missing contact email.");
        }

        if (!string.IsNullOrWhiteSpace(request.DestinationAddress) &&
            string.Equals(request.PickupAddress?.Trim(), request.DestinationAddress.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            score += 20;
            reasons.Add("Pickup and destination are identical.");
        }

        if (amount >= (settings?.PreAuthorizationMaxAmount ?? 300m))
        {
            score += 10;
            reasons.Add("High-value transaction.");
        }

        var threshold = settings?.FraudReviewScoreThreshold ?? 60;
        return (score >= threshold, score, string.Join(" ", reasons));
    }
}

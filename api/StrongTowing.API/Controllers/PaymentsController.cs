using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.DTOs.Payments;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Application.Exceptions;
using StrongTowing.Core.Entities;
using StrongTowing.Core.Constants;
using StrongTowing.Infrastructure.Data;
using StrongTowing.API.Services;
using StrongTowing.Application.Abstractions;
using System.Security.Claims;
using System.Text.Json;

namespace StrongTowing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PaymentsController> _logger;
    private readonly IPaymentProvider _paymentProvider;
    private readonly ISmsNotificationService _smsNotificationService;
    private readonly IEmailNotificationService _emailNotificationService;

    public PaymentsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<PaymentsController> logger,
        IPaymentProvider paymentProvider,
        ISmsNotificationService smsNotificationService,
        IEmailNotificationService emailNotificationService)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _paymentProvider = paymentProvider;
        _smsNotificationService = smsNotificationService;
        _emailNotificationService = emailNotificationService;
    }

    /// <summary>
    /// Get payments with optional filters and pagination (Admin/Dispatcher only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<PagedResponse<PaymentListItemDto>>> GetAllPayments(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? paymentMethod = null,
        [FromQuery] string? paymentStatus = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        [FromQuery] string? driverId = null,
        [FromQuery] string? searchTerm = null)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 25;
            if (pageSize > 100) pageSize = 100;

            var query = _context.Payments
                .AsNoTracking()
                .Include(p => p.Job)
                    .ThenInclude(j => j.Vehicle)
                        .ThenInclude(v => v.Owner)
                .Include(p => p.Job)
                    .ThenInclude(j => j.Driver)
                .AsQueryable();

            if (!string.IsNullOrEmpty(paymentMethod))
                query = query.Where(p => p.PaymentMethod == paymentMethod);

            if (!string.IsNullOrEmpty(paymentStatus))
                query = query.Where(p => p.PaymentStatus == paymentStatus);

            if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var start))
                query = query.Where(p => p.CreatedAt >= start);

            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var end))
                query = query.Where(p => p.CreatedAt <= end.AddDays(1));

            if (!string.IsNullOrEmpty(driverId))
                query = query.Where(p => p.Job!.DriverId == driverId);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var search = searchTerm.ToLower();
                query = query.Where(p =>
                    p.Job!.Id.ToString().Contains(search) ||
                    (p.Job.Vehicle.Owner != null && p.Job.Vehicle.Owner.FullName.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(p.TransactionId) && p.TransactionId.ToLower().Contains(search))
                );
            }

            var totalCount = await query.CountAsync();

            var payments = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = payments.Select(MapToPaymentListItemDto).ToList();

            return Ok(new PagedResponse<PaymentListItemDto>
            {
                Data = data,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while retrieving payments." });
        }
    }

    /// <summary>
    /// Get payment by ID (Admin/Dispatcher only)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<PaymentDto>> GetPaymentById(int id)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Job)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound(new { error = "Not Found", message = $"Payment with ID {id} was not found." });

            return Ok(MapToPaymentDto(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while retrieving payment." });
        }
    }

    /// <summary>
    /// Get payment by job ID (Admin/Dispatcher only)
    /// </summary>
    [HttpGet("job/{jobId}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<PaymentDto>> GetPaymentByJobId(int jobId)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Job)
                .FirstOrDefaultAsync(p => p.JobId == jobId);

            if (payment == null)
                return NotFound(new { error = "Not Found", message = $"Payment for job ID {jobId} was not found." });

            return Ok(MapToPaymentDto(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment by job ID");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while retrieving payment." });
        }
    }

    /// <summary>
    /// Get payment statistics (Admin/Dispatcher only)
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<PaymentStatisticsDto>> GetPaymentStatistics(
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        try
        {
            var query = _context.Payments.AsQueryable();

            if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var start))
                query = query.Where(p => p.CreatedAt >= start);

            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var end))
                query = query.Where(p => p.CreatedAt <= end.AddDays(1));

            var payments = await query.ToListAsync();

            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            var commissionPercentage = settings?.DriverCommissionPercentage ?? 30.00m;

            var statistics = new PaymentStatisticsDto
            {
                TotalPayments = payments.Count,
                TotalRevenue = payments.Where(p => p.PaymentStatus == PaymentLifecycle.Statuses.Paid).Sum(p => p.Amount),
                RevenueByMethod = new RevenueByMethodDto
                {
                    Card = payments.Where(p => p.PaymentMethod == PaymentLifecycle.Methods.Card && p.PaymentStatus == PaymentLifecycle.Statuses.Paid).Sum(p => p.Amount),
                    PaymentLink = payments.Where(p => p.PaymentMethod == PaymentLifecycle.Methods.PaymentLink && p.PaymentStatus == PaymentLifecycle.Statuses.Paid).Sum(p => p.Amount),
                    Cash = payments.Where(p => p.PaymentMethod == PaymentLifecycle.Methods.Cash && p.PaymentStatus == PaymentLifecycle.Statuses.Paid).Sum(p => p.Amount)
                },
                PendingPayments = payments.Count(p => p.PaymentStatus == PaymentLifecycle.Statuses.Pending || p.PaymentStatus == PaymentLifecycle.Statuses.PendingCash),
                TotalCashCollected = payments.Where(p => p.PaymentMethod == PaymentLifecycle.Methods.Cash && p.PaymentStatus == PaymentLifecycle.Statuses.Paid).Sum(p => p.Amount),
                TotalDriverCommissions = payments
                    .Where(p => p.PaymentStatus == PaymentLifecycle.Statuses.Paid)
                    .Sum(p => p.Amount * (commissionPercentage / 100)),
                DriverCommissionRatePercent = commissionPercentage
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment statistics");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while retrieving payment statistics." });
        }
    }

    /// <summary>
    /// Get payments waiting for fraud review (Admin/Dispatcher only)
    /// </summary>
    [HttpGet("fraud-review-queue")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<IEnumerable<PaymentListItemDto>>> GetFraudReviewQueue()
    {
        var queue = await _context.Payments
            .Include(p => p.Job)
                .ThenInclude(j => j.Vehicle)
                    .ThenInclude(v => v.Owner)
            .Include(p => p.Job)
                .ThenInclude(j => j.Driver)
            .Where(p => p.FraudStatus == PaymentLifecycle.FraudStatuses.UnderReview
                     || p.PaymentStatus == PaymentLifecycle.Statuses.UnderReview)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Ok(queue.Select(MapToPaymentListItemDto).ToList());
    }

    /// <summary>
    /// Approve or reject a payment under fraud review.
    /// </summary>
    [HttpPost("{id}/review-decision")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
    public async Task<ActionResult<PaymentDto>> ReviewPayment(int id, [FromBody] ReviewPaymentRequest request)
    {
        var payment = await _context.Payments
            .Include(p => p.Job)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
            return NotFound(new { error = "Not Found", message = $"Payment with ID {id} was not found." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var approve = string.Equals(request.Decision, "approve", StringComparison.OrdinalIgnoreCase);
        var reject = string.Equals(request.Decision, "reject", StringComparison.OrdinalIgnoreCase);
        if (!approve && !reject)
            return BadRequest(new { error = "Bad Request", message = "Decision must be either approve or reject." });

        payment.FraudReviewedBy = userId;
        payment.FraudReviewedAt = DateTime.UtcNow;
        payment.FraudReasons = $"{payment.FraudReasons} {(string.IsNullOrWhiteSpace(request.Notes) ? "" : $" ReviewNotes: {request.Notes}")}".Trim();

        if (approve)
        {
            payment.FraudStatus = PaymentLifecycle.FraudStatuses.Approved;
            if (payment.PaymentStatus == PaymentLifecycle.Statuses.UnderReview)
            {
                payment.PaymentStatus = payment.IsPreAuthorization
                    ? PaymentLifecycle.Statuses.CapturePending
                    : PaymentLifecycle.Statuses.Pending;
            }
            if (payment.Job != null && payment.Job.PaymentStatus == PaymentLifecycle.Statuses.UnderReview)
            {
                payment.Job.PaymentStatus = payment.PaymentStatus;
            }
        }
        else
        {
            payment.FraudStatus = PaymentLifecycle.FraudStatuses.Rejected;
            payment.PaymentStatus = PaymentLifecycle.Statuses.Cancelled;
            if (payment.Job != null)
            {
                payment.Job.PaymentStatus = PaymentLifecycle.Statuses.Cancelled;
            }
        }

        await _context.SaveChangesAsync();
        return Ok(MapToPaymentDto(payment));
    }

    /// <summary>
    /// Record a manual (non-provider) payment, e.g. Cash (Admin/Dispatcher only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<PaymentDto>> ProcessPayment([FromBody] ProcessPaymentRequest request)
    {
        try
        {
            var job = await _context.Jobs
                .Include(j => j.Vehicle)
                .FirstOrDefaultAsync(j => j.Id == request.JobId);

            if (job == null)
                return NotFound(new { error = "Not Found", message = $"Job with ID {request.JobId} was not found." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var normalizedMethod = NormalizePaymentMethod(request.PaymentMethod);
            var initialStatus = normalizedMethod == "Cash" ? "PendingCash" : "Paid";

            var payment = new Payment
            {
                JobId = request.JobId,
                Amount = request.Amount,
                PaymentMethod = normalizedMethod,
                PaymentStatus = initialStatus,
                ProcessedBy = userId,
                ProcessedAt = initialStatus == "Paid" ? DateTime.UtcNow : null,
                TransactionId = request.TransactionId,
                StripePaymentIntentId = request.PaymentIntentId
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            job.PaymentStatus = initialStatus;
            job.PaymentId = payment.Id;
            job.PaidAt = initialStatus == "Paid" ? DateTime.UtcNow : null;
            job.PaidBy = initialStatus == "Paid" ? userId : null;
            job.PaymentMethod = normalizedMethod;
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPaymentById), new { id = payment.Id }, MapToPaymentDto(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while processing payment." });
        }
    }

    /// <summary>
    /// Mark a payment as cash-selected/pending collection (Admin/Dispatcher only)
    /// </summary>
    [HttpPost("{id}/mark-cash-pending")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<PaymentDto>> MarkCashPending(int id)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Job)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound(new { error = "Not Found", message = $"Payment with ID {id} was not found." });

            payment.PaymentMethod = "Cash";
            payment.PaymentStatus = "PendingCash";
            payment.ProcessedAt = null;

            if (payment.Job != null)
            {
                payment.Job.PaymentMethod = "Cash";
                payment.Job.PaymentStatus = "PendingCash";
                payment.Job.PaidAt = null;
                payment.Job.PaidBy = null;
            }

            await _context.SaveChangesAsync();
            return Ok(MapToPaymentDto(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking payment {PaymentId} as cash pending", id);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while updating payment." });
        }
    }

    /// <summary>
    /// Confirm cash was collected and settle payment (Admin/Dispatcher only)
    /// </summary>
    [HttpPost("{id}/mark-cash-collected")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<PaymentDto>> MarkCashCollected(int id)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Job)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound(new { error = "Not Found", message = $"Payment with ID {id} was not found." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            payment.PaymentMethod = "Cash";
            payment.PaymentStatus = PaymentLifecycle.Statuses.Paid;
            payment.CashCollectedBy = userId;
            payment.CashCollectedAt = DateTime.UtcNow;
            payment.ProcessedBy = userId;
            payment.ProcessedAt = DateTime.UtcNow;
            payment.PaymentErrorMessage = null;

            if (payment.Job != null)
            {
                payment.Job.PaymentMethod = "Cash";
                payment.Job.PaymentStatus = PaymentLifecycle.Statuses.Paid;
                payment.Job.PaymentId = payment.Id;
                payment.Job.PaidBy = userId;
                payment.Job.PaidAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(MapToPaymentDto(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking payment {PaymentId} as cash collected", id);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while updating payment." });
        }
    }

    /// <summary>
    /// Cancel a payment if it is not already settled (Admin/Dispatcher only)
    /// </summary>
    [HttpPost("{id}/cancel")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<PaymentDto>> CancelPayment(int id)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Job)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound(new { error = "Not Found", message = $"Payment with ID {id} was not found." });

            if (payment.PaymentStatus == "Paid")
                return BadRequest(new { error = "Invalid State", message = "Paid payments cannot be cancelled." });

            payment.PaymentStatus = "Cancelled";
            payment.PaymentErrorMessage = null;

            if (payment.Job != null && payment.Job.PaymentStatus != "Paid")
            {
                payment.Job.PaymentStatus = PaymentLifecycle.Statuses.Cancelled;
            }

            await _context.SaveChangesAsync();
            return Ok(MapToPaymentDto(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payment {PaymentId}", id);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while cancelling payment." });
        }
    }

    // ─── Payment Provider Endpoints ──────────────────────────────────────────

    /// <summary>
    /// Create a payment intent and return the client secret to the frontend SDK (Admin/Dispatcher only)
    /// </summary>
    [HttpPost("create-payment-intent")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<CreatePaymentIntentResponse>> CreatePaymentIntent(
        [FromBody] CreatePaymentIntentRequest request)
    {
        try
        {
            var job = await _context.Jobs.FindAsync(request.JobId);
            if (job == null)
                return NotFound(new { error = "Not Found", message = $"Job with ID {request.JobId} was not found." });

            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            if (settings == null || !settings.StripeEnabled)
                return BadRequest(new { error = "Payment Provider Disabled", message = "The payment provider is not enabled. Please configure it in Settings." });

            var result = await _paymentProvider.CreatePaymentIntentAsync(
                request.Amount, request.Currency, request.JobId, request.ManualCapture);

            return Ok(new CreatePaymentIntentResponse
            {
                ClientSecret = result.ClientSecret,
                PaymentIntentId = result.IntentId,
                PublishableKey = result.PublishableKey,
                Amount = request.Amount,
                Currency = request.Currency,
                ManualCapture = result.ManualCapture,
                CapturableAmount = result.CapturableAmount,
                AuthorizationExpiresAt = result.AuthorizationExpiresAt,
                RiskLevel = result.RiskLevel
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Payment provider configuration error");
            return BadRequest(new { error = "Configuration Error", message = ex.Message });
        }
        catch (PaymentProviderException ex)
        {
            _logger.LogError(ex, "{Provider} error creating payment intent", ex.ProviderName);
            return StatusCode(502, new { error = "Payment Provider Error", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while creating the payment intent." });
        }
    }

    /// <summary>
    /// Create a hosted payment link for a job (Admin/Dispatcher only)
    /// </summary>
    [HttpPost("create-payment-link")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<CreateStripePaymentLinkResponse>> CreatePaymentLink(
        [FromBody] CreateStripePaymentLinkRequest request)
    {
        try
        {
            var job = await _context.Jobs.FindAsync(request.JobId);
            if (job == null)
                return NotFound(new { error = "Not Found", message = $"Job with ID {request.JobId} was not found." });

            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            if (settings == null || !settings.StripeEnabled)
                return BadRequest(new { error = "Payment Provider Disabled", message = "The payment provider is not enabled. Please configure it in Settings." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = await _paymentProvider.CreatePaymentLinkAsync(
                request.Amount, request.JobId, request.SuccessUrl);

            var payment = new Payment
            {
                JobId = request.JobId,
                Amount = request.Amount,
                PaymentMethod = "PaymentLink",
                PaymentStatus = "Pending",
                StripePaymentLinkId = result.LinkId,
                StripePaymentLinkUrl = result.Url,
                ProcessedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Surface payment state on the job for dispatch/admin UIs (webhook sets Paid when client completes checkout).
            if (job.PaymentStatus != PaymentLifecycle.Statuses.Paid
                && job.PaymentStatus != PaymentLifecycle.Statuses.Refunded
                && job.PaymentStatus != PaymentLifecycle.Statuses.PartiallyRefunded)
            {
                job.PaymentStatus = PaymentLifecycle.Statuses.Pending;
                job.PaymentMethod = PaymentLifecycle.Methods.PaymentLink;
                job.PaymentId = payment.Id;
            }

            await _context.SaveChangesAsync();

            await _context.Entry(job).Reference(j => j.Vehicle).LoadAsync();
            if (job.Vehicle != null)
                await _context.Entry(job.Vehicle).Reference(v => v.Owner).LoadAsync();
            await _smsNotificationService.NotifyClientPaymentLinkCreatedAsync(
                job.Id,
                request.Amount,
                result.Url,
                job.ContactPhoneNumber,
                job.Vehicle?.Owner?.PhoneNumber);
            await _emailNotificationService.NotifyClientPaymentLinkCreatedAsync(
                job.Id,
                request.Amount,
                result.Url,
                null,
                job.Vehicle?.Owner?.Email);

            return Ok(new CreateStripePaymentLinkResponse
            {
                Url = result.Url,
                StripePaymentLinkId = result.LinkId,
                PaymentRecordId = payment.Id,
                Amount = request.Amount
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Payment provider configuration error");
            return BadRequest(new { error = "Configuration Error", message = ex.Message });
        }
        catch (PaymentProviderException ex)
        {
            _logger.LogError(ex, "{Provider} error creating payment link", ex.ProviderName);
            return StatusCode(502, new { error = "Payment Provider Error", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment link");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while creating the payment link." });
        }
    }

    /// <summary>
    /// Capture all or part of a pre-authorized payment intent (Admin/Dispatcher only)
    /// </summary>
    [HttpPost("{id}/capture-authorization")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<PaymentDto>> CaptureAuthorization(int id, [FromBody] CaptureAuthorizationRequest request)
    {
        var payment = await _context.Payments
            .Include(p => p.Job)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
            return NotFound(new { error = "Not Found", message = $"Payment with ID {id} was not found." });

        if (string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
            return BadRequest(new { error = "Bad Request", message = "Payment does not have an authorization intent ID." });

        if (!payment.IsPreAuthorization)
            return BadRequest(new { error = "Bad Request", message = "Payment is not a pre-authorization record." });

        var captureResult = await _paymentProvider.CapturePaymentIntentAsync(payment.StripePaymentIntentId, request.Amount);
        var capturedAmount = request.Amount ?? captureResult.Amount;

        payment.CapturedAmount = capturedAmount;
        payment.CapturedAt = DateTime.UtcNow;
        payment.CaptureStatus = capturedAmount < (payment.AuthorizedAmount ?? capturedAmount)
            ? PaymentLifecycle.CaptureStatuses.PartiallyCaptured
            : PaymentLifecycle.CaptureStatuses.Captured;
        payment.PaymentStatus = PaymentLifecycle.Statuses.Paid;
        payment.ProcessedAt = DateTime.UtcNow;
        payment.PaymentErrorMessage = null;

        if (payment.Job != null)
        {
            payment.Job.PaymentStatus = PaymentLifecycle.Statuses.Paid;
            payment.Job.PaidAt = DateTime.UtcNow;
            payment.Job.PaymentId = payment.Id;
        }

        await _context.SaveChangesAsync();
        return Ok(MapToPaymentDto(payment));
    }

    /// <summary>
    /// Release/void a pre-authorization hold (Admin/Dispatcher only)
    /// </summary>
    [HttpPost("{id}/release-authorization")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<PaymentDto>> ReleaseAuthorization(int id)
    {
        var payment = await _context.Payments
            .Include(p => p.Job)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
            return NotFound(new { error = "Not Found", message = $"Payment with ID {id} was not found." });

        if (string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
            return BadRequest(new { error = "Bad Request", message = "Payment does not have an authorization intent ID." });

        await _paymentProvider.CancelPaymentIntentAsync(payment.StripePaymentIntentId);
        payment.CaptureStatus = PaymentLifecycle.CaptureStatuses.Released;
        payment.ReleasedAt = DateTime.UtcNow;
        payment.PaymentStatus = PaymentLifecycle.Statuses.Cancelled;

        if (payment.Job != null && payment.Job.PaymentStatus != PaymentLifecycle.Statuses.Paid)
        {
            payment.Job.PaymentStatus = PaymentLifecycle.Statuses.Cancelled;
        }

        await _context.SaveChangesAsync();
        return Ok(MapToPaymentDto(payment));
    }

    /// <summary>
    /// Issue a full or partial refund for a payment (Admin only)
    /// </summary>
    [HttpPost("{id}/refund")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
    public async Task<ActionResult<RefundPaymentResponse>> RefundPayment(
        int id, [FromBody] RefundPaymentRequest request)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Job)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound(new { error = "Not Found", message = $"Payment with ID {id} was not found." });

            if (payment.PaymentStatus == "Refunded")
                return BadRequest(new { error = "Already Refunded", message = "This payment has already been refunded." });

            if (payment.PaymentStatus != "Paid")
                return BadRequest(new { error = "Not Paid", message = "Only paid payments can be refunded." });

            if (string.IsNullOrEmpty(payment.StripePaymentIntentId))
                return BadRequest(new { error = "No Transaction ID", message = "This payment does not have an associated transaction ID and cannot be refunded through the payment provider." });

            var result = await _paymentProvider.RefundAsync(
                payment.StripePaymentIntentId, request.Amount, request.Reason);

            var refundAmount = request.Amount ?? payment.Amount;
            var isFullRefund = !request.Amount.HasValue || request.Amount >= payment.Amount;

            payment.PaymentStatus = isFullRefund ? "Refunded" : "PartiallyRefunded";
            payment.RefundedAt = DateTime.UtcNow;
            payment.RefundReason = request.Reason;
            payment.RefundAmount = refundAmount;

            if (isFullRefund && payment.Job != null)
                payment.Job.PaymentStatus = PaymentLifecycle.Statuses.Refunded;

            await _context.SaveChangesAsync();

            return Ok(new RefundPaymentResponse
            {
                RefundId = result.RefundId,
                Amount = refundAmount,
                Status = result.Status,
                PaymentId = payment.Id,
                Reason = request.Reason
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Payment provider configuration error");
            return BadRequest(new { error = "Configuration Error", message = ex.Message });
        }
        catch (PaymentProviderException ex)
        {
            _logger.LogError(ex, "{Provider} error processing refund for payment {PaymentId}", ex.ProviderName, id);
            return StatusCode(502, new { error = "Payment Provider Error", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId}", id);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while processing the refund." });
        }
    }

    /// <summary>
    /// Webhook endpoint — verifies the provider signature and processes the inbound event.
    /// Must NOT be behind JWT authentication.
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        // Dynamically read whichever signature header the active provider expects
        var signature = Request.Headers[_paymentProvider.WebhookSignatureHeaderName].FirstOrDefault();

        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("Webhook received without {Header} header.", _paymentProvider.WebhookSignatureHeaderName);
            return BadRequest(new { error = $"Missing {_paymentProvider.WebhookSignatureHeaderName} header." });
        }

        var settings = await _context.SystemSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            _logger.LogError("System settings are not configured.");
            return StatusCode(500, new { error = "System settings not configured." });
        }

        var encryptionService = HttpContext.RequestServices.GetRequiredService<IEncryptionService>();

        // Resolve the active webhook secret based on the current Stripe mode
        var isLive = string.Equals(settings.StripeMode, "live", StringComparison.OrdinalIgnoreCase);
        var encryptedWebhookSecret = isLive
            ? (settings.StripeLiveWebhookSecret ?? settings.StripeWebhookSecret)
            : (settings.StripeTestWebhookSecret ?? settings.StripeWebhookSecret);

        if (string.IsNullOrEmpty(encryptedWebhookSecret))
        {
            _logger.LogError("Webhook secret is not configured for {Mode} mode.", isLive ? "live" : "test");
            return StatusCode(500, new { error = "Webhook secret not configured." });
        }

        string webhookSecret;
        try
        {
            webhookSecret = encryptionService.Decrypt(encryptedWebhookSecret);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt webhook secret.");
            return StatusCode(500, new { error = "Internal configuration error." });
        }

        WebhookEventResult webhookEvent;
        try
        {
            webhookEvent = _paymentProvider.ParseWebhookEvent(json, signature, webhookSecret);
        }
        catch (PaymentProviderException ex)
        {
            _logger.LogWarning(ex, "{Provider} webhook signature verification failed.", ex.ProviderName);
            var reason = string.IsNullOrWhiteSpace(ex.Message)
                ? "Webhook signature verification failed."
                : ex.Message;
            await PersistWebhookErrorFromPayloadAsync(json, reason, ex.ErrorCode);
            return BadRequest(new
            {
                error = "Webhook signature verification failed.",
                code = ex.ErrorCode ?? "webhook_signature_verification_failed",
                reason
            });
        }

        try
        {
            await HandleWebhookEvent(webhookEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook event {EventType}", webhookEvent.EventType);
            await PersistWebhookErrorAsync(webhookEvent, ex.Message);
            return StatusCode(500, new
            {
                error = "Webhook processing failed.",
                eventType = webhookEvent.EventType,
                rawProviderEventType = webhookEvent.RawProviderEventType,
                sessionId = webhookEvent.SessionId,
                transactionId = webhookEvent.TransactionId,
                paymentLinkId = webhookEvent.PaymentLinkId,
                jobId = webhookEvent.JobId,
                details = ex.Message
            });
        }

        return Ok(new { received = true });
    }

    // ─── Webhook event handler ────────────────────────────────────────────────

    private async Task HandleWebhookEvent(WebhookEventResult webhookEvent)
    {
        switch (webhookEvent.EventType)
        {
            case WebhookEventResult.PaymentAuthorized:
                await HandlePaymentAuthorized(webhookEvent);
                break;

            case WebhookEventResult.PaymentSucceeded:
                await HandlePaymentSucceeded(webhookEvent);
                break;

            case WebhookEventResult.PaymentFailed:
                await HandlePaymentFailed(webhookEvent);
                break;

            case WebhookEventResult.PaymentLinkCompleted:
                await HandlePaymentLinkCompleted(webhookEvent);
                break;

            case WebhookEventResult.PaymentRefunded:
                await HandlePaymentRefunded(webhookEvent);
                break;

            default:
                _logger.LogInformation("Unhandled webhook event type: {EventType}", webhookEvent.EventType);
                break;
        }
    }

    private async Task HandlePaymentAuthorized(WebhookEventResult webhookEvent)
    {
        if (string.IsNullOrEmpty(webhookEvent.TransactionId)) return;

        var payment = await _context.Payments
            .Include(p => p.Job)
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == webhookEvent.TransactionId);

        if (payment == null)
        {
            return;
        }

        payment.CaptureStatus = PaymentLifecycle.CaptureStatuses.Authorized;
        payment.PaymentStatus = PaymentLifecycle.Statuses.Authorized;
        payment.AuthorizedAmount = webhookEvent.AmountPaid ?? payment.AuthorizedAmount ?? payment.Amount;
        payment.PaymentErrorMessage = null;

        if (payment.Job != null)
        {
            payment.Job.PaymentStatus = PaymentLifecycle.Statuses.Authorized;
            payment.Job.PaymentId = payment.Id;
        }

        await _context.SaveChangesAsync();
    }

    private async Task HandlePaymentSucceeded(WebhookEventResult webhookEvent)
    {
        if (string.IsNullOrEmpty(webhookEvent.TransactionId)) return;

        PaymentLinkCorrelationResult? correlation = null;
        var payment = await _context.Payments
            .Include(p => p.Job)
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == webhookEvent.TransactionId);

        // Fallback: payment link flow may only correlate by jobId metadata on first success callback.
        if (payment == null && webhookEvent.JobId.HasValue)
        {
            payment = await _context.Payments
                .Include(p => p.Job)
                .Where(p =>
                    p.JobId == webhookEvent.JobId.Value &&
                    p.PaymentMethod == PaymentLifecycle.Methods.PaymentLink &&
                    p.PaymentStatus == "Pending")
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }

        // Fallback: if we only received payment_intent.* events, resolve Checkout Session data from Stripe.
        if (payment == null)
        {
            try
            {
                correlation = await _paymentProvider.ResolvePaymentLinkCorrelationAsync(webhookEvent.TransactionId);
            }
            catch (PaymentProviderException ex)
            {
                _logger.LogWarning(ex, "{Provider} could not resolve payment-link correlation for transaction {TransactionId}.", ex.ProviderName, webhookEvent.TransactionId);
            }

            if (correlation != null && !string.IsNullOrEmpty(correlation.PaymentLinkId))
            {
                payment = await _context.Payments
                    .Include(p => p.Job)
                    .FirstOrDefaultAsync(p => p.StripePaymentLinkId == correlation.PaymentLinkId);
            }

            if (payment == null && correlation?.JobId.HasValue == true)
            {
                payment = await _context.Payments
                    .Include(p => p.Job)
                    .Where(p =>
                        p.JobId == correlation.JobId.Value &&
                        p.PaymentMethod == PaymentLifecycle.Methods.PaymentLink &&
                        p.PaymentStatus == "Pending")
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync();
            }
        }

        if (payment == null)
        {
            _logger.LogWarning("No payment record found for succeeded transaction {TransactionId}.", webhookEvent.TransactionId);
            return;
        }

        var previousPaymentStatus = payment.PaymentStatus;

        payment.PaymentStatus = PaymentLifecycle.Statuses.Paid;
        payment.ProcessedAt = DateTime.UtcNow;
        payment.TransactionId ??= webhookEvent.SessionId ?? correlation?.SessionId;
        payment.StripePaymentIntentId ??= webhookEvent.TransactionId;
        payment.StripeChargeId = webhookEvent.ChargeId;
        payment.CardLast4 = webhookEvent.CardLast4;
        payment.CardBrand = webhookEvent.CardBrand;
        payment.StripePaymentLinkId ??= webhookEvent.PaymentLinkId ?? correlation?.PaymentLinkId;
        payment.PaymentErrorMessage = null;

        if (payment.Job != null)
        {
            payment.Job.PaymentStatus = PaymentLifecycle.Statuses.Paid;
            payment.Job.PaymentId = payment.Id;
            payment.Job.PaidAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Marked payment as Paid for transaction {TransactionId}.", webhookEvent.TransactionId);

        if (previousPaymentStatus != PaymentLifecycle.Statuses.Paid && payment.Job != null)
        {
            await LoadJobVehicleOwnerAsync(payment.Job);
            await _smsNotificationService.NotifyClientPaymentSucceededAsync(
                payment.JobId,
                payment.Amount,
                payment.Job.ContactPhoneNumber,
                payment.Job.Vehicle?.Owner?.PhoneNumber);
            await _emailNotificationService.NotifyClientPaymentSucceededAsync(
                payment.JobId,
                payment.Amount,
                null,
                payment.Job.Vehicle?.Owner?.Email);
        }
    }

    private async Task HandlePaymentFailed(WebhookEventResult webhookEvent)
    {
        Payment? payment = null;
        PaymentLinkCorrelationResult? correlation = null;
        if (!string.IsNullOrEmpty(webhookEvent.TransactionId))
        {
            payment = await _context.Payments
                .Include(p => p.Job)
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == webhookEvent.TransactionId);
        }

        if (payment == null && webhookEvent.JobId.HasValue)
        {
            payment = await _context.Payments
                .Include(p => p.Job)
                .Where(p =>
                    p.JobId == webhookEvent.JobId.Value &&
                    p.PaymentMethod == PaymentLifecycle.Methods.PaymentLink &&
                    p.PaymentStatus == "Pending")
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }

        if (payment == null && !string.IsNullOrEmpty(webhookEvent.TransactionId))
        {
            try
            {
                correlation = await _paymentProvider.ResolvePaymentLinkCorrelationAsync(webhookEvent.TransactionId);
            }
            catch (PaymentProviderException ex)
            {
                _logger.LogWarning(ex, "{Provider} could not resolve failed payment-link correlation for transaction {TransactionId}.", ex.ProviderName, webhookEvent.TransactionId);
            }

            if (correlation != null && !string.IsNullOrEmpty(correlation.PaymentLinkId))
            {
                payment = await _context.Payments
                    .Include(p => p.Job)
                    .FirstOrDefaultAsync(p => p.StripePaymentLinkId == correlation.PaymentLinkId);
            }

            if (payment == null && correlation?.JobId.HasValue == true)
            {
                payment = await _context.Payments
                    .Include(p => p.Job)
                    .Where(p =>
                        p.JobId == correlation.JobId.Value &&
                        p.PaymentMethod == PaymentLifecycle.Methods.PaymentLink &&
                        p.PaymentStatus == "Pending")
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync();
            }
        }

        if (payment == null)
        {
            _logger.LogWarning("No payment record found for failed transaction {TransactionId}.", webhookEvent.TransactionId);
            return;
        }

        var previousFailedStatus = payment.PaymentStatus;

        payment.PaymentStatus = PaymentLifecycle.Statuses.Failed;
        payment.PaymentErrorMessage = webhookEvent.ErrorMessage;
        payment.StripePaymentIntentId ??= webhookEvent.TransactionId;
        payment.TransactionId ??= webhookEvent.SessionId ?? correlation?.SessionId;
        payment.StripePaymentLinkId ??= webhookEvent.PaymentLinkId ?? correlation?.PaymentLinkId;

        if (payment.Job != null && payment.Job.PaymentStatus != "Paid")
        {
            payment.Job.PaymentStatus = PaymentLifecycle.Statuses.Failed;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Marked payment as Failed for transaction {TransactionId}.", webhookEvent.TransactionId);

        if (previousFailedStatus != PaymentLifecycle.Statuses.Failed && payment.Job != null)
        {
            await LoadJobVehicleOwnerAsync(payment.Job);
            await _smsNotificationService.NotifyClientPaymentFailedAsync(
                payment.JobId,
                payment.Job.ContactPhoneNumber,
                payment.Job.Vehicle?.Owner?.PhoneNumber);
            await _emailNotificationService.NotifyClientPaymentFailedAsync(
                payment.JobId,
                null,
                payment.Job.Vehicle?.Owner?.Email);
        }
    }

    private async Task HandlePaymentLinkCompleted(WebhookEventResult webhookEvent)
    {
        var sessionPaymentStatus = webhookEvent.SessionPaymentStatus?.Trim().ToLowerInvariant();
        if (sessionPaymentStatus == WebhookEventResult.SessionPaymentUnpaid)
        {
            await PersistWebhookErrorAsync(webhookEvent, "Checkout session completed but payment_status is unpaid.");
            return;
        }

        Payment? payment = null;
        PaymentLinkCorrelationResult? correlation = null;
        if (!string.IsNullOrEmpty(webhookEvent.PaymentLinkId))
        {
            payment = await _context.Payments
                .Include(p => p.Job)
                .FirstOrDefaultAsync(p => p.StripePaymentLinkId == webhookEvent.PaymentLinkId);
        }

        if (payment == null && !string.IsNullOrEmpty(webhookEvent.TransactionId))
        {
            payment = await _context.Payments
                .Include(p => p.Job)
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == webhookEvent.TransactionId);
        }

        if (payment == null && !string.IsNullOrEmpty(webhookEvent.TransactionId))
        {
            try
            {
                correlation = await _paymentProvider.ResolvePaymentLinkCorrelationAsync(webhookEvent.TransactionId);
            }
            catch (PaymentProviderException ex)
            {
                _logger.LogWarning(ex, "{Provider} could not resolve completed payment-link correlation for transaction {TransactionId}.", ex.ProviderName, webhookEvent.TransactionId);
            }

            if (correlation != null && !string.IsNullOrEmpty(correlation.PaymentLinkId))
            {
                payment = await _context.Payments
                    .Include(p => p.Job)
                    .FirstOrDefaultAsync(p => p.StripePaymentLinkId == correlation.PaymentLinkId);
            }
        }

        // Fallback: match by jobId metadata if payment_link identifier is not present.
        if (payment == null && webhookEvent.JobId.HasValue)
        {
            payment = await _context.Payments
                .Include(p => p.Job)
                .Where(p =>
                    p.JobId == webhookEvent.JobId.Value &&
                    p.PaymentMethod == PaymentLifecycle.Methods.PaymentLink &&
                    p.PaymentStatus == "Pending")
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }

        if (payment == null && correlation?.JobId.HasValue == true)
        {
            payment = await _context.Payments
                .Include(p => p.Job)
                .Where(p =>
                    p.JobId == correlation.JobId.Value &&
                    p.PaymentMethod == PaymentLifecycle.Methods.PaymentLink &&
                    p.PaymentStatus == "Pending")
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }

        if (payment == null)
        {
            await PersistWebhookErrorAsync(webhookEvent, "No payment record found for completed payment link webhook event.");
            return;
        }

        var previousLinkPaymentStatus = payment.PaymentStatus;

        payment.PaymentStatus = PaymentLifecycle.Statuses.Paid;
        payment.ProcessedAt = DateTime.UtcNow;
        payment.TransactionId ??= webhookEvent.SessionId ?? correlation?.SessionId;
        payment.StripePaymentIntentId ??= webhookEvent.TransactionId;
        payment.StripePaymentLinkId ??= webhookEvent.PaymentLinkId ?? correlation?.PaymentLinkId;
        payment.PaymentErrorMessage = null;

        if (payment.Job != null)
        {
            payment.Job.PaymentStatus = PaymentLifecycle.Statuses.Paid;
            payment.Job.PaymentId = payment.Id;
            payment.Job.PaidAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Marked payment as Paid for payment link {PaymentLinkId}.", webhookEvent.PaymentLinkId);

        if (previousLinkPaymentStatus != PaymentLifecycle.Statuses.Paid && payment.Job != null)
        {
            await LoadJobVehicleOwnerAsync(payment.Job);
            await _smsNotificationService.NotifyClientPaymentSucceededAsync(
                payment.JobId,
                payment.Amount,
                payment.Job.ContactPhoneNumber,
                payment.Job.Vehicle?.Owner?.PhoneNumber);
            await _emailNotificationService.NotifyClientPaymentSucceededAsync(
                payment.JobId,
                payment.Amount,
                null,
                payment.Job.Vehicle?.Owner?.Email);
        }
    }

    private async Task LoadJobVehicleOwnerAsync(Job job)
    {
        if (job.Vehicle == null)
            await _context.Entry(job).Reference(j => j.Vehicle).LoadAsync();
        if (job.Vehicle != null)
            await _context.Entry(job.Vehicle).Reference(v => v.Owner).LoadAsync();
    }

    private async Task PersistWebhookErrorAsync(WebhookEventResult webhookEvent, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return;
        }

        Payment? payment = null;
        PaymentLinkCorrelationResult? correlation = null;

        if (!string.IsNullOrEmpty(webhookEvent.PaymentLinkId))
        {
            payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.StripePaymentLinkId == webhookEvent.PaymentLinkId);
        }

        if (payment == null && !string.IsNullOrEmpty(webhookEvent.TransactionId))
        {
            payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == webhookEvent.TransactionId);
        }

        if (payment == null && webhookEvent.JobId.HasValue)
        {
            payment = await _context.Payments
                .Where(p =>
                    p.JobId == webhookEvent.JobId.Value &&
                    p.PaymentMethod == PaymentLifecycle.Methods.PaymentLink)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }

        if (payment == null && !string.IsNullOrEmpty(webhookEvent.TransactionId))
        {
            try
            {
                correlation = await _paymentProvider.ResolvePaymentLinkCorrelationAsync(webhookEvent.TransactionId);
            }
            catch (PaymentProviderException)
            {
                // Best-effort persistence only. Ignore lookup failures here.
            }

            if (payment == null && correlation?.PaymentLinkId != null)
            {
                payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.StripePaymentLinkId == correlation.PaymentLinkId);
            }

            if (payment == null && correlation?.JobId.HasValue == true)
            {
                payment = await _context.Payments
                    .Where(p =>
                        p.JobId == correlation.JobId.Value &&
                        p.PaymentMethod == PaymentLifecycle.Methods.PaymentLink)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync();
            }
        }

        if (payment == null)
        {
            return;
        }

        payment.PaymentErrorMessage = $"Webhook: {errorMessage}";
        await _context.SaveChangesAsync();
    }

    private async Task PersistWebhookErrorFromPayloadAsync(string rawPayload, string errorMessage, string? errorCode)
    {
        if (string.IsNullOrWhiteSpace(rawPayload) || string.IsNullOrWhiteSpace(errorMessage))
        {
            return;
        }

        string? paymentIntentId = null;
        string? paymentLinkId = null;
        int? jobId = null;

        try
        {
            using var jsonDoc = JsonDocument.Parse(rawPayload);
            var root = jsonDoc.RootElement;
            if (!root.TryGetProperty("data", out var dataNode) ||
                !dataNode.TryGetProperty("object", out var objectNode))
            {
                return;
            }

            if (objectNode.TryGetProperty("payment_intent", out var paymentIntentProp) && paymentIntentProp.ValueKind == JsonValueKind.String)
            {
                paymentIntentId = paymentIntentProp.GetString();
            }

            if (objectNode.TryGetProperty("id", out var objectIdProp) && objectIdProp.ValueKind == JsonValueKind.String)
            {
                var objectId = objectIdProp.GetString();
                if (!string.IsNullOrWhiteSpace(objectId))
                {
                    if (objectId.StartsWith("pi_", StringComparison.Ordinal))
                    {
                        paymentIntentId ??= objectId;
                    }
                    else if (objectId.StartsWith("plink_", StringComparison.Ordinal))
                    {
                        paymentLinkId ??= objectId;
                    }
                }
            }

            if (objectNode.TryGetProperty("payment_link", out var paymentLinkProp) && paymentLinkProp.ValueKind == JsonValueKind.String)
            {
                paymentLinkId = paymentLinkProp.GetString();
            }

            if (objectNode.TryGetProperty("metadata", out var metadataProp) &&
                metadataProp.ValueKind == JsonValueKind.Object &&
                metadataProp.TryGetProperty("jobId", out var jobIdProp))
            {
                if (jobIdProp.ValueKind == JsonValueKind.String && int.TryParse(jobIdProp.GetString(), out var parsedJobId))
                {
                    jobId = parsedJobId;
                }
                else if (jobIdProp.ValueKind == JsonValueKind.Number && jobIdProp.TryGetInt32(out var numericJobId))
                {
                    jobId = numericJobId;
                }
            }
        }
        catch (JsonException)
        {
            return;
        }

        Payment? payment = null;

        if (!string.IsNullOrEmpty(paymentLinkId))
        {
            payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.StripePaymentLinkId == paymentLinkId);
        }

        if (payment == null && !string.IsNullOrEmpty(paymentIntentId))
        {
            payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId);
        }

        if (payment == null && jobId.HasValue)
        {
            payment = await _context.Payments
                .Where(p =>
                    p.JobId == jobId.Value &&
                    p.PaymentMethod == PaymentLifecycle.Methods.PaymentLink)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }

        if (payment == null)
        {
            return;
        }

        var prefix = string.IsNullOrWhiteSpace(errorCode) ? "Webhook" : $"Webhook[{errorCode}]";
        payment.PaymentErrorMessage = $"{prefix}: {errorMessage}";
        await _context.SaveChangesAsync();
    }

    private async Task HandlePaymentRefunded(WebhookEventResult webhookEvent)
    {
        if (string.IsNullOrEmpty(webhookEvent.TransactionId)) return;

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == webhookEvent.TransactionId);

        if (payment == null)
        {
            _logger.LogWarning("No payment record found for refunded transaction {TransactionId}.", webhookEvent.TransactionId);
            return;
        }

        payment.PaymentStatus = PaymentLifecycle.Statuses.Refunded;
        payment.RefundedAt = DateTime.UtcNow;
        payment.RefundAmount = webhookEvent.AmountRefunded;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Marked payment as Refunded for transaction {TransactionId}.", webhookEvent.TransactionId);
    }

    private static string NormalizePaymentMethod(string? method)
    {
        if (string.Equals(method, "Cash", StringComparison.OrdinalIgnoreCase))
            return "Cash";

        if (string.Equals(method, "PaymentLink", StringComparison.OrdinalIgnoreCase))
            return "PaymentLink";

        return "Card";
    }

    // ─── Mapping helpers ─────────────────────────────────────────────────────

    private PaymentDto MapToPaymentDto(Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            JobId = payment.JobId,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            PaymentStatus = payment.PaymentStatus,
            CaptureStatus = payment.CaptureStatus,
            IsPreAuthorization = payment.IsPreAuthorization,
            AuthorizedAmount = payment.AuthorizedAmount,
            CapturedAmount = payment.CapturedAmount,
            AuthorizationExpiresAt = payment.AuthorizationExpiresAt,
            CapturedAt = payment.CapturedAt,
            ReleasedAt = payment.ReleasedAt,
            StripePaymentIntentId = payment.StripePaymentIntentId,
            StripeChargeId = payment.StripeChargeId,
            CardLast4 = payment.CardLast4,
            CardBrand = payment.CardBrand,
            PaymentLinkId = payment.PaymentLinkId,
            StripePaymentLinkId = payment.StripePaymentLinkId,
            StripePaymentLinkUrl = payment.StripePaymentLinkUrl,
            CashCollectedBy = payment.CashCollectedBy,
            CashCollectedAt = payment.CashCollectedAt,
            ProcessedBy = payment.ProcessedBy,
            ProcessedAt = payment.ProcessedAt,
            TransactionId = payment.TransactionId,
            PaymentErrorMessage = payment.PaymentErrorMessage,
            FraudStatus = payment.FraudStatus,
            FraudScore = payment.FraudScore,
            FraudReasons = payment.FraudReasons,
            FraudReviewedBy = payment.FraudReviewedBy,
            FraudReviewedAt = payment.FraudReviewedAt,
            IsCancellationFeePayment = payment.IsCancellationFeePayment,
            CancellationFeeAmount = payment.CancellationFeeAmount,
            RefundedAt = payment.RefundedAt,
            RefundReason = payment.RefundReason,
            RefundAmount = payment.RefundAmount,
            CreatedAt = payment.CreatedAt
        };
    }

    private PaymentListItemDto MapToPaymentListItemDto(Payment payment)
    {
        var job = payment.Job;
        var client = job?.Vehicle?.Owner;

        var settings = _context.SystemSettings.FirstOrDefault();
        var commissionPercentage = settings?.DriverCommissionPercentage ?? 30.00m;
        var driverCommission = payment.Amount * (commissionPercentage / 100);

        var cashCollection = _context.CashCollections
            .FirstOrDefault(cc => cc.PaymentId == payment.Id);

        var processedByUser = payment.ProcessedBy != null
            ? _context.Users.FirstOrDefault(u => u.Id == payment.ProcessedBy)
            : null;

        return new PaymentListItemDto
        {
            Id = payment.Id,
            JobId = payment.JobId,
            JobNumber = $"Job #{payment.JobId}",
            ClientName = client?.FullName ?? "Unknown",
            ClientEmail = client?.Email ?? "",
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            PaymentStatus = payment.PaymentStatus,
            CaptureStatus = payment.CaptureStatus,
            IsPreAuthorization = payment.IsPreAuthorization,
            AuthorizedAmount = payment.AuthorizedAmount,
            CapturedAmount = payment.CapturedAmount,
            AuthorizationExpiresAt = payment.AuthorizationExpiresAt,
            ProcessedAt = payment.ProcessedAt ?? payment.CreatedAt,
            ProcessedByName = processedByUser?.FullName ?? "Unknown",
            DriverId = job?.DriverId,
            DriverName = job?.Driver?.FullName,
            DriverCommission = driverCommission,
            DriverCommissionRatePercent = commissionPercentage,
            CashCollected = cashCollection != null,
            CashCollectedBy = cashCollection?.Driver?.FullName,
            CashCollectedAt = cashCollection?.CollectedAt,
            TransactionId = payment.TransactionId,
            CardLast4 = payment.CardLast4,
            CardBrand = payment.CardBrand,
            PaymentErrorMessage = payment.PaymentErrorMessage,
            FraudStatus = payment.FraudStatus,
            FraudScore = payment.FraudScore,
            IsCancellationFeePayment = payment.IsCancellationFeePayment,
            CancellationFeeAmount = payment.CancellationFeeAmount
        };
    }
}

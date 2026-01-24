using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Core.Entities;
using StrongTowing.Core.Constants;
using StrongTowing.Infrastructure.Data;
using System.Security.Claims;

namespace StrongTowing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<PaymentsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Get all payments with optional filters (Admin/Dispatcher only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<IEnumerable<PaymentListItemDto>>> GetAllPayments(
        [FromQuery] string? paymentMethod = null,
        [FromQuery] string? paymentStatus = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        [FromQuery] string? driverId = null,
        [FromQuery] string? searchTerm = null)
    {
        try
        {
            var query = _context.Payments
                .Include(p => p.Job)
                    .ThenInclude(j => j.Vehicle)
                        .ThenInclude(v => v.Owner)
                .Include(p => p.Job)
                    .ThenInclude(j => j.Driver)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(paymentMethod))
            {
                query = query.Where(p => p.PaymentMethod == paymentMethod);
            }

            if (!string.IsNullOrEmpty(paymentStatus))
            {
                query = query.Where(p => p.PaymentStatus == paymentStatus);
            }

            if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var start))
            {
                query = query.Where(p => p.CreatedAt >= start);
            }

            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var end))
            {
                query = query.Where(p => p.CreatedAt <= end.AddDays(1)); // Include full end date
            }

            if (!string.IsNullOrEmpty(driverId))
            {
                query = query.Where(p => p.Job.DriverId == driverId);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var search = searchTerm.ToLower();
                query = query.Where(p =>
                    p.Job.Id.ToString().Contains(search) ||
                    (p.Job.Vehicle.Owner != null && p.Job.Vehicle.Owner.FullName.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(p.TransactionId) && p.TransactionId.ToLower().Contains(search))
                );
            }

            var payments = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var paymentDtos = payments.Select(p => MapToPaymentListItemDto(p)).ToList();

            return Ok(paymentDtos);
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
    [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<PaymentDto>> GetPaymentById(int id)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Job)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
            {
                return NotFound(new { error = "Not Found", message = $"Payment with ID {id} was not found." });
            }

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
    [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<PaymentDto>> GetPaymentByJobId(int jobId)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Job)
                .FirstOrDefaultAsync(p => p.JobId == jobId);

            if (payment == null)
            {
                return NotFound(new { error = "Not Found", message = $"Payment for job ID {jobId} was not found." });
            }

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
    [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<PaymentStatisticsDto>> GetPaymentStatistics(
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        try
        {
            var query = _context.Payments.AsQueryable();

            if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var start))
            {
                query = query.Where(p => p.CreatedAt >= start);
            }

            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var end))
            {
                query = query.Where(p => p.CreatedAt <= end.AddDays(1));
            }

            var payments = await query.ToListAsync();

            // Get system settings for commission percentage
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            var commissionPercentage = settings?.DriverCommissionPercentage ?? 30.00m;

            var statistics = new PaymentStatisticsDto
            {
                TotalPayments = payments.Count,
                TotalRevenue = payments.Where(p => p.PaymentStatus == "Paid").Sum(p => p.Amount),
                RevenueByMethod = new RevenueByMethodDto
                {
                    Card = payments.Where(p => p.PaymentMethod == "Card" && p.PaymentStatus == "Paid").Sum(p => p.Amount),
                    PaymentLink = payments.Where(p => p.PaymentMethod == "PaymentLink" && p.PaymentStatus == "Paid").Sum(p => p.Amount),
                    Cash = payments.Where(p => p.PaymentMethod == "Cash" && p.PaymentStatus == "Paid").Sum(p => p.Amount)
                },
                PendingPayments = payments.Count(p => p.PaymentStatus == "Pending"),
                TotalCashCollected = payments.Where(p => p.PaymentMethod == "Cash" && p.PaymentStatus == "Paid").Sum(p => p.Amount),
                TotalDriverCommissions = payments
                    .Where(p => p.PaymentStatus == "Paid")
                    .Sum(p => p.Amount * (commissionPercentage / 100))
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
    /// Process payment (Admin/Dispatcher only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<PaymentDto>> ProcessPayment([FromBody] ProcessPaymentRequest request)
    {
        try
        {
            var job = await _context.Jobs
                .Include(j => j.Vehicle)
                .FirstOrDefaultAsync(j => j.Id == request.JobId);

            if (job == null)
            {
                return NotFound(new { error = "Not Found", message = $"Job with ID {request.JobId} was not found." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var payment = new Payment
            {
                JobId = request.JobId,
                Amount = request.Amount,
                PaymentMethod = request.PaymentMethod,
                PaymentStatus = "Paid",
                ProcessedBy = userId,
                ProcessedAt = DateTime.UtcNow,
                TransactionId = request.TransactionId,
                StripePaymentIntentId = request.PaymentIntentId
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Update job payment status
            job.PaymentStatus = "Paid";
            job.PaymentId = payment.Id;
            job.PaidAt = DateTime.UtcNow;
            job.PaidBy = userId;
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPaymentById), new { id = payment.Id }, MapToPaymentDto(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while processing payment." });
        }
    }

    // Helper methods
    private PaymentDto MapToPaymentDto(Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            JobId = payment.JobId,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            PaymentStatus = payment.PaymentStatus,
            StripePaymentIntentId = payment.StripePaymentIntentId,
            StripeChargeId = payment.StripeChargeId,
            CardLast4 = payment.CardLast4,
            CardBrand = payment.CardBrand,
            PaymentLinkId = payment.PaymentLinkId,
            CashCollectedBy = payment.CashCollectedBy,
            CashCollectedAt = payment.CashCollectedAt,
            ProcessedBy = payment.ProcessedBy,
            ProcessedAt = payment.ProcessedAt,
            TransactionId = payment.TransactionId,
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
        
        // Get system settings for commission calculation
        var settings = _context.SystemSettings.FirstOrDefault();
        var commissionPercentage = settings?.DriverCommissionPercentage ?? 30.00m;
        var driverCommission = payment.Amount * (commissionPercentage / 100);

        // Check if cash was collected
        var cashCollection = _context.CashCollections
            .FirstOrDefault(cc => cc.PaymentId == payment.Id);

        // Get processed by user name
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
            ProcessedAt = payment.ProcessedAt ?? payment.CreatedAt,
            ProcessedByName = processedByUser?.FullName ?? "Unknown",
            DriverId = job?.DriverId,
            DriverName = job?.Driver?.FullName,
            DriverCommission = driverCommission,
            CashCollected = cashCollection != null,
            CashCollectedBy = cashCollection?.Driver?.FullName,
            CashCollectedAt = cashCollection?.CollectedAt,
            TransactionId = payment.TransactionId,
            CardLast4 = payment.CardLast4,
            CardBrand = payment.CardBrand
        };
    }
}

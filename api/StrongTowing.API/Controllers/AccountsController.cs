using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Core.Constants;
using StrongTowing.Core.Entities;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(
        ApplicationDbContext context,
        ILogger<AccountsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all insurance accounts.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<IEnumerable<InsuranceAccountDto>>> GetAll([FromQuery] bool includeInactive = false)
    {
        try
        {
            var query = _context.InsuranceAccounts.AsQueryable();
            if (!includeInactive)
            {
                query = query.Where(a => a.IsActive);
            }

            var accounts = await query
                .OrderBy(a => a.Name)
                .ToListAsync();

            return Ok(accounts.Select(MapToDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving insurance accounts. IncludeInactive={IncludeInactive}", includeInactive);
            return HandleUnexpectedError("An error occurred while retrieving accounts.", ex);
        }
    }

    /// <summary>
    /// Get insurance account by id.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
    public async Task<ActionResult<InsuranceAccountDto>> GetById(int id)
    {
        try
        {
            var account = await _context.InsuranceAccounts.FindAsync(id);
            if (account == null)
            {
                return NotFound(new { error = "Not Found", message = "Account was not found." });
            }

            return Ok(MapToDto(account));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving insurance account {AccountId}", id);
            return HandleUnexpectedError("An error occurred while retrieving the account.", ex);
        }
    }

    /// <summary>
    /// Create insurance account.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
    public async Task<ActionResult<InsuranceAccountDto>> Create([FromBody] CreateInsuranceAccountRequest request)
    {
        try
        {
            var normalizedName = request.Name.Trim();

            var duplicate = await _context.InsuranceAccounts
                .AnyAsync(a => a.Name.ToLower() == normalizedName.ToLower());

            if (duplicate)
            {
                return Conflict(new { error = "Conflict", message = "An account with this name already exists." });
            }

            var account = new InsuranceAccount
            {
                Name = normalizedName,
                AccountNumber = request.AccountNumber?.Trim(),
                ContactName = request.ContactName?.Trim(),
                BillingEmail = request.BillingEmail?.Trim(),
                BillingPhone = request.BillingPhone?.Trim(),
                AddressLine1 = request.AddressLine1?.Trim(),
                AddressLine2 = request.AddressLine2?.Trim(),
                City = request.City?.Trim(),
                State = request.State?.Trim(),
                PostalCode = request.PostalCode?.Trim(),
                Notes = request.Notes?.Trim(),
                IsActive = request.IsActive,
                HookupFee = request.HookupFee,
                RateAB = request.RateAB,
                RateBC = request.RateBC,
                RateCA = request.RateCA,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.InsuranceAccounts.Add(account);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = account.Id }, MapToDto(account));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating insurance account {AccountName}", request.Name);
            return HandleUnexpectedError("An error occurred while creating the account.", ex);
        }
    }

    /// <summary>
    /// Update insurance account.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
    public async Task<ActionResult<InsuranceAccountDto>> Update(int id, [FromBody] UpdateInsuranceAccountRequest request)
    {
        try
        {
            var account = await _context.InsuranceAccounts.FindAsync(id);
            if (account == null)
            {
                return NotFound(new { error = "Not Found", message = "Account was not found." });
            }

            var normalizedName = request.Name.Trim();
            var duplicate = await _context.InsuranceAccounts
                .AnyAsync(a => a.Id != id && a.Name.ToLower() == normalizedName.ToLower());

            if (duplicate)
            {
                return Conflict(new { error = "Conflict", message = "An account with this name already exists." });
            }

            account.Name = normalizedName;
            account.AccountNumber = request.AccountNumber?.Trim();
            account.ContactName = request.ContactName?.Trim();
            account.BillingEmail = request.BillingEmail?.Trim();
            account.BillingPhone = request.BillingPhone?.Trim();
            account.AddressLine1 = request.AddressLine1?.Trim();
            account.AddressLine2 = request.AddressLine2?.Trim();
            account.City = request.City?.Trim();
            account.State = request.State?.Trim();
            account.PostalCode = request.PostalCode?.Trim();
            account.Notes = request.Notes?.Trim();
            account.IsActive = request.IsActive;
            account.HookupFee = request.HookupFee;
            account.RateAB = request.RateAB;
            account.RateBC = request.RateBC;
            account.RateCA = request.RateCA;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(MapToDto(account));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating insurance account {AccountId}", id);
            return HandleUnexpectedError("An error occurred while updating the account.", ex);
        }
    }

    /// <summary>
    /// Delete insurance account.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var account = await _context.InsuranceAccounts.FindAsync(id);
            if (account == null)
            {
                return NotFound(new { error = "Not Found", message = "Account was not found." });
            }

            _context.InsuranceAccounts.Remove(account);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting insurance account {AccountId}", id);
            return HandleUnexpectedError("An error occurred while deleting the account.", ex);
        }
    }

    private ObjectResult HandleUnexpectedError(string message, Exception ex)
    {
        var baseException = ex.GetBaseException();
        var sqlException = FindSqlException(ex);

        return StatusCode(500, new
        {
            error = "Internal Server Error",
            message,
            details = baseException.Message,
            sqlError = sqlException?.Message,
            traceId = HttpContext.TraceIdentifier
        });
    }

    private static SqlException? FindSqlException(Exception exception)
    {
        var current = exception;
        while (current != null)
        {
            if (current is SqlException sqlException)
            {
                return sqlException;
            }

            current = current.InnerException;
        }

        return null;
    }

    private static InsuranceAccountDto MapToDto(InsuranceAccount account)
    {
        return new InsuranceAccountDto
        {
            Id = account.Id,
            Name = account.Name,
            AccountNumber = account.AccountNumber,
            ContactName = account.ContactName,
            BillingEmail = account.BillingEmail,
            BillingPhone = account.BillingPhone,
            AddressLine1 = account.AddressLine1,
            AddressLine2 = account.AddressLine2,
            City = account.City,
            State = account.State,
            PostalCode = account.PostalCode,
            Notes = account.Notes,
            IsActive = account.IsActive,
            HookupFee = account.HookupFee,
            RateAB = account.RateAB,
            RateBC = account.RateBC,
            RateCA = account.RateCA,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }
}

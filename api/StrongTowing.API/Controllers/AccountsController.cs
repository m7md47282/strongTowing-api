using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    public AccountsController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all insurance accounts.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<IEnumerable<InsuranceAccountDto>>> GetAll([FromQuery] bool includeInactive = false)
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

    /// <summary>
    /// Get insurance account by id.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
    public async Task<ActionResult<InsuranceAccountDto>> GetById(int id)
    {
        var account = await _context.InsuranceAccounts.FindAsync(id);
        if (account == null)
        {
            return NotFound(new { error = "Not Found", message = "Account was not found." });
        }

        return Ok(MapToDto(account));
    }

    /// <summary>
    /// Create insurance account.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
    public async Task<ActionResult<InsuranceAccountDto>> Create([FromBody] CreateInsuranceAccountRequest request)
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
            ServiceChargePercent = request.ServiceChargePercent,
            TaxPercent = request.TaxPercent,
            IsTaxExemptByDefault = request.IsTaxExemptByDefault,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.InsuranceAccounts.Add(account);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = account.Id }, MapToDto(account));
    }

    /// <summary>
    /// Update insurance account.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
    public async Task<ActionResult<InsuranceAccountDto>> Update(int id, [FromBody] UpdateInsuranceAccountRequest request)
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
        account.ServiceChargePercent = request.ServiceChargePercent;
        account.TaxPercent = request.TaxPercent;
        account.IsTaxExemptByDefault = request.IsTaxExemptByDefault;
        account.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(MapToDto(account));
    }

    /// <summary>
    /// Delete insurance account.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
    public async Task<IActionResult> Delete(int id)
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
            ServiceChargePercent = account.ServiceChargePercent,
            TaxPercent = account.TaxPercent,
            IsTaxExemptByDefault = account.IsTaxExemptByDefault,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }
}

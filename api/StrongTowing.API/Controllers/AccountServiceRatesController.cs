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
[Route("api/accounts/{accountId:int}/service-rates")]
[Authorize]
public class AccountServiceRatesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountServiceRatesController> _logger;

    public AccountServiceRatesController(ApplicationDbContext context, ILogger<AccountServiceRatesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<IEnumerable<InsuranceAccountServiceRateDto>>> List(int accountId)
    {
        var exists = await _context.InsuranceAccounts.AnyAsync(a => a.Id == accountId);
        if (!exists)
        {
            return NotFound(new { error = "Not Found", message = "Account was not found." });
        }

        var rows = await _context.InsuranceAccountServiceRates.AsNoTracking()
            .Include(x => x.ServicePricingProfile)
            .Where(x => x.InsuranceAccountId == accountId)
            .OrderBy(x => x.ServicePricingProfile!.Name)
            .ToListAsync();

        return Ok(rows.Select(MapToDto));
    }

    [HttpPost]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
    public async Task<ActionResult<InsuranceAccountServiceRateDto>> Create(int accountId, [FromBody] UpsertInsuranceAccountServiceRateRequest request)
    {
        var account = await _context.InsuranceAccounts.FindAsync(accountId);
        if (account == null)
        {
            return NotFound(new { error = "Not Found", message = "Account was not found." });
        }

        var profile = await _context.ServicePricingProfiles.FindAsync(request.ServicePricingProfileId);
        if (profile == null)
        {
            return BadRequest(new { error = "Bad Request", message = "Service pricing profile was not found." });
        }

        var duplicate = await _context.InsuranceAccountServiceRates
            .AnyAsync(x => x.InsuranceAccountId == accountId && x.ServicePricingProfileId == request.ServicePricingProfileId);
        if (duplicate)
        {
            return Conflict(new { error = "Conflict", message = "This service is already configured for this account." });
        }

        var row = new InsuranceAccountServiceRate
        {
            InsuranceAccountId = accountId,
            ServicePricingProfileId = request.ServicePricingProfileId,
            BasePrice = request.BasePrice,
            PricePerMile = request.PricePerMile,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.InsuranceAccountServiceRates.Add(row);
        await _context.SaveChangesAsync();

        await _context.Entry(row).Reference(x => x.ServicePricingProfile).LoadAsync();
        return CreatedAtAction(nameof(List), new { accountId }, MapToDto(row));
    }

    [HttpPut("{rateId:int}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
    public async Task<ActionResult<InsuranceAccountServiceRateDto>> Update(
        int accountId,
        int rateId,
        [FromBody] UpsertInsuranceAccountServiceRateRequest request)
    {
        var row = await _context.InsuranceAccountServiceRates
            .Include(x => x.ServicePricingProfile)
            .FirstOrDefaultAsync(x => x.Id == rateId && x.InsuranceAccountId == accountId);

        if (row == null)
        {
            return NotFound(new { error = "Not Found", message = "Rate row was not found." });
        }

        if (request.ServicePricingProfileId != row.ServicePricingProfileId)
        {
            var duplicate = await _context.InsuranceAccountServiceRates
                .AnyAsync(x => x.InsuranceAccountId == accountId
                    && x.ServicePricingProfileId == request.ServicePricingProfileId
                    && x.Id != rateId);
            if (duplicate)
            {
                return Conflict(new { error = "Conflict", message = "This service is already configured for this account." });
            }

            var profile = await _context.ServicePricingProfiles.FindAsync(request.ServicePricingProfileId);
            if (profile == null)
            {
                return BadRequest(new { error = "Bad Request", message = "Service pricing profile was not found." });
            }

            row.ServicePricingProfileId = request.ServicePricingProfileId;
        }

        row.BasePrice = request.BasePrice;
        row.PricePerMile = request.PricePerMile;
        row.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _context.Entry(row).Reference(x => x.ServicePricingProfile).LoadAsync();
        return Ok(MapToDto(row));
    }

    [HttpDelete("{rateId:int}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
    public async Task<IActionResult> Delete(int accountId, int rateId)
    {
        var row = await _context.InsuranceAccountServiceRates
            .FirstOrDefaultAsync(x => x.Id == rateId && x.InsuranceAccountId == accountId);

        if (row == null)
        {
            return NotFound(new { error = "Not Found", message = "Rate row was not found." });
        }

        _context.InsuranceAccountServiceRates.Remove(row);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static InsuranceAccountServiceRateDto MapToDto(InsuranceAccountServiceRate row)
    {
        return new InsuranceAccountServiceRateDto
        {
            Id = row.Id,
            InsuranceAccountId = row.InsuranceAccountId,
            ServicePricingProfileId = row.ServicePricingProfileId,
            ServiceName = row.ServicePricingProfile?.Name ?? string.Empty,
            BasePrice = row.BasePrice,
            PricePerMile = row.PricePerMile,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt
        };
    }
}

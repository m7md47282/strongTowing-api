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
public class ServicePricingController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ServicePricingController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<IEnumerable<ServicePricingProfileDto>>> GetAll([FromQuery] bool includeUnavailable = false)
    {
        var query = _context.ServicePricingProfiles.AsQueryable();
        if (!includeUnavailable)
        {
            query = query.Where(x => x.IsAvailable);
        }

        var rows = await query
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(rows.Select(MapToDto));
    }

    [HttpGet("by-name/{name}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<ServicePricingProfileDto>> GetByName(string name)
    {
        var normalizedName = name.Trim();
        var row = await _context.ServicePricingProfiles
            .FirstOrDefaultAsync(x => x.Name.ToLower() == normalizedName.ToLower() && x.IsAvailable);

        if (row == null)
        {
            return NotFound(new { error = "Not Found", message = "Service pricing profile was not found." });
        }

        return Ok(MapToDto(row));
    }

    [HttpPost]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
    public async Task<ActionResult<ServicePricingProfileDto>> Create([FromBody] CreateServicePricingProfileRequest request)
    {
        var normalizedName = request.Name.Trim();
        var existingByName = await _context.ServicePricingProfiles
            .FirstOrDefaultAsync(x => x.Name.ToLower() == normalizedName.ToLower());

        if (existingByName != null)
        {
            if (existingByName.IsAvailable)
            {
                return Conflict(new { error = "Conflict", message = "A service with this name already exists." });
            }

            // Unavailable rows are hidden from the default list but still reserve the name.
            // Hard-delete the placeholder so admins can recreate an active service with the same name.
            _context.ServicePricingProfiles.Remove(existingByName);
        }

        var effectiveLoaded = request.LoadedPricePerMile ?? request.PricePerMile;
        var row = new ServicePricingProfile
        {
            Name = normalizedName,
            BasePrice = request.BasePrice,
            PricePerMile = effectiveLoaded,
            LoadedPricePerMile = request.LoadedPricePerMile,
            EnroutePricePerMile = request.EnroutePricePerMile,
            DeadheadPricePerMile = request.DeadheadPricePerMile,
            IsAvailable = request.IsAvailable,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ServicePricingProfiles.Add(row);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByName), new { name = row.Name }, MapToDto(row));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
    public async Task<ActionResult<ServicePricingProfileDto>> Update(int id, [FromBody] UpdateServicePricingProfileRequest request)
    {
        var row = await _context.ServicePricingProfiles.FindAsync(id);
        if (row == null)
        {
            return NotFound(new { error = "Not Found", message = "Service pricing profile was not found." });
        }

        var normalizedName = request.Name.Trim();
        var conflicting = await _context.ServicePricingProfiles
            .FirstOrDefaultAsync(x => x.Id != id && x.Name.ToLower() == normalizedName.ToLower());

        if (conflicting != null)
        {
            if (conflicting.IsAvailable)
            {
                return Conflict(new { error = "Conflict", message = "A service with this name already exists." });
            }

            _context.ServicePricingProfiles.Remove(conflicting);
        }

        row.Name = normalizedName;
        row.BasePrice = request.BasePrice;
        row.PricePerMile = request.LoadedPricePerMile ?? request.PricePerMile;
        row.LoadedPricePerMile = request.LoadedPricePerMile;
        row.EnroutePricePerMile = request.EnroutePricePerMile;
        row.DeadheadPricePerMile = request.DeadheadPricePerMile;
        row.IsAvailable = request.IsAvailable;
        row.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(MapToDto(row));
    }

    [HttpPut("{id:int}/availability")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
    public async Task<ActionResult<ServicePricingProfileDto>> SetAvailability(int id, [FromBody] bool isAvailable)
    {
        var row = await _context.ServicePricingProfiles.FindAsync(id);
        if (row == null)
        {
            return NotFound(new { error = "Not Found", message = "Service pricing profile was not found." });
        }

        row.IsAvailable = isAvailable;
        row.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(MapToDto(row));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator}")]
    public async Task<IActionResult> Delete(int id)
    {
        var row = await _context.ServicePricingProfiles.FindAsync(id);
        if (row == null)
        {
            return NotFound(new { error = "Not Found", message = "Service pricing profile was not found." });
        }

        _context.ServicePricingProfiles.Remove(row);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static ServicePricingProfileDto MapToDto(ServicePricingProfile row)
    {
        return new ServicePricingProfileDto
        {
            Id = row.Id,
            Name = row.Name,
            BasePrice = row.BasePrice,
            PricePerMile = row.PricePerMile,
            EnroutePricePerMile = row.EnroutePricePerMile,
            LoadedPricePerMile = row.LoadedPricePerMile,
            DeadheadPricePerMile = row.DeadheadPricePerMile,
            IsAvailable = row.IsAvailable,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt
        };
    }
}

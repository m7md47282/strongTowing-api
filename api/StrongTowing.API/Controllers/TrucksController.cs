using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Core.Constants;
using StrongTowing.Core.Entities;
using StrongTowing.Core.Enums;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TrucksController : ControllerBase
{
    private static readonly JobStatus[] ActiveJobStatuses =
    {
        JobStatus.Waiting,
        JobStatus.Dispatch,
        JobStatus.OnRoute,
        JobStatus.OnScene,
        JobStatus.Loaded
    };

    private readonly ApplicationDbContext _context;
    private readonly ILogger<TrucksController> _logger;

    public TrucksController(ApplicationDbContext context, ILogger<TrucksController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// List trucks with active job assignments and availability label.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<IEnumerable<TruckListItemDto>>> GetAll([FromQuery] bool includeInactive = false)
    {
        try
        {
            var query = _context.Trucks
                .AsNoTracking()
                .Include(t => t.TruckType)
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(t => t.IsActive);
            }

            var trucks = await query.OrderBy(t => t.TruckType!.Name).ThenBy(t => t.UnitLabel).ToListAsync();

            var activeJobs = await _context.Jobs
                .AsNoTracking()
                .Where(j => j.TruckId != null && ActiveJobStatuses.Contains(j.Status))
                .Include(j => j.Driver)
                .ToListAsync();

            var byTruck = activeJobs
                .Where(j => j.TruckId.HasValue)
                .GroupBy(j => j.TruckId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = trucks.Select(t =>
            {
                byTruck.TryGetValue(t.Id, out var jobsForTruck);
                jobsForTruck ??= new List<Job>();

                var activeJobDtos = jobsForTruck.Select(j => new TruckActiveJobDto
                {
                    JobId = j.Id,
                    Status = j.Status.ToString(),
                    DriverId = j.DriverId,
                    DriverName = j.Driver?.FullName
                }).ToList();

                var hasActiveJob = jobsForTruck.Count > 0;
                var label = t.IsOutOfService
                    ? "Out of service"
                    : hasActiveJob
                        ? "Busy"
                        : "Available";

                return new TruckListItemDto
                {
                    Id = t.Id,
                    TruckTypeId = t.TruckTypeId,
                    TruckTypeName = t.TruckType?.Name ?? string.Empty,
                    UnitLabel = t.UnitLabel,
                    LicensePlate = t.LicensePlate,
                    Vin = t.Vin,
                    Make = t.Make,
                    Model = t.Model,
                    Year = t.Year,
                    Notes = t.Notes,
                    IsOutOfService = t.IsOutOfService,
                    IsActive = t.IsActive,
                    AvailabilityLabel = label,
                    ActiveJobs = activeJobDtos,
                    CreatedAt = t.CreatedAt
                };
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing trucks");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while listing trucks." });
        }
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<TruckListItemDto>> GetById(int id)
    {
        var t = await _context.Trucks
            .AsNoTracking()
            .Include(x => x.TruckType)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (t == null)
        {
            return NotFound(new { error = "Not Found", message = $"Truck {id} was not found." });
        }

        var activeJobs = await _context.Jobs
            .AsNoTracking()
            .Where(j => j.TruckId == id && ActiveJobStatuses.Contains(j.Status))
            .Include(j => j.Driver)
            .ToListAsync();

        var activeJobDtos = activeJobs.Select(j => new TruckActiveJobDto
        {
            JobId = j.Id,
            Status = j.Status.ToString(),
            DriverId = j.DriverId,
            DriverName = j.Driver?.FullName
        }).ToList();

        var hasActiveJob = activeJobs.Count > 0;
        var label = t.IsOutOfService
            ? "Out of service"
            : hasActiveJob
                ? "Busy"
                : "Available";

        return Ok(new TruckListItemDto
        {
            Id = t.Id,
            TruckTypeId = t.TruckTypeId,
            TruckTypeName = t.TruckType?.Name ?? string.Empty,
            UnitLabel = t.UnitLabel,
            LicensePlate = t.LicensePlate,
            Vin = t.Vin,
            Make = t.Make,
            Model = t.Model,
            Year = t.Year,
            Notes = t.Notes,
            IsOutOfService = t.IsOutOfService,
            IsActive = t.IsActive,
            AvailabilityLabel = label,
            ActiveJobs = activeJobDtos,
            CreatedAt = t.CreatedAt
        });
    }

    /// <summary>
    /// Trucks for job dropdowns (active only). Includes availability for UI badges.
    /// </summary>
    [HttpGet("for-jobs")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<IEnumerable<TruckListItemDto>>> GetForJobDropdowns()
    {
        return await GetAll(includeInactive: false);
    }

    [HttpPost]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<TruckListItemDto>> Create([FromBody] CreateTruckRequest request)
    {
        try
        {
            var typeExists = await _context.TruckTypes.AnyAsync(t => t.Id == request.TruckTypeId);
            if (!typeExists)
            {
                return BadRequest(new { error = "Bad Request", message = "Truck type was not found." });
            }

            var entity = new Truck
            {
                TruckTypeId = request.TruckTypeId,
                UnitLabel = request.UnitLabel.Trim(),
                LicensePlate = string.IsNullOrWhiteSpace(request.LicensePlate) ? null : request.LicensePlate.Trim(),
                Vin = string.IsNullOrWhiteSpace(request.Vin) ? null : request.Vin.Trim(),
                Make = string.IsNullOrWhiteSpace(request.Make) ? null : request.Make.Trim(),
                Model = string.IsNullOrWhiteSpace(request.Model) ? null : request.Model.Trim(),
                Year = request.Year,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                IsOutOfService = request.IsOutOfService,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Trucks.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, await BuildListItemAsync(entity.Id));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Could not create truck (duplicate unit label?)");
            return BadRequest(new { error = "Bad Request", message = "Could not create truck (duplicate unit label for this type?)." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating truck");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while creating the truck." });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<TruckListItemDto>> Update(int id, [FromBody] UpdateTruckRequest request)
    {
        try
        {
            var typeExists = await _context.TruckTypes.AnyAsync(t => t.Id == request.TruckTypeId);
            if (!typeExists)
            {
                return BadRequest(new { error = "Bad Request", message = "Truck type was not found." });
            }

            var entity = await _context.Trucks.FirstOrDefaultAsync(t => t.Id == id);
            if (entity == null)
            {
                return NotFound(new { error = "Not Found", message = $"Truck {id} was not found." });
            }

            entity.TruckTypeId = request.TruckTypeId;
            entity.UnitLabel = request.UnitLabel.Trim();
            entity.LicensePlate = string.IsNullOrWhiteSpace(request.LicensePlate) ? null : request.LicensePlate.Trim();
            entity.Vin = string.IsNullOrWhiteSpace(request.Vin) ? null : request.Vin.Trim();
            entity.Make = string.IsNullOrWhiteSpace(request.Make) ? null : request.Make.Trim();
            entity.Model = string.IsNullOrWhiteSpace(request.Model) ? null : request.Model.Trim();
            entity.Year = request.Year;
            entity.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            entity.IsOutOfService = request.IsOutOfService;
            entity.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return Ok(await BuildListItemAsync(id));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Could not update truck");
            return BadRequest(new { error = "Bad Request", message = "Could not update truck (duplicate unit label for this type?)." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating truck {Id}", id);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while updating the truck." });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var entity = await _context.Trucks.FirstOrDefaultAsync(t => t.Id == id);
            if (entity == null)
            {
                return NotFound(new { error = "Not Found", message = $"Truck {id} was not found." });
            }

            var referenced = await _context.Jobs.AnyAsync(j => j.TruckId == id);
            if (referenced)
            {
                entity.IsActive = false;
                await _context.SaveChangesAsync();
                return NoContent();
            }

            _context.Trucks.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting truck {Id}", id);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while deleting the truck." });
        }
    }

    private async Task<TruckListItemDto> BuildListItemAsync(int id)
    {
        var t = await _context.Trucks
            .AsNoTracking()
            .Include(x => x.TruckType)
            .FirstAsync(x => x.Id == id);

        var activeJobs = await _context.Jobs
            .AsNoTracking()
            .Where(j => j.TruckId == id && ActiveJobStatuses.Contains(j.Status))
            .Include(j => j.Driver)
            .ToListAsync();

        var activeJobDtos = activeJobs.Select(j => new TruckActiveJobDto
        {
            JobId = j.Id,
            Status = j.Status.ToString(),
            DriverId = j.DriverId,
            DriverName = j.Driver?.FullName
        }).ToList();

        var hasActiveJob = activeJobs.Count > 0;
        var label = t.IsOutOfService
            ? "Out of service"
            : hasActiveJob
                ? "Busy"
                : "Available";

        return new TruckListItemDto
        {
            Id = t.Id,
            TruckTypeId = t.TruckTypeId,
            TruckTypeName = t.TruckType?.Name ?? string.Empty,
            UnitLabel = t.UnitLabel,
            LicensePlate = t.LicensePlate,
            Vin = t.Vin,
            Make = t.Make,
            Model = t.Model,
            Year = t.Year,
            Notes = t.Notes,
            IsOutOfService = t.IsOutOfService,
            IsActive = t.IsActive,
            AvailabilityLabel = label,
            ActiveJobs = activeJobDtos,
            CreatedAt = t.CreatedAt
        };
    }
}

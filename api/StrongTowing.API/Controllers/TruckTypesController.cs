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
[Route("api/truck-types")]
[Authorize]
public class TruckTypesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TruckTypesController> _logger;

    public TruckTypesController(ApplicationDbContext context, ILogger<TruckTypesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<IEnumerable<TruckTypeDto>>> GetAll()
    {
        try
        {
            var list = await _context.TruckTypes
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .Select(t => new TruckTypeDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing truck types");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while listing truck types." });
        }
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<TruckTypeDto>> GetById(int id)
    {
        var t = await _context.TruckTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (t == null)
        {
            return NotFound(new { error = "Not Found", message = $"Truck type {id} was not found." });
        }

        return Ok(new TruckTypeDto
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            CreatedAt = t.CreatedAt
        });
    }

    [HttpPost]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<TruckTypeDto>> Create([FromBody] CreateTruckTypeRequest request)
    {
        try
        {
            var entity = new TruckType
            {
                Name = request.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.TruckTypes.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new TruckTypeDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                CreatedAt = entity.CreatedAt
            });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Duplicate or invalid truck type create");
            return BadRequest(new { error = "Bad Request", message = "Could not create truck type (duplicate name?)." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating truck type");
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while creating the truck type." });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<ActionResult<TruckTypeDto>> Update(int id, [FromBody] UpdateTruckTypeRequest request)
    {
        try
        {
            var entity = await _context.TruckTypes.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound(new { error = "Not Found", message = $"Truck type {id} was not found." });
            }

            entity.Name = request.Name.Trim();
            entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            await _context.SaveChangesAsync();

            return Ok(new TruckTypeDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                CreatedAt = entity.CreatedAt
            });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Duplicate or invalid truck type update");
            return BadRequest(new { error = "Bad Request", message = "Could not update truck type (duplicate name?)." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating truck type {Id}", id);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while updating the truck type." });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var entity = await _context.TruckTypes.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return NotFound(new { error = "Not Found", message = $"Truck type {id} was not found." });
            }

            var inUse = await _context.Trucks.AnyAsync(t => t.TruckTypeId == id);
            if (inUse)
            {
                return BadRequest(new { error = "Bad Request", message = "Cannot delete a truck type that still has trucks." });
            }

            _context.TruckTypes.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting truck type {Id}", id);
            return StatusCode(500, new { error = "Internal Server Error", message = "An error occurred while deleting the truck type." });
        }
    }
}

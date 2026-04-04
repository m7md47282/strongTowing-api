using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTowing.API.Services;
using StrongTowing.Core.Constants;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.API.Controllers;

/// <summary>Vehicle make/model catalog (SQL). Populate via POST sync from NHTSA (Admin).</summary>
[ApiController]
[Route("api/vehicle-catalog")]
[Authorize]
public class VehicleCatalogController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly INhtsaVehicleCatalogSyncService _sync;

    public VehicleCatalogController(ApplicationDbContext db, INhtsaVehicleCatalogSyncService sync)
    {
        _db = db;
        _sync = sync;
    }

    /// <summary>Search makes with pagination.</summary>
    [HttpGet("makes")]
    [ProducesResponseType(typeof(VehicleCatalogMakesPageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<VehicleCatalogMakesPageResponse>> GetMakes(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var totalMakes = await _db.VehicleCatalogMakes.CountAsync(cancellationToken);
        var catalogEmpty = totalMakes == 0;

        var query = _db.VehicleCatalogMakes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(m => m.Name.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var skip = (page - 1) * pageSize;
        var items = await query
            .OrderBy(m => m.Name)
            .Skip(skip)
            .Take(pageSize)
            .Select(m => new VehicleCatalogMakeItem(m.Id, m.Name))
            .ToListAsync(cancellationToken);

        return Ok(new VehicleCatalogMakesPageResponse(items, totalCount, page, pageSize, totalPages, catalogEmpty));
    }

    /// <summary>Search models for a make with pagination.</summary>
    [HttpGet("models")]
    [ProducesResponseType(typeof(VehicleCatalogModelsPageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<VehicleCatalogModelsPageResponse>> GetModels(
        [FromQuery] int? makeId,
        [FromQuery] string? makeName,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken cancellationToken = default)
    {
        if (!makeId.HasValue || makeId.Value <= 0)
        {
            if (!string.IsNullOrWhiteSpace(makeName))
            {
                var mk = await _db.VehicleCatalogMakes.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Name == makeName.Trim(), cancellationToken);
                makeId = mk?.Id;
            }
        }

        if (!makeId.HasValue || makeId.Value <= 0)
        {
            return BadRequest(new { error = "Bad Request", message = "makeId or makeName is required." });
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var totalModels = await _db.VehicleCatalogModels.CountAsync(cancellationToken);
        var catalogEmpty = totalModels == 0;

        var query = _db.VehicleCatalogModels.AsNoTracking().Where(m => m.MakeId == makeId);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(m => m.Name.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var skip = (page - 1) * pageSize;
        var items = await query
            .OrderBy(m => m.Name)
            .Skip(skip)
            .Take(pageSize)
            .Select(m => new VehicleCatalogModelItem(m.Id, m.Name))
            .ToListAsync(cancellationToken);

        return Ok(new VehicleCatalogModelsPageResponse(items, totalCount, page, pageSize, totalPages, catalogEmpty));
    }

    /// <summary>Admin: trigger NHTSA sync (background).</summary>
    [HttpPost("sync")]
    [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.SuperAdmin}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PostSync(CancellationToken cancellationToken)
    {
        var started = await _sync.TryStartSyncAsync(cancellationToken);
        if (!started)
        {
            return Conflict(new { error = "Conflict", message = "Vehicle catalog sync is already running." });
        }

        return Accepted(new { message = "Sync started." });
    }

    /// <summary>Admin: sync job status.</summary>
    [HttpGet("sync-status")]
    [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(VehicleCatalogSyncStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VehicleCatalogSyncStatusDto>> GetSyncStatus(CancellationToken cancellationToken)
    {
        var dto = await _sync.GetStatusAsync(cancellationToken);
        return Ok(dto);
    }
}

public record VehicleCatalogMakeItem(int Id, string Name);

public record VehicleCatalogModelItem(int Id, string Name);

public record VehicleCatalogMakesPageResponse(
    IReadOnlyList<VehicleCatalogMakeItem> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    bool CatalogEmpty);

public record VehicleCatalogModelsPageResponse(
    IReadOnlyList<VehicleCatalogModelItem> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    bool CatalogEmpty);

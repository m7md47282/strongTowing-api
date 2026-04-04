using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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
    private readonly ILogger<VehicleCatalogController> _logger;

    public VehicleCatalogController(
        ApplicationDbContext db,
        INhtsaVehicleCatalogSyncService sync,
        ILogger<VehicleCatalogController> logger)
    {
        _db = db;
        _sync = sync;
        _logger = logger;
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
        try
        {
            var started = await _sync.TryStartSyncAsync(cancellationToken);
            if (!started)
            {
                return Conflict(new { error = "Conflict", message = "Vehicle catalog sync is already running." });
            }

            return Accepted(new { message = "Sync started." });
        }
        catch (Exception ex) when (IsSqlMissingObject(ex))
        {
            _logger.LogError(ex, "Vehicle catalog sync: database object missing (run EF migrations).");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "Service Unavailable",
                message = "Vehicle catalog tables are missing. Apply the latest EF Core migrations to this environment's database (migration AddVehicleCatalogTables), then retry."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vehicle catalog sync could not start.");
            return StatusCode(StatusCodes.Status500InternalServerError, BuildSyncErrorBody(ex));
        }
    }

    /// <summary>Admin: sync job status.</summary>
    [HttpGet("sync-status")]
    [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(VehicleCatalogSyncStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VehicleCatalogSyncStatusDto>> GetSyncStatus(CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _sync.GetStatusAsync(cancellationToken);
            return Ok(dto);
        }
        catch (Exception ex) when (IsSqlMissingObject(ex))
        {
            _logger.LogError(ex, "Vehicle catalog sync-status: database object missing (run EF migrations).");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "Service Unavailable",
                message = "Vehicle catalog tables are missing. Apply EF migrations (AddVehicleCatalogTables), then retry."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vehicle catalog sync-status failed.");
            return StatusCode(StatusCodes.Status500InternalServerError, BuildSyncErrorBody(ex));
        }
    }

    /// <summary>SQL Server 208 = invalid object name (tables not migrated).</summary>
    private static bool IsSqlMissingObject(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is SqlException sql && sql.Number == 208)
            {
                return true;
            }

            // EF sometimes surfaces only message text
            if (e.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Safe diagnostics for admin-only sync endpoints (helps production without log access).</summary>
    private static object BuildSyncErrorBody(Exception ex)
    {
        var sql = FindSqlException(ex);
        var inner = ex.GetBaseException();
        const int maxLen = 600;
        var innerMsg = inner.Message;
        if (innerMsg.Length > maxLen)
        {
            innerMsg = innerMsg[..maxLen] + "…";
        }

        return new
        {
            error = "Internal Server Error",
            message = "Could not complete vehicle catalog sync operation. Use sqlErrorNumber / sqlMessage below to fix the database or permissions.",
            sqlErrorNumber = sql?.Number,
            sqlMessage = sql != null && sql.Message.Length > maxLen ? sql.Message[..maxLen] + "…" : sql?.Message,
            innerMessage = innerMsg,
            hint = HintForSqlError(sql?.Number)
        };
    }

    private static SqlException? FindSqlException(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is SqlException sql)
            {
                return sql;
            }
        }

        return null;
    }

    private static string? HintForSqlError(int? number) => number switch
    {
        208 => "Run EF migrations so VehicleCatalog* tables exist.",
        207 => "Schema mismatch: migration not applied or outdated build.",
        544 => "IDENTITY insert conflict: deploy app build where VehicleCatalogSyncState.Id is store-generated (not forced to 1).",
        229 or 230 => "SQL login lacks permission on VehicleCatalog* tables.",
        18456 => "SQL authentication failed (check connection string / password).",
        4060 => "Cannot open database (name, availability, or firewall).",
        53 or 258 or 10060 => "Network / firewall: SQL Server not reachable from this host.",
        -2 => "Query/command timed out (try again; check SQL load).",
        _ => null
    };
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

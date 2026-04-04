using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace StrongTowing.API.Controllers;

/// <summary>
/// Proxies NHTSA vPIC (US vehicle catalog). Full responses are cached in memory; models are paginated for smaller payloads.
/// </summary>
[ApiController]
[Route("api/vehicle-catalog")]
[Authorize]
public class VehicleCatalogController : ControllerBase
{
    private const string VpicBase = "https://vpic.nhtsa.dot.gov/api/vehicles/";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<VehicleCatalogController> _logger;

    public VehicleCatalogController(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<VehicleCatalogController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>All manufacturer names (US market, NHTSA). Cached server-side.</summary>
    [HttpGet("makes")]
    [ProducesResponseType(typeof(VehicleMakesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<VehicleMakesResponse>> GetMakes(CancellationToken cancellationToken)
    {
        try
        {
            var list = await _cache.GetOrCreateAsync(
                "vpic:makes:v1",
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                    return await FetchAllMakesFromNhtsaAsync(cancellationToken);
                }) ?? new List<string>();

            return Ok(new VehicleMakesResponse(list));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading NHTSA makes");
            return StatusCode((int)HttpStatusCode.BadGateway, new { error = "Bad Gateway", message = "Could not load vehicle makes." });
        }
    }

    /// <summary>Models for a make, paginated. Full list per make is cached after first NHTSA call.</summary>
    [HttpGet("models")]
    [ProducesResponseType(typeof(VehicleModelsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<VehicleModelsResponse>> GetModelsForMake(
        [FromQuery] string? makeName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(makeName))
        {
            return BadRequest(new { error = "Bad Request", message = "makeName is required." });
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        try
        {
            var cacheKey = $"vpic:models:v1:{makeName.Trim().ToUpperInvariant()}";
            var fullList = await _cache.GetOrCreateAsync(
                cacheKey,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                    return await FetchAllModelsForMakeFromNhtsaAsync(makeName.Trim(), cancellationToken);
                }) ?? new List<string>();

            var totalCount = fullList.Count;
            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var skip = (page - 1) * pageSize;
            IReadOnlyList<string> slice = skip >= totalCount
                ? Array.Empty<string>()
                : fullList.Skip(skip).Take(pageSize).ToList();

            return Ok(new VehicleModelsResponse(slice, totalCount, page, pageSize, totalPages));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading NHTSA models for make {Make}", makeName);
            return StatusCode((int)HttpStatusCode.BadGateway, new { error = "Bad Gateway", message = "Could not load vehicle models." });
        }
    }

    private async Task<List<string>> FetchAllMakesFromNhtsaAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "StrongTowing-VehicleCatalog/1.0");
        client.Timeout = TimeSpan.FromSeconds(60);

        var url = $"{VpicBase}getallmakes?format=json";
        using var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("NHTSA getallmakes failed: {Status}", response.StatusCode);
            throw new InvalidOperationException("NHTSA getallmakes failed.");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("Results", out var results) || results.ValueKind != JsonValueKind.Array)
        {
            return new List<string>();
        }

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in results.EnumerateArray())
        {
            if (row.TryGetProperty("Make_Name", out var makeEl))
            {
                var name = makeEl.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    set.Add(name.Trim());
                }
            }
        }

        return set.OrderBy(m => m, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task<List<string>> FetchAllModelsForMakeFromNhtsaAsync(string makeName, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "StrongTowing-VehicleCatalog/1.0");
        client.Timeout = TimeSpan.FromSeconds(60);

        var encoded = Uri.EscapeDataString(makeName);
        var url = $"{VpicBase}GetModelsForMake/{encoded}?format=json";
        using var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("NHTSA GetModelsForMake failed: {Status} make={Make}", response.StatusCode, makeName);
            throw new InvalidOperationException("NHTSA GetModelsForMake failed.");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("Results", out var results) || results.ValueKind != JsonValueKind.Array)
        {
            return new List<string>();
        }

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in results.EnumerateArray())
        {
            if (row.TryGetProperty("Model_Name", out var modelEl))
            {
                var name = modelEl.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    set.Add(name.Trim());
                }
            }
        }

        return set.OrderBy(m => m, StringComparer.OrdinalIgnoreCase).ToList();
    }
}

public record VehicleMakesResponse(IReadOnlyList<string> Makes);

public record VehicleModelsResponse(
    IReadOnlyList<string> Models,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Core.Entities;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.API.Services;

/// <summary>NHTSA vPIC ingest into <see cref="VehicleCatalogMake"/> / <see cref="VehicleCatalogModel"/>.</summary>
public class NhtsaVehicleCatalogSyncService : INhtsaVehicleCatalogSyncService
{
    public const int StatusIdle = 0;
    public const int StatusRunning = 1;
    public const int StatusSucceeded = 2;
    public const int StatusFailed = 3;

    private const string VpicBase = "https://vpic.nhtsa.dot.gov/api/vehicles/";
    private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(120);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NhtsaVehicleCatalogSyncService> _logger;

    public NhtsaVehicleCatalogSyncService(
        IServiceScopeFactory scopeFactory,
        ILogger<NhtsaVehicleCatalogSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<bool> TryStartSyncAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var state = await GetOrCreateStateAsync(db, cancellationToken);
        if (state.Status == StatusRunning)
        {
            return false;
        }

        state.Status = StatusRunning;
        state.LastSyncStartedUtc = DateTime.UtcNow;
        state.LastSyncCompletedUtc = null;
        state.LastSyncError = null;
        state.MakesCount = 0;
        state.ModelsCount = 0;
        state.TotalMakes = 0;
        state.MakesProcessed = 0;
        state.ModelsAddedSoFar = 0;
        await db.SaveChangesAsync(cancellationToken);

        _ = Task.Run(async () => await RunSyncBackgroundAsync(CancellationToken.None).ConfigureAwait(false));
        return true;
    }

    public async Task<VehicleCatalogSyncStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var state = await GetOrCreateStateAsync(db, cancellationToken);

        // Live row counts so the UI is not stuck at 0 during a long Running sync (state snapshot is reset at start).
        var makesCount = await db.VehicleCatalogMakes.AsNoTracking().CountAsync(cancellationToken);
        var modelsCount = await db.VehicleCatalogModels.AsNoTracking().CountAsync(cancellationToken);

        return new VehicleCatalogSyncStatusDto(
            state.Status,
            state.LastSyncStartedUtc,
            state.LastSyncCompletedUtc,
            state.LastSyncError,
            makesCount,
            modelsCount,
            state.TotalMakes,
            state.MakesProcessed,
            state.ModelsAddedSoFar);
    }

    private async Task RunSyncBackgroundAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var httpFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        try
        {
            await db.VehicleCatalogModels.ExecuteDeleteAsync(cancellationToken);
            await db.VehicleCatalogMakes.ExecuteDeleteAsync(cancellationToken);

            var makes = await FetchAllMakesFromNhtsaAsync(httpFactory, _logger, cancellationToken);
            foreach (var (nhtsaMakeId, name) in makes)
            {
                db.VehicleCatalogMakes.Add(new VehicleCatalogMake
                {
                    NhtsaMakeId = nhtsaMakeId,
                    Name = name
                });
            }

            await db.SaveChangesAsync(cancellationToken);

            var makeKeyByNhtsaId = await db.VehicleCatalogMakes
                .AsNoTracking()
                .ToDictionaryAsync(m => m.NhtsaMakeId, m => m.Id, cancellationToken);

            {
                var progress = await GetOrCreateStateAsync(db, cancellationToken);
                progress.TotalMakes = makes.Count;
                progress.MakesProcessed = 0;
                progress.ModelsAddedSoFar = 0;
                await db.SaveChangesAsync(cancellationToken);
            }

            var modelsAdded = 0;
            var makeIndex = 0;
            foreach (var (nhtsaMakeId, name) in makes)
            {
                makeIndex++;

                if (!makeKeyByNhtsaId.TryGetValue(nhtsaMakeId, out var makePk))
                {
                    var progress = await GetOrCreateStateAsync(db, cancellationToken);
                    progress.TotalMakes = makes.Count;
                    progress.MakesProcessed = makeIndex;
                    await db.SaveChangesAsync(cancellationToken);
                    continue;
                }

                var modelRows = await FetchModelsForMakeFromNhtsaAsync(httpFactory, _logger, name, cancellationToken);
                foreach (var (nhtsaModelId, modelName) in modelRows)
                {
                    db.VehicleCatalogModels.Add(new VehicleCatalogModel
                    {
                        MakeId = makePk,
                        NhtsaModelId = nhtsaModelId,
                        Name = modelName
                    });
                }

                modelsAdded += modelRows.Count;

                await db.SaveChangesAsync(cancellationToken);

                {
                    var progress = await GetOrCreateStateAsync(db, cancellationToken);
                    progress.TotalMakes = makes.Count;
                    progress.MakesProcessed = makeIndex;
                    progress.ModelsAddedSoFar = modelsAdded;
                    await db.SaveChangesAsync(cancellationToken);
                }

                await Task.Delay(100, cancellationToken);
            }

            var state = await GetOrCreateStateAsync(db, cancellationToken);
            state.Status = StatusSucceeded;
            state.LastSyncCompletedUtc = DateTime.UtcNow;
            state.LastSyncError = null;
            state.MakesCount = await db.VehicleCatalogMakes.CountAsync(cancellationToken);
            state.ModelsCount = await db.VehicleCatalogModels.CountAsync(cancellationToken);
            state.TotalMakes = state.MakesCount;
            state.MakesProcessed = state.MakesCount;
            state.ModelsAddedSoFar = state.ModelsCount;
            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "NHTSA vehicle catalog sync completed: {Makes} makes, {Models} models.",
                state.MakesCount,
                state.ModelsCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NHTSA vehicle catalog sync failed");
            try
            {
                var state = await GetOrCreateStateAsync(db, cancellationToken);
                state.Status = StatusFailed;
                state.LastSyncCompletedUtc = DateTime.UtcNow;
                state.LastSyncError = ex.Message.Length > 4000 ? ex.Message[..4000] : ex.Message;
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception inner)
            {
                _logger.LogError(inner, "Failed to persist sync failure state");
            }
        }
    }

    private static async Task<VehicleCatalogSyncState> GetOrCreateStateAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var state = await db.VehicleCatalogSyncStates.FindAsync(new object[] { VehicleCatalogSyncState.SingletonId }, cancellationToken);
        if (state != null)
        {
            return state;
        }

        // EF Core often sends an explicit Id into IDENTITY columns (SQL error 544). Seed row 1 with IDENTITY_INSERT instead of Add().
        const string seedSql = """
            SET IDENTITY_INSERT VehicleCatalogSyncStates ON;
            IF NOT EXISTS (SELECT 1 FROM VehicleCatalogSyncStates WHERE Id = 1)
                INSERT INTO VehicleCatalogSyncStates (Id, Status, LastSyncStartedUtc, LastSyncCompletedUtc, LastSyncError, MakesCount, ModelsCount, TotalMakes, MakesProcessed, ModelsAddedSoFar)
                VALUES (1, 0, NULL, NULL, NULL, 0, 0, 0, 0, 0);
            SET IDENTITY_INSERT VehicleCatalogSyncStates OFF;
            """;

        await db.Database.ExecuteSqlRawAsync(seedSql, cancellationToken);

        state = await db.VehicleCatalogSyncStates.FindAsync(new object[] { VehicleCatalogSyncState.SingletonId }, cancellationToken);
        if (state == null)
        {
            throw new InvalidOperationException("VehicleCatalogSyncStates row Id=1 is missing after seed.");
        }

        return state;
    }

    private static async Task<List<(int NhtsaMakeId, string Name)>> FetchAllMakesFromNhtsaAsync(
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "StrongTowing-VehicleCatalog/1.0");
        client.Timeout = HttpTimeout;

        var url = $"{VpicBase}getallmakes?format=json";
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
        using var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = ParseNhtsaJsonBody(json, url, logger);

        if (!doc.RootElement.TryGetProperty("Results", out var results) || results.ValueKind != JsonValueKind.Array)
        {
            return new List<(int, string)>();
        }

        var list = new List<(int, string)>();
        foreach (var row in results.EnumerateArray())
        {
            if (!row.TryGetProperty("Make_ID", out var idEl) || !row.TryGetProperty("Make_Name", out var nameEl))
            {
                continue;
            }

            if (!TryGetIntFromJson(idEl, out var nhtsaId))
            {
                continue;
            }

            var name = nameEl.ValueKind == JsonValueKind.String ? nameEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            list.Add((nhtsaId, name.Trim()));
        }

        return list.OrderBy(m => m.Item2, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static async Task<List<(int NhtsaModelId, string Name)>> FetchModelsForMakeFromNhtsaAsync(
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        string makeName,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "StrongTowing-VehicleCatalog/1.0");
        client.Timeout = HttpTimeout;

        var encoded = Uri.EscapeDataString(makeName);
        var url = $"{VpicBase}GetModelsForMake/{encoded}?format=json";
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
        using var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("NHTSA GetModelsForMake failed: {Status} make={Make}", response.StatusCode, makeName);
            return new List<(int, string)>();
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = ParseNhtsaJsonBody(json, url, logger);

        if (!doc.RootElement.TryGetProperty("Results", out var results) || results.ValueKind != JsonValueKind.Array)
        {
            return new List<(int, string)>();
        }

        var set = new Dictionary<int, string>();
        foreach (var row in results.EnumerateArray())
        {
            if (!row.TryGetProperty("Model_ID", out var idEl) || !row.TryGetProperty("Model_Name", out var nameEl))
            {
                continue;
            }

            if (!TryGetIntFromJson(idEl, out var modelId))
            {
                continue;
            }

            var name = nameEl.ValueKind == JsonValueKind.String ? nameEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            set[modelId] = name.Trim();
        }

        return set.OrderBy(kv => kv.Value, StringComparer.OrdinalIgnoreCase).Select(kv => (kv.Key, kv.Value)).ToList();
    }

    /// <summary>
    /// vPIC must return JSON; HTML (often starting with '&lt;') means proxy/WAF/block page or wrong URL.
    /// </summary>
    private static JsonDocument ParseNhtsaJsonBody(string body, string requestUrl, ILogger logger)
    {
        var trimmed = body.TrimStart();
        if (trimmed.Length == 0)
        {
            logger.LogError("NHTSA vPIC returned empty body for {Url}", requestUrl);
            throw new InvalidOperationException($"NHTSA vPIC returned an empty body. URL: {requestUrl}");
        }

        if (trimmed[0] == '<')
        {
            var preview = body.Length > 280 ? body[..280] + "…" : body;
            logger.LogError("NHTSA vPIC returned HTML instead of JSON for {Url}. Preview: {Preview}", requestUrl, preview);
            throw new InvalidOperationException(
                "NHTSA vPIC returned HTML (not JSON). The server may be blocked by a firewall/proxy, or TLS interception returned an error page. " +
                $"Allow outbound HTTPS to vpic.nhtsa.dot.gov from this host. URL: {requestUrl}");
        }

        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
            var preview = body.Length > 280 ? body[..280] + "…" : body;
            logger.LogError(ex, "NHTSA vPIC JSON parse failed for {Url}. Preview: {Preview}", requestUrl, preview);
            throw new InvalidOperationException(
                $"NHTSA vPIC response was not valid JSON. URL: {requestUrl}", ex);
        }
    }

    private static bool TryGetIntFromJson(JsonElement el, out int value)
    {
        value = 0;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out value))
        {
            return true;
        }

        var s = el.GetString();
        return !string.IsNullOrWhiteSpace(s) && int.TryParse(s, out value);
    }
}

using System.Text.Json;
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
        await db.SaveChangesAsync(cancellationToken);

        _ = Task.Run(async () => await RunSyncBackgroundAsync(CancellationToken.None).ConfigureAwait(false));
        return true;
    }

    public async Task<VehicleCatalogSyncStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var state = await GetOrCreateStateAsync(db, cancellationToken);
        return ToDto(state);
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

            foreach (var (nhtsaMakeId, name) in makes)
            {
                if (!makeKeyByNhtsaId.TryGetValue(nhtsaMakeId, out var makePk))
                {
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

                await db.SaveChangesAsync(cancellationToken);
                await Task.Delay(100, cancellationToken);
            }

            var state = await GetOrCreateStateAsync(db, cancellationToken);
            state.Status = StatusSucceeded;
            state.LastSyncCompletedUtc = DateTime.UtcNow;
            state.LastSyncError = null;
            state.MakesCount = await db.VehicleCatalogMakes.CountAsync(cancellationToken);
            state.ModelsCount = await db.VehicleCatalogModels.CountAsync(cancellationToken);
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

        state = new VehicleCatalogSyncState
        {
            Id = VehicleCatalogSyncState.SingletonId,
            Status = StatusIdle
        };
        db.VehicleCatalogSyncStates.Add(state);
        await db.SaveChangesAsync(cancellationToken);
        return state;
    }

    private static VehicleCatalogSyncStatusDto ToDto(VehicleCatalogSyncState s) =>
        new(s.Status, s.LastSyncStartedUtc, s.LastSyncCompletedUtc, s.LastSyncError, s.MakesCount, s.ModelsCount);

    private static async Task<List<(int NhtsaMakeId, string Name)>> FetchAllMakesFromNhtsaAsync(
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "StrongTowing-VehicleCatalog/1.0");
        client.Timeout = HttpTimeout;

        var url = $"{VpicBase}getallmakes?format=json";
        using var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);

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
        using var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("NHTSA GetModelsForMake failed: {Status} make={Make}", response.StatusCode, makeName);
            return new List<(int, string)>();
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);

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

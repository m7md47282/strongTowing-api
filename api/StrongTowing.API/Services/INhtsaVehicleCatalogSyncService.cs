namespace StrongTowing.API.Services;

public interface INhtsaVehicleCatalogSyncService
{
    /// <summary>Returns false if a sync is already running.</summary>
    Task<bool> TryStartSyncAsync(CancellationToken cancellationToken = default);

    Task<VehicleCatalogSyncStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
}

public record VehicleCatalogSyncStatusDto(
    int Status,
    DateTime? LastSyncStartedUtc,
    DateTime? LastSyncCompletedUtc,
    string? LastSyncError,
    int MakesCount,
    int ModelsCount);

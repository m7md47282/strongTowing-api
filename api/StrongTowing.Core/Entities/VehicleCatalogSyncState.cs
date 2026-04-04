using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Core.Entities;

/// <summary>Singleton row (Id = 1) tracking NHTSA catalog sync progress.</summary>
public class VehicleCatalogSyncState
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    /// <summary>0=Idle, 1=Running, 2=Succeeded, 3=Failed</summary>
    public int Status { get; set; }

    public DateTime? LastSyncStartedUtc { get; set; }
    public DateTime? LastSyncCompletedUtc { get; set; }

    [MaxLength(4000)]
    public string? LastSyncError { get; set; }

    public int MakesCount { get; set; }
    public int ModelsCount { get; set; }
}

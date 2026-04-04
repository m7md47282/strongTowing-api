using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Core.Entities;

/// <summary>Singleton row (Id = 1) tracking NHTSA catalog sync progress.</summary>
public class VehicleCatalogSyncState
{
    public const int SingletonId = 1;

    /// <summary>PK; store-generated (IDENTITY). Do not default to 1 or SQL Server rejects inserts (error 544).</summary>
    public int Id { get; set; }

    /// <summary>0=Idle, 1=Running, 2=Succeeded, 3=Failed</summary>
    public int Status { get; set; }

    public DateTime? LastSyncStartedUtc { get; set; }
    public DateTime? LastSyncCompletedUtc { get; set; }

    [MaxLength(4000)]
    public string? LastSyncError { get; set; }

    /// <summary>Final counts after last successful sync (same as catalog table row counts).</summary>
    public int MakesCount { get; set; }
    public int ModelsCount { get; set; }

    /// <summary>Total makes from NHTSA for the current/last run (for progress).</summary>
    public int TotalMakes { get; set; }

    /// <summary>Makes processed in the models loop (0..TotalMakes).</summary>
    public int MakesProcessed { get; set; }

    /// <summary>Model rows inserted so far during the current/last run.</summary>
    public int ModelsAddedSoFar { get; set; }
}

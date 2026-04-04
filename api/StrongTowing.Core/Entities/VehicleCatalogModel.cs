using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Core.Entities;

/// <summary>US-market vehicle model from NHTSA vPIC (cached locally).</summary>
public class VehicleCatalogModel
{
    public int Id { get; set; }

    public int MakeId { get; set; }
    public VehicleCatalogMake Make { get; set; } = null!;

    /// <summary>NHTSA Model_ID from vPIC.</summary>
    public int NhtsaModelId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
}

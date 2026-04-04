using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Core.Entities;

/// <summary>US-market vehicle make from NHTSA vPIC (cached locally).</summary>
public class VehicleCatalogMake
{
    public int Id { get; set; }

    /// <summary>NHTSA Make_ID from vPIC.</summary>
    public int NhtsaMakeId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public ICollection<VehicleCatalogModel> Models { get; set; } = new List<VehicleCatalogModel>();
}

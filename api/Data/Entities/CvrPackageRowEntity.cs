using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class CvrPackageRowEntity
{
    [Key, MaxLength(64)] public string CvrPackageRowId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string PackageName { get; set; } = "";
    public decimal OrderCost { get; set; }
    public decimal OrderValue { get; set; }
    public decimal VariationCost { get; set; }
    public decimal VariationValue { get; set; }
    public decimal MovementSinceLastSnapshot { get; set; }
}

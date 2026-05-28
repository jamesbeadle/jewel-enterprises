using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class DayworkEntity
{
    [Key, MaxLength(64)] public string DayworkId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public DateTimeOffset WorkedOn { get; set; }
    [MaxLength(256)]     public string SubcontractorReference { get; set; } = "";
    [MaxLength(512)]     public string Description { get; set; } = "";
    [MaxLength(256)]     public string InstructedBy { get; set; } = "";
    public decimal Hours { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal LabourCost { get; set; }
    public decimal PlantCost { get; set; }
    public decimal MaterialsCost { get; set; }
    public decimal UpliftPercent { get; set; }
    public decimal ChargeableAmount { get; set; }
}

public sealed class ContraChargeEntity
{
    [Key, MaxLength(64)] public string ContraChargeId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string SubcontractorReference { get; set; } = "";
    public DateTimeOffset RaisedOn { get; set; }
    [MaxLength(512)]     public string Description { get; set; } = "";
    [MaxLength(128)]     public string Category { get; set; } = "";
    public decimal Amount { get; set; }
    [MaxLength(32)]      public string Status { get; set; } = "";
    public decimal RecoveredAmount { get; set; }
}

public sealed class SubcontractorRetentionEntity
{
    [Key, MaxLength(64)] public string SubcontractorRetentionId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string SubcontractorReference { get; set; } = "";
    public decimal CertifiedAmount { get; set; }
    public decimal RetentionPercent { get; set; }
    public decimal FirstReleasedAmount { get; set; }
    public decimal FinalReleasedAmount { get; set; }
}

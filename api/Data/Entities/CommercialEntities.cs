using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class SiteReportEntity
{
    [Key, MaxLength(64)] public string SiteReportId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public DateTimeOffset PeriodEnd { get; set; }
    [MaxLength(4096)]    public string Narrative { get; set; } = "";
    public int AttendanceDays { get; set; }
    public int OpenSnags { get; set; }
    public decimal ProgressPercent { get; set; }
    public bool IsIssued { get; set; }
}

public sealed class ProgrammeTaskEntity
{
    [Key, MaxLength(64)] public string ProgrammeTaskId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    public DateTimeOffset PlannedStart { get; set; }
    public DateTimeOffset PlannedEnd { get; set; }
    public decimal ProgressPercent { get; set; }
    [MaxLength(64)]      public string? BoqLineItemId { get; set; }
}

public sealed class ValuationEntity
{
    [Key, MaxLength(64)] public string ValuationId { get; set; } = "";
    [MaxLength(64)]      public string ClaimPeriodId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public decimal GrossValue { get; set; }
    public decimal RetentionPercent { get; set; }
    public decimal NetValue { get; set; }
    public bool IsIssued { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
}

public sealed class CvrSnapshotEntity
{
    [Key, MaxLength(64)] public string CvrSnapshotId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public DateTimeOffset SnapshotAt { get; set; }
    public decimal TenderValue { get; set; }
    public decimal ForecastFinalCost { get; set; }
    public decimal ForecastFinalValue { get; set; }
    public decimal MarginPounds { get; set; }
    public decimal MarginPercent { get; set; }
    public int WeeksAheadOrBehind { get; set; }
}

public sealed class CostCodeBudgetEntity
{
    [Key, MaxLength(64)] public string CostCodeBudgetId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(32)]      public string CostCode { get; set; } = "";
    public decimal AllocatedAmount { get; set; }
    public decimal SpentAmount { get; set; }
}

public sealed class TimesheetEntity
{
    [Key, MaxLength(64)] public string TimesheetId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string PersonEmail { get; set; } = "";
    public DateTimeOffset WorkedOn { get; set; }
    public decimal Hours { get; set; }
    [MaxLength(32)]      public string CostCode { get; set; } = "";
    public bool IsApproved { get; set; }
}

public sealed class DefectEntity
{
    [Key, MaxLength(64)] public string DefectId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(1024)]    public string Description { get; set; } = "";
    [MaxLength(256)]     public string Location { get; set; } = "";
    [MaxLength(256)]     public string AssignedToEmail { get; set; } = "";
    public int Status { get; set; }
    public DateTimeOffset RaisedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}

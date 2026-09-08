using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// A site operative whose time is captured through the labour tracking system. Workers are
/// day-rate subcontractor labour (see docs/Labour-Time-Tracking-Scope.md): each normally belongs
/// to a Subcontractor company, and the hourly rate is the agreed day rate ÷ 8. The rate is
/// server-side only — it must never be serialised to the site capture page.
/// </summary>
public sealed class WorkerEntity
{
    [Key, MaxLength(64)] public string WorkerId { get; set; } = "";
    [MaxLength(256)]     public string Name { get; set; } = "";
    [MaxLength(64)]      public string? SubcontractorId { get; set; }
    public decimal HourlyRate { get; set; }
    public bool IsActive { get; set; } = true;
    [MaxLength(256)]     public string ContactEmail { get; set; } = "";
    [MaxLength(64)]      public string ContactPhone { get; set; } = "";
    // Sole trader (2026-08-31, the accountant's month-end ask): the worker bills Dext/Xero under
    // their OWN name and is their own settlement counterparty — no directory company is invented
    // for them. Ignored whenever SubcontractorId is set: a real company link wins.
    public bool IsSoleTrader { get; set; }
    // Engagement window (2026-08-31): the chase list and confidence figures only EXPECT time
    // inside it. Null = open-ended on that side. Recorded time outside the window still counts —
    // the window bounds expectation, never data.
    public DateTimeOffset? EngagedFrom { get; set; }
    public DateTimeOffset? EngagedTo { get; set; }
}

/// <summary>
/// Rate history for a worker. Costing uses the rate effective on the worked date, and the
/// resolved rate is snapshotted onto the timesheet at approval so historic cost never changes
/// when a rate changes.
/// </summary>
public sealed class WorkerRateHistoryEntity
{
    [Key, MaxLength(64)] public string WorkerRateHistoryId { get; set; } = "";
    [MaxLength(64)]      public string WorkerId { get; set; } = "";
    public decimal HourlyRate { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
}

/// <summary>Controls whose names appear on a project's site sign-in sheet.</summary>
public sealed class ProjectWorkerAssignmentEntity
{
    [Key, MaxLength(64)] public string ProjectWorkerAssignmentId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(64)]      public string WorkerId { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// One worker's presence on one site for one working day — the daily site register. Created at
/// sign-in; SignedOutAt set when the end-of-day allocation is submitted. One row per worker per
/// project per day.
/// </summary>
public sealed class SiteAttendanceEntity
{
    [Key, MaxLength(64)] public string SiteAttendanceId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(64)]      public string WorkerId { get; set; } = "";

    // The working date (midnight UTC). Day-uniqueness and "today" checks key off this.
    public DateTimeOffset WorkDate { get; set; }
    public DateTimeOffset SignedInAt { get; set; }
    public DateTimeOffset? SignedOutAt { get; set; }
}

/// <summary>
/// Marks a Xero purchase line as settlement of approved timesheets rather than fresh cost
/// (docs/Labour-Time-Tracking-Scope.md §6 — the approved timesheet is the timely actual, the
/// paid invoice is the truth at settlement). Covered lines are excluded from the cost-of-sales
/// aggregation to prevent labour double-counting.
/// </summary>
public sealed class XeroLineTimesheetCoverEntity
{
    [Key, MaxLength(64)]  public string XeroLineTimesheetCoverId { get; set; } = "";
    [MaxLength(140)]      public string XeroLedgerLineId { get; set; } = "";
    [MaxLength(64)]       public string ProjectId { get; set; } = "";
    [MaxLength(64)]       public string SubcontractorId { get; set; } = "";
    // The worker this line settles (2026-09-08): the coding run marks a company bill's cover per
    // worker, so each worker's schedule reconciles against their own lines. Null is the
    // counterparty's cover as a whole — a cover marked by hand, or from before this column.
    [MaxLength(64)]       public string? WorkerId { get; set; }
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    [MaxLength(256)]      public string CreatedByEmail { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// An accepted difference between a subcontractor's invoice and the approved timesheet cost it
/// settles — resolution path (4) in the scope's settlement model. Posts into the cost-of-sales
/// aggregation as its own visible line so posted cost always equals cash paid and nothing is
/// silently absorbed.
/// </summary>
public sealed class LabourSettlementVarianceEntity
{
    [Key, MaxLength(64)]  public string LabourSettlementVarianceId { get; set; } = "";
    [MaxLength(64)]       public string ProjectId { get; set; } = "";
    [MaxLength(32)]       public string CostCode { get; set; } = "";
    [MaxLength(64)]       public string SubcontractorId { get; set; } = "";
    public decimal Amount { get; set; }
    [MaxLength(1024)]     public string Reason { get; set; } = "";
    [MaxLength(140)]      public string? XeroLedgerLineId { get; set; }
    [MaxLength(256)]      public string CreatedByEmail { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

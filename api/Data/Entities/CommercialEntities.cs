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

// A finish-to-start dependency between two programme tasks (successor starts LagDays after the
// predecessor finishes; lag may be negative for overlap). No FK constraints, by-id only, matching
// every JPMS table.
public sealed class ProgrammeTaskLinkEntity
{
    [Key, MaxLength(64)] public string ProgrammeTaskLinkId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(64)]      public string PredecessorTaskId { get; set; } = "";
    [MaxLength(64)]      public string SuccessorTaskId { get; set; } = "";
    public int LagDays { get; set; }
}

// A named snapshot of the whole programme at a point in time — the yardstick movement is measured
// against, and the contemporaneous record behind NOD/EOT claims.
public sealed class ProgrammeBaselineEntity
{
    [Key, MaxLength(64)] public string ProgrammeBaselineId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string Label { get; set; } = "";
    [MaxLength(256)]     public string TakenByEmail { get; set; } = "";
    public DateTimeOffset TakenAt { get; set; }
}

// One task's dates as they stood when the baseline was taken. Title is copied so the snapshot
// stays meaningful even if the live task is later renamed.
public sealed class ProgrammeBaselineTaskEntity
{
    [Key, MaxLength(64)] public string ProgrammeBaselineTaskId { get; set; } = "";
    [MaxLength(64)]      public string ProgrammeBaselineId { get; set; } = "";
    [MaxLength(64)]      public string ProgrammeTaskId { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    public DateTimeOffset PlannedStart { get; set; }
    public DateTimeOffset PlannedEnd { get; set; }
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

    // Committed-but-not-yet-spent value, e.g. an approved variation order awarded against this code.
    public decimal CommittedAmount { get; set; }
}

/// <summary>A named roll-up of cost centres shown as one line on the project's Financials
/// tab. Presentation only — figures remain stored per cost centre; members link below.</summary>
public sealed class CostCentreGroupEntity
{
    [Key, MaxLength(64)] public string CostCentreGroupId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(128)]     public string Name { get; set; } = "";
}

/// <summary>One cost centre inside a roll-up. The unique (ProjectId, CostCode) index
/// keeps each centre in at most one group per project.</summary>
public sealed class CostCentreGroupMemberEntity
{
    [Key, MaxLength(64)] public string CostCentreGroupMemberId { get; set; } = "";
    [MaxLength(64)]      public string CostCentreGroupId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(32)]      public string CostCode { get; set; } = "";
}

/// <summary>A reconciliation package: work orders (cost side) matched against valuation
/// sales lines (sales side) at the level the work was bought and sold — see the
/// members below. Presentation only. Locking freezes the snapshot columns, realising
/// profit / loss against actual invoiced cost.</summary>
public sealed class ReconciliationPackageEntity
{
    [Key, MaxLength(64)] public string ReconciliationPackageId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(128)]     public string Name { get; set; } = "";

    public bool IsLocked { get; set; }
    public DateTimeOffset? LockedAt { get; set; }
    // Snapshot frozen at lock; meaningless while unlocked.
    public decimal LockedSalesValue { get; set; }
    public decimal LockedClaimedToDate { get; set; }
    public decimal LockedTargetCost { get; set; }
    public decimal LockedWoCommitted { get; set; }
    public decimal LockedInvoicedCost { get; set; }
    public decimal LockedProfitLoss { get; set; }
}

/// <summary>A work order inside a package. The unique (ProjectId, WorkOrderId) index
/// keeps each order in at most one package.</summary>
public sealed class ReconciliationPackageOrderEntity
{
    [Key, MaxLength(64)] public string ReconciliationPackageOrderId { get; set; } = "";
    [MaxLength(64)]      public string ReconciliationPackageId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(64)]      public string WorkOrderId { get; set; } = "";
}

/// <summary>A sales line's share inside a package — the whole line's value or a partial
/// £ amount (signed like the line). A line's value can be shared across packages but the
/// slices may never total past the line; the save command enforces it.</summary>
public sealed class ReconciliationPackageSalesLineEntity
{
    [Key, MaxLength(64)] public string ReconciliationPackageSalesLineId { get; set; } = "";
    [MaxLength(64)]      public string ReconciliationPackageId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(64)]      public string ValuationLineItemId { get; set; } = "";
    public decimal Amount { get; set; }
}

/// <summary>A direct purchase cost's share inside a package — a £ slice of an allocated
/// Xero purchase line that isn't paying any work order (materials bought directly for
/// the packaged scope, e.g. roofing supplies alongside the roofer's labour-only order).
/// Signed like the line's net (credit notes negative). Only whole-line allocations can
/// be sliced (centre-split lines can't — same rule as work-order links); the slices of
/// one line across all packages may never exceed its non-work-order remainder.</summary>
public sealed class ReconciliationPackageCostLineEntity
{
    [Key, MaxLength(64)] public string ReconciliationPackageCostLineId { get; set; } = "";
    [MaxLength(64)]      public string ReconciliationPackageId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    // 140 like every table keyed on this id: it's "{TransactionId}:{LineItemId}", two
    // Xero GUIDs plus a colon (~73 chars) — 64 would truncate and never match again.
    [MaxLength(140)]     public string XeroLedgerLineId { get; set; } = "";
    public decimal Amount { get; set; }
}

/// <summary>Cost-side completion per cost centre per project (0–100), edited inline
/// on the Financials tab. Distinct from sales-side completion, which is derived from
/// the latest claim's cumulative claimed value on the valuation report.</summary>
public sealed class CostCentreCostProgressEntity
{
    [Key, MaxLength(64)] public string CostCentreCostProgressId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(32)]      public string CostCode { get; set; } = "";
    public decimal CostCompletionPercent { get; set; }

    // Locked down on the Financials tab: no more money is to be spent against this centre,
    // so its remaining drawdown reads as realised profit / loss instead of available funds.
    public bool IsFinalised { get; set; }
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

    // Labour tracking (docs/Labour-Time-Tracking-Scope.md). WorkerId/SiteAttendanceId are empty
    // on legacy rows submitted before the site capture flow existed. Status mirrors
    // TimesheetStatus (contracts); IsApproved is kept in sync for legacy readers.
    [MaxLength(64)]      public string WorkerId { get; set; } = "";
    [MaxLength(64)]      public string SiteAttendanceId { get; set; } = "";
    public int Status { get; set; }

    // Costing snapshot, written at approval: the worker's rate effective on WorkedOn and the
    // resulting cost. Zero until approved — unapproved time is never cost.
    public decimal RateApplied { get; set; }
    public decimal CostAmount { get; set; }
    [MaxLength(256)]     public string ApprovedByEmail { get; set; } = "";
    public DateTimeOffset? ApprovedAt { get; set; }
    [MaxLength(1024)]    public string RejectionReason { get; set; } = "";
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

    // The supplier the defect is raised with — a directory record id (SubcontractorEntity; the
    // Subcontractor / Supplier categories), the way a work order names its supplier. Loose string
    // id, no FK, house style. Null = no company picked (pre-2026-09-07 rows carry only the
    // free-typed AssignedToEmail). Added by AddDefectSupplierAndTodoAboutRecord.
    [MaxLength(64)] public string? SubcontractorId { get; set; }

    // First time the defect was SENT to the supplier from its page (the compose pipeline stamps
    // it when a sent email carries this defect's tag and goes to the supplier's address), and by
    // whom. Null = never sent. Later sends are chases — visible in the correspondence, not here.
    public DateTimeOffset? SentToSupplierAt { get; set; }
    [MaxLength(256)] public string? SentToSupplierByEmail { get; set; }

    // Sequential, human-readable defect number (rendered as DEF-0001). Global — like to-do and
    // work-order numbers — so the tag stem is unique across the flat JPMS mailbox-category space.
    // Minted by RaiseDefectHandler; the AddDefectNumbers migration backfilled existing rows.
    public int Number { get; set; }

    // The canonical reference this defect's emails are tagged with ("DEF-0001" -> "JPMS/DEF-0001").
    // Computed, not stored. The id-derived fallback covers any unnumbered row (there should be
    // none after the backfill) so two such rows can never share the "DEF-0000" stem.
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Reference => Number > 0
        ? $"DEF-{Number:0000}"
        : $"DEF-{DefectId.PadRight(8, '0')[..8].ToUpperInvariant()}";
}

namespace Jewel.JPMS.Models;

public sealed record SiteReport(
    string SiteReportId,
    string ProjectId,
    DateTimeOffset PeriodEnd,
    string Narrative,
    int AttendanceDays,
    int OpenSnags,
    decimal ProgressPercent,
    bool IsIssued);

public sealed record ProgrammeTask(
    string ProgrammeTaskId,
    string ProjectId,
    string Title,
    DateTimeOffset PlannedStart,
    DateTimeOffset PlannedEnd,
    decimal ProgressPercent,
    string? BoqLineItemId);

// A finish-to-start dependency between two programme tasks: the successor cannot start until the
// predecessor finishes (plus LagDays, which may be negative for an overlap). Finish-to-start is the
// only link type — it covers the overwhelming majority of construction sequencing and keeps the
// programme legible; other link types can be modelled with lag.
public sealed record ProgrammeTaskLink(
    string ProgrammeTaskLinkId,
    string ProjectId,
    string PredecessorTaskId,
    string SuccessorTaskId,
    int LagDays);

// A named snapshot of the whole programme at a point in time — the yardstick that makes movement
// visible. The current programme is always compared against the latest baseline; the slippage that
// comparison exposes is also the contemporaneous evidence behind NOD/EOT claims.
public sealed record ProgrammeBaseline(
    string ProgrammeBaselineId,
    string ProjectId,
    string Label,
    string TakenByEmail,
    DateTimeOffset TakenAt);

// One task's dates as they stood when a baseline was taken. Title is copied so the snapshot stays
// meaningful even if the live task is later renamed.
public sealed record ProgrammeBaselineTask(
    string ProgrammeBaselineTaskId,
    string ProgrammeBaselineId,
    string ProgrammeTaskId,
    string Title,
    DateTimeOffset PlannedStart,
    DateTimeOffset PlannedEnd);

// A programme task's confirmed link to one cost centre on the valuation report — the saved half
// of the mapping a draft programme update works from (decision 2026-09-08: rules + Claude
// propose, the reviewer confirms, the confirmation is kept on the task so the next valuation's
// draft never asks again). A task may sit on several centres ("Insulation, UFH & Screed") and a
// centre may feed several tasks (plumbing on every floor). No FK, by-id, like every JPMS link.
public sealed record ProgrammeTaskCostCentre(
    string ProgrammeTaskCostCentreId,
    string ProjectId,
    string ProgrammeTaskId,
    string CostCode);

// Everything the Programme tab's programme view needs in one round trip: the live tasks, their
// dependency links, and the latest baseline (with its task snapshots) to overlay movement against.
// Baselines lists every baseline taken, newest first (so Baselines[0] is the current yardstick,
// the same one Baseline carries), for the tab's baseline-management view. CostCentres is every
// task's confirmed cost-centre mapping — the saved half of the valuation-to-programme draft.
public sealed record ProgrammeDetail(
    IReadOnlyList<ProgrammeTask> Tasks,
    IReadOnlyList<ProgrammeTaskLink> Links,
    ProgrammeBaseline? Baseline,
    IReadOnlyList<ProgrammeBaselineTask> BaselineTasks,
    IReadOnlyList<ProgrammeBaseline> Baselines,
    IReadOnlyList<ProgrammeTaskCostCentre> CostCentres);

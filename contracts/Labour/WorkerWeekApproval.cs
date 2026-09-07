using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

// The connector's approval leg (2026-08-28, the accountant's ask): code and approve a worker's
// week from an AI session the way the Labour tab does it — same handlers, same gates, same
// uncoded and budget hard-blocks — keyed by worker NAME and dates, because an AI caller never
// holds the grid's opaque timesheet ids. Approval remains final: an approved timesheet's cost
// has posted and its code and hours can no longer be changed, which is why approve carries the
// connector's confirm-first gate.

/// <summary>
/// Applies ONE cost code to a worker's Submitted timesheets in a week on one project — the bulk
/// twin of the grid's per-row Adjust, sharing AdjustTimesheet's handler per row (hours pass
/// through unchanged; approved rows are immutable and report so). Dates narrows the act to
/// specific days; null means every Submitted day of that worker's week on the project.
/// </summary>
public sealed record CodeWorkerWeekByName(
    string ProjectId,
    string WorkerName,
    DateTimeOffset WeekStart,
    string CostCode,
    IReadOnlyList<DateTimeOffset>? Dates = null) : ICommand<WorkerWeekCodingResult>;

/// <summary>What happened to one day: Coded = the Submitted timesheet now carries the cost code;
/// otherwise Detail says why not ("already approved — immutable").</summary>
public sealed record WorkerDayCodingOutcome(
    DateTimeOffset Date,
    bool Coded,
    string Detail);

public sealed record WorkerWeekCodingResult(
    string WorkerId,
    string WorkerName,
    DateTimeOffset WeekStart,
    string CostCode,
    IReadOnlyList<WorkerDayCodingOutcome> Outcomes);

/// <summary>
/// Approves a worker's Submitted timesheets in a week on one project, exactly as the grid's
/// Approve selected: delegates the resolved ids to ApproveTimesheetsHandler, so rate resolution,
/// cost snapshotting, the uncoded refusal and the per-cost-code budget hard-block all apply
/// unchanged, and partial success reports per day. Dates narrows to specific days; null means
/// every Submitted day. ApprovedByEmail is stamped server-side from the connector caller.
/// </summary>
// AllowOverBudget/OverBudgetReason (2026-08-29): connector parity with the Labour tab's deliberate
// over-budget approval — MD/FD/Admin only (gated in the Authorisation), a reason is mandatory, and
// every overridden day writes a LabourBudgetOverridden audit row via the shared ApproveTimesheets
// handler. For everyone else the per-cost-code hard-block stays absolute.
public sealed record ApproveWorkerWeekByName(
    string ProjectId,
    string WorkerName,
    DateTimeOffset WeekStart,
    IReadOnlyList<DateTimeOffset>? Dates = null,
    string ApprovedByEmail = "",
    bool AllowOverBudget = false,
    string OverBudgetReason = "") : ICommand<WorkerWeekApprovalResult>;

/// <summary>What happened to one day: Approved = cost has posted (Hours at the worker's rate on
/// CostCode); otherwise Detail carries the handler's own refusal — uncoded, budget hard-block,
/// already approved, rejected awaiting resubmit.</summary>
public sealed record WorkerDayApprovalOutcome(
    DateTimeOffset Date,
    bool Approved,
    decimal Hours,
    string CostCode,
    string Detail);

public sealed record WorkerWeekApprovalResult(
    string WorkerId,
    string WorkerName,
    DateTimeOffset WeekStart,
    IReadOnlyList<WorkerDayApprovalOutcome> Outcomes);

/// <summary>
/// Rejects a worker's Submitted timesheet(s) on one date back to them with a reason they will
/// read — the grid's Reject, keyed by name and date. Approved rows are immutable and refuse.
/// </summary>
public sealed record RejectWorkerDayByName(
    string ProjectId,
    string WorkerName,
    DateTimeOffset Date,
    string Reason) : ICommand<TimesheetDetail>;

// ---- Corrections by name (2026-09-07, the accountant's ask) -----------------------------------
// The connector twins of UnapproveTimesheet / MoveTimesheet: keyed by worker name and date, and
// wrappers over the SAME handlers, so an FD correcting a day in Claude meets exactly the gates,
// refusals and audit rows the Labour tab's Actions menu gives them.

/// <summary>Puts a worker's approved timesheet on one date on one project back to Submitted with
/// a reason — withdrawing its posted cost. MD/FD/Admin only. ReversedByEmail is stamped
/// server-side from the connector caller.</summary>
public sealed record UnapproveWorkerDayByName(
    string ProjectId,
    string WorkerName,
    DateTimeOffset Date,
    string Reason,
    string ReversedByEmail = "") : ICommand<TimesheetDetail>;

/// <summary>Moves a worker's timesheet on one date from one project to another with a reason —
/// any status, everything on the row kept. An approved day meets the destination's budget
/// hard-block; AllowOverBudget is the MD/FD/Admin override. MovedByEmail is stamped server-side
/// from the connector caller.</summary>
public sealed record MoveWorkerDayByName(
    string ProjectId,
    string WorkerName,
    DateTimeOffset Date,
    string ToProjectId,
    string Reason,
    bool AllowOverBudget = false,
    string MovedByEmail = "") : ICommand<TimesheetMoveResult>;

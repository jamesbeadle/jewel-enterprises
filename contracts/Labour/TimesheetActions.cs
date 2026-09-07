using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

/// <summary>All timesheets for a project with labour detail (worker names, status, £ for
/// commercial roles). The Labour tab filters to a week client-side.</summary>
public sealed record ListTimesheetDetailsForProject(string ProjectId)
    : IQuery<IReadOnlyList<TimesheetDetail>>;

/// <summary>PM manual entry on a worker's behalf (missed sign-out path). Creates a
/// Submitted timesheet for the given worked date.</summary>
public sealed record AddWorkerTimesheet(string ProjectId, string WorkerId, DateTimeOffset WorkedOn,
    decimal Hours, string CostCode) : ICommand<TimesheetDetail>;

/// <summary>PM correction before approval: change hours and/or re-code to another cost code.</summary>
public sealed record AdjustTimesheet(string TimesheetId, decimal Hours, string CostCode)
    : ICommand<TimesheetDetail>;

/// <summary>
/// Batch approval. Per timesheet: resolves the worker's rate effective on the worked date,
/// snapshots rate and cost, and enforces the budget hard-block (workflow 07-D) — approval is
/// rejected for a cost code whose remaining budget the new cost would exceed. Partial success:
/// approvable timesheets approve, failures return with reasons.
///
/// AllowOverBudget (2026-08-29, the accountant's ask) is the deliberate exception to the
/// hard-block: the MD/FD/Admin only (endpoint-gated), with a mandatory reason, approves the
/// blocked rows anyway — the cost still posts, the overspend stays red on Financials, and each
/// overridden row is written to the audit trail with who, why, and the block it overrode. It
/// never bypasses the other refusals (uncoded, no worker, already decided).
/// </summary>
public sealed record ApproveTimesheets(string ProjectId, IReadOnlyList<string> TimesheetIds,
    bool AllowOverBudget = false, string OverBudgetReason = "")
    : ICommand<LabourApprovalResult>;

/// <summary>Rejection re-opens the day for the worker on the capture page; no deadline is
/// enforced and the PM can always correct and approve on the worker's behalf instead.</summary>
public sealed record RejectTimesheet(string TimesheetId, string Reason) : ICommand<TimesheetDetail>;

// ---- Corrections (2026-09-07, the accountant's ask) -------------------------------------------
// Approval posts cost and an approved row is immutable to the approver — that rule stands. What
// was missing was any route back when a day was approved on the wrong project, or approved
// wrong: the only remedies were a settlement variance or a request to the developer. These two
// commands are that route, and they are deliberately narrow: MD/FD/Admin only (the roles that
// may already sign an overspend), a mandatory reason, and an audit row that keeps what the day
// lost. Both refuse once the day's month has gone downstream — a signed-off week part (unapprove
// only: sign-off is a view over approval), a settlement cover spanning the day, or a Xero coding
// run that has posted the worker's month — because those are the points past which the trail
// would no longer tie to the money.

/// <summary>
/// Puts ONE approved timesheet back to Submitted, withdrawing its posted cost: the rate/cost
/// snapshot, approver and approval time are cleared on the row and preserved on the
/// LabourApprovalReversed audit row with the reason. The day is then the approver's again —
/// Adjust, code, approve, or reject back to the worker.
/// </summary>
public sealed record UnapproveTimesheet(string TimesheetId, string Reason) : ICommand<TimesheetDetail>;

/// <summary>
/// Moves ONE timesheet — any status — to another project, keeping its date, hours, cost code,
/// status and (when approved) its rate/cost snapshot, approver and approval time; a linked site
/// register row moves with it. An approved day re-meets the destination's per-cost-code budget
/// hard-block exactly as approval would: a block comes back as
/// <see cref="TimesheetMoveResult.BudgetBlockReason"/> with nothing moved, and AllowOverBudget
/// (the same MD/FD/Admin override, same reason, same LabourBudgetOverridden audit row) moves it
/// anyway. Writes a LabourDayMoved audit row on BOTH projects.
/// </summary>
public sealed record MoveTimesheet(string TimesheetId, string ToProjectId, string Reason,
    bool AllowOverBudget = false) : ICommand<TimesheetMoveResult>;

/// <summary>Moved = the row now sits on the destination (Timesheet is its new state). Otherwise
/// BudgetBlockReason carries the destination's budget refusal — the row is untouched and the
/// caller may re-send with AllowOverBudget.</summary>
public sealed record TimesheetMoveResult(bool Moved, TimesheetDetail Timesheet, string? BudgetBlockReason);

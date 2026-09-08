using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

// The per-worker monthly settlement schedule and its Dext/Xero coding contract (scope §6, §6a),
// plus the effective-dated Xero mapping admin (scope §3). CommercialTeam-gated throughout.

/// <summary>Every worker's settlement schedule for the month, with reconciliation verdicts.</summary>
public sealed record GetSettlementSchedules(int Year, int Month) : IQuery<SettlementScheduleSnapshot>;

/// <summary>
/// Adds a non-labour line (materials / travel) to a worker's month at sign-off level — these
/// change both the Xero account and the CIS treatment (no deduction). Labour lines are never
/// added here: they come from approved timesheets only.
/// </summary>
public sealed record AddWorkerSettlementLine(
    string WorkerId, int Year, int Month, string ProjectId, string CostCode,
    SettlementLineNature Nature, decimal Amount, string Note) : ICommand<Acknowledgement>;

public sealed record RemoveWorkerSettlementLine(string WorkerSettlementLineId) : ICommand<Acknowledgement>;

/// <summary>Both effective-dated Xero maps, current and historic rows.</summary>
public sealed record ListXeroMappings : IQuery<XeroMappingsSnapshot>;

/// <summary>
/// Points a project at a Xero site tracking option from now on. The previous row (if any) is
/// closed with EffectiveTo = now, never edited — historic reads keep translating through it.
/// </summary>
// XeroTrackingOptionId is nullable (2026-08-31) so the connector's schema marks it optional —
// the coding run matches by NAME; the id is a convenience when the caller holds it. The handler
// has always coalesced null to "".
public sealed record SetSiteXeroMapping(string ProjectId, string? XeroTrackingOptionId, string XeroTrackingOptionName)
    : ICommand<Acknowledgement>;

/// <summary>Same effective-dated contract for a cost code: its tracking option and the account
/// code per line nature (blank = the run's configured default for that nature).</summary>
// Everything but CostCode is nullable (2026-08-31) so the connector's schema marks them
// optional — a blank tracking option codes under the cost code's own name, and a blank account
// code is legal to STORE (the coding run then skips lines of that nature and says so). The
// handler has always coalesced nulls to "".
public sealed record SetCostCodeXeroMapping(
    string CostCode, string? XeroTrackingOptionId, string? XeroTrackingOptionName,
    string? LabourAccountCode, string? MaterialsAccountCode, string? TravelAccountCode)
    : ICommand<Acknowledgement>;

/// <summary>
/// The §6a automation, run per month (optionally narrowed to named workers). For each fully
/// signed-off worker-month it finds the worker's existing bill for the month — covered, or
/// recognised by contact + period, draft OR authorised (2026-09-03: the cover route is the sole
/// trader's normal path, so an authorised bill is the normal case) — and recodes its lines to
/// the schedule's split, keeping the bill's total, VAT treatment, status and cover; or stages a
/// draft bill only where no bill exists at all. A bill it cannot recode (paid, credited, voided)
/// skips with its status named — never a second bill. Mapping gaps skip-and-report; nothing is
/// ever guessed. Every write and every skip is recorded against the worker-month.
/// DryRun (2026-09-03) reports what the run WOULD do per worker and writes nothing anywhere.
/// Workers who settle through the same company are coded as one (2026-09-08, item J): the
/// company's one bill for the month is recoded once to every worker's lines, cover marked per
/// worker, so naming one worker on a company bill runs — and reports — every worker on it. A
/// matched bill that Xero says is voided or deleted is followed to its live re-issue under the
/// same contact, number and period (item K).
/// </summary>
public sealed record RunXeroCoding(int Year, int Month, IReadOnlyList<string>? WorkerIds, bool DryRun = false)
    : ICommand<IReadOnlyList<XeroCodingRunResult>>;

/// <summary>
/// Approves a covered labour bill in Xero (2026-09-08, the accountant's ask once J and K ran):
/// DRAFT → AUTHORISED, only once every worker-month the bill covers reads Matches — the
/// settlement view's own rule, checked again server-side against the bill as Xero holds it
/// now. The lines stay exactly as coded; nothing is paid. Recorded as BillApproved against
/// every worker-month on the bill, so the history reads recoded → approved. Confirm-first.
/// ApprovedByEmail is stamped server-side from the caller.
/// </summary>
public sealed record ApproveLabourBill(string XeroInvoiceId, int Year, int Month, string ApprovedByEmail = "")
    : ICommand<LabourBillApproval>;

/// <summary>
/// Resets a worker-month's coding outcome (2026-09-03): appends a Reset outcome to the run
/// history (who, why, what it was) so the run-once gate — which reads the latest outcome —
/// lets the month be coded again. Touches nothing in Xero; the reason is mandatory.
/// </summary>
public sealed record ResetXeroCodingOutcome(string WorkerId, int Year, int Month, string Reason)
    : ICommand<Acknowledgement>;

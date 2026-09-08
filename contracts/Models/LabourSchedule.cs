namespace Jewel.JPMS.Models;

/// <summary>The per-worker monthly reconciliation verdict (scope §6).</summary>
public enum ScheduleVerdict
{
    /// <summary>Covered bill total equals the schedule gross (to the penny), net of any
    /// variance posted against the covered lines.</summary>
    Matches = 0,
    /// <summary>A bill is covered but its total differs from the schedule.</summary>
    VarianceOpen = 1,
    /// <summary>No covering bill has been marked for the period yet — chase it.</summary>
    NoBillYet = 2,
    /// <summary>Nothing approved and nothing invoiced — an empty month.</summary>
    Nothing = 3,
}

/// <summary>One line of a worker's settlement schedule: site × cost code × nature.</summary>
public sealed record ScheduleLine(
    string ProjectId,
    string ProjectName,
    string CostCode,
    SettlementLineNature Nature,
    decimal Amount,
    /// <summary>Set on manually added (materials/travel) lines so they can be removed.</summary>
    string? WorkerSettlementLineId);

/// <summary>
/// One worker's settlement schedule for one month: what the signed-off timesheets say they are
/// owed, split the way the covering bill should be coded in Dext/Xero, with the CIS deduction
/// and the reconciliation verdict against what Xero actually holds.
/// </summary>
// SubcontractorId is the settlement COUNTERPARTY id (2026-08-31): the linked company's id, or
// the worker's own id when they are a flagged sole trader billing under their own name. Covers
// are stored against it and the coding run reconciles by it; SubcontractorName follows suit
// (the worker's name for a sole trader), so staged draft bills carry the right Xero contact.
public sealed record WorkerSettlementSchedule(
    string WorkerId,
    string WorkerName,
    string? SubcontractorId,
    string SubcontractorName,
    IReadOnlyList<ScheduleLine> Lines,
    decimal GrossLabour,
    decimal GrossOther,
    decimal GrossTotal,
    decimal CisRatePercent,
    decimal CisDeduction,
    decimal NetPayable,
    decimal CoveredBillTotal,
    decimal Difference,
    ScheduleVerdict Verdict,
    /// <summary>True when every week with approved time in the month carries a sign-off marker —
    /// the §6a automation refuses a worker-month that is not fully signed off.</summary>
    bool FullySignedOff,
    /// <summary>The latest Xero coding run for this worker-month, empty when none has run.</summary>
    string LastCodingOutcome,
    DateTimeOffset? LastCodedAt,
    /// <summary>The bill this month is covered by, when its covers all point at one bill
    /// (2026-09-08) — null while nothing covers it, or while its covers span two bills.</summary>
    CoveredBill? CoveredBill = null,
    /// <summary>The accepted difference posted against this month's covered lines (2026-09-08),
    /// signed as add_labour_settlement_variance signs it (positive = paying more than the
    /// timesheets say). Difference and Verdict read net of it.</summary>
    decimal PostedVariance = 0m);

/// <summary>
/// The bill a worker's month is covered by, as the ledger last saw it (2026-09-08): which bill,
/// its status, the workers it covers — and whether it can be approved from the portal, which it
/// can while it is still DRAFT (or SUBMITTED) and every worker on it reads Matches.
/// </summary>
public sealed record CoveredBill(
    string XeroInvoiceId,
    string Label,
    string Status,
    decimal Total,
    IReadOnlyList<string> WorkerNames,
    bool IsApprovable,
    /// <summary>The worker's own covered lines on the bill — what a settlement variance is
    /// posted against so it nets into this month.</summary>
    IReadOnlyList<string> LineIds);

/// <summary>What approving a covered labour bill did (2026-09-08): the bill, its status now,
/// whether Xero already had it approved, and the workers whose months it settles.</summary>
public sealed record LabourBillApproval(
    string XeroInvoiceId,
    string Label,
    string Status,
    bool WasAlreadyApproved,
    IReadOnlyList<string> WorkerNames);

/// <summary>The month's schedules plus the chase counts the dashboard chips show.</summary>
public sealed record SettlementScheduleSnapshot(
    int Year,
    int Month,
    IReadOnlyList<WorkerSettlementSchedule> Workers,
    int InvoicesToChase,
    int WorkersToReconcile);

// ---- Xero mapping admin (scope §3/§6) -------------------------------------------------------

public sealed record SiteXeroMapping(
    string SiteXeroMappingId,
    string ProjectId,
    string ProjectName,
    string XeroTrackingOptionId,
    string XeroTrackingOptionName,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo);

public sealed record CostCodeXeroMapping(
    string CostCodeXeroMappingId,
    string CostCode,
    string XeroTrackingOptionId,
    string XeroTrackingOptionName,
    string LabourAccountCode,
    string MaterialsAccountCode,
    string TravelAccountCode,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo);

public sealed record XeroMappingsSnapshot(
    IReadOnlyList<SiteXeroMapping> Sites,
    IReadOnlyList<CostCodeXeroMapping> CostCodes);

// ---- §6a: the automated coding run ----------------------------------------------------------

public enum XeroCodingOutcome
{
    /// <summary>The worker's existing bill for the month (Dext-arrived, draft OR authorised —
    /// the cover route's normal state) was recoded to the schedule's split, its total, VAT
    /// treatment and cover kept.</summary>
    BillRecoded = 0,
    /// <summary>No bill had arrived; a draft bill matching the schedule was staged.</summary>
    DraftStaged = 1,
    /// <summary>The run skipped this worker-month and reported why (mapping gap, not signed
    /// off, already coded, bill paid…). Nothing was written.</summary>
    Skipped = 2,
    /// <summary>Xero rejected the write; Detail carries its own words.</summary>
    Failed = 3,
    /// <summary>Dry run (2026-09-03): the run WOULD recode the named bill. Nothing written, not
    /// recorded against the worker-month.</summary>
    WouldRecodeBill = 4,
    /// <summary>Dry run: the run WOULD stage a draft bill. Nothing written, not recorded.</summary>
    WouldStageDraft = 5,
    /// <summary>The worker-month's coding outcome was reset by a person (2026-09-03) so the run
    /// can take it again — Detail carries who, why and what it was before. Recorded, so the
    /// history reads: staged → reset → recoded.</summary>
    Reset = 6,
    /// <summary>The covered bill was approved in Xero from the portal (2026-09-08): DRAFT →
    /// AUTHORISED, once every worker on it read Matches. Recorded against every worker-month
    /// the bill covers; a written month, like BillRecoded.</summary>
    BillApproved = 7,
}

/// <summary>The outcomes that mean a worker-month is WRITTEN to Xero — read the same way by the
/// run-once gate, the timesheet-correction guards, the chase list, the reset and the settlement
/// table's Reset button (2026-09-08).</summary>
public static class XeroCodingOutcomes
{
    public static readonly int[] WrittenValues =
        { (int)XeroCodingOutcome.BillRecoded, (int)XeroCodingOutcome.DraftStaged, (int)XeroCodingOutcome.BillApproved };

    public static bool IsWritten(XeroCodingOutcome outcome) => WrittenValues.Contains((int)outcome);

    public static bool IsWritten(int storedOutcome) => WrittenValues.Contains(storedOutcome);

    public static bool IsWritten(string storedOutcome) =>
        Enum.TryParse<XeroCodingOutcome>(storedOutcome, out var outcome) && IsWritten(outcome);
}

public sealed record XeroCodingRunResult(
    string WorkerId,
    string WorkerName,
    XeroCodingOutcome Outcome,
    string Detail,
    string XeroBillId);

/// <summary>What the coding run reports for a month (2026-09-03): the per-worker outcomes plus
/// whether this was a dry run — a preview never writes and never records.</summary>
public sealed record XeroCodingRunReport(
    int Year,
    int Month,
    bool DryRun,
    IReadOnlyList<XeroCodingRunResult> Outcomes);

using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Xero;

// Xero ledger allocation — reconciling accounts (Xero) with projects (JPMS).
// Purchase invoice LINES pulled from Xero are stored in JPMS with an
// allocation status; each line is allocated to a JPMS project + master cost
// centre (00001..00137 — deliberately independent of Xero's own tracking
// codes). Syncing upserts by Xero line id: new lines arrive Unallocated and
// existing lines have their Xero facts refreshed without ever touching the
// allocation. A line is allocated to ONE project, but its value can be SPLIT
// across multiple cost centres (Splits — net amounts summing to the line net).
//
// Write-back: bills arrive from Dext as DRAFT. Once every stored line of a
// draft (or submitted) bill is allocated, JPMS writes the allocation back to
// Xero — Sites + Cost Code tracking per line, splitting Xero lines where the
// allocation is split — and approves the invoice (DRAFT → AUTHORISED). Bills
// already approved outside JPMS are still allocated portal-side only.

// Disputed (2026-08-14): a cost the director contests, parked in its own bucket while he and the
// accountant talk it through on the allocation page — a message thread per line, coding settable
// mid-dispute, and either side resolves it back into the queue.
public enum XeroAllocationStatus { Unallocated = 0, Allocated = 1, Ignored = 2, Bucketed = 3, Disputed = 4 }

/// <summary>
/// SetProject is the half-step before Allocate: it persists the project on a
/// line that STAYS Unallocated (so it sits in that project's queue awaiting a
/// cost centre) and best-effort writes the project's Site tracking to Xero
/// without approving the bill. The line leaves the queue only via Allocate.
/// SetProject also applies to Disputed lines (saving the coding both sides are
/// converging on, Xero untouched until resolution), and may carry a
/// CostCenterCode to persist alongside the project.
///
/// The dispute trio (2026-08-14): Dispute parks a queued or allocated line in
/// the Disputed bucket (Note = the opening message, stored on the thread);
/// AddDisputeMessage appends to a disputed line's thread and changes nothing
/// else; ResolveDispute returns the line to Unallocated keeping whatever
/// project + cost centre were agreed — set, it lands on that project's tab
/// armed for Allocate, and the agreed Site tracking is written to Xero.
/// </summary>
public enum XeroAllocationAction
{
    Allocate = 0, Ignore = 1, Reset = 2, AllocateToBucket = 3, SetProject = 4,
    Dispute = 5, AddDisputeMessage = 6, ResolveDispute = 7
}

/// <summary>
/// Outcome of the last attempt to write an invoice's allocation back to Xero
/// (tracking + approval). None: never attempted — either the invoice was
/// already approved outside JPMS (no write-back needed) or its other lines are
/// still awaiting allocation. Failed lines keep their JPMS allocation; the
/// error is stored and the write-back can be retried.
/// </summary>
public enum XeroWriteBackStatus { None = 0, Approved = 1, Failed = 2 }

/// <summary>
/// One share of a ledger line: a cost centre, the net amount (pre-VAT and
/// positive, like the line's Net) and the project the share belongs to — a
/// split can span projects as well as cost centres. A null ProjectId falls
/// back to the command/line-level project.
/// </summary>
public sealed record XeroCostSplit(string CostCenterCode, decimal Net, string? ProjectId = null);

/// <summary>
/// Buckets for cost-of-sales lines with no identifiable project (parking charges,
/// fuel, software subscriptions...). Bucketed spend stays visible with per-bucket
/// totals so it can be drilled into and dealt with internally, while the
/// allocation queue clears down to genuine project costs.
/// </summary>
public static class XeroBuckets
{
    public const string Parking = "Parking";
    public const string Fuel = "Fuel";
    public const string Tolls = "Tolls";
    public const string Travel = "Travel";
    public const string Software = "Software subscriptions";
    public const string Ica = "ICA (Intercompany Account)";
    public const string Other = "Other";

    public static readonly IReadOnlyList<string> All = new[] { Parking, Fuel, Tolls, Travel, Software, Ica, Other };
}

/// <summary>
/// Stored ledger lines with allocation state and server-computed suggestions.
///
/// <paramref name="Status"/> narrows the read to one allocation status, which is how the
/// allocation page actually works — it is a tab per status, and only one tab is on screen at a
/// time. Left null, the query returns the entire ledger: that is what it always used to do, and it
/// is why the Xero tab was slow, since every visit shipped every line the business had ever
/// received (plus the whole cost-split table) to the browser. Pass a status.
/// </summary>
public sealed record ListXeroLedgerLines(XeroAllocationStatus? Status = null)
    : IQuery<IReadOnlyList<XeroLedgerLine>>;

/// <summary>
/// How many lines sit in each allocation status. The allocation page's tab bar shows a count for
/// every status while only ever holding one status' lines, so the counts come from a GROUP BY on
/// the server rather than from counting a list the browser had to download first.
/// </summary>
public sealed record GetXeroLedgerCounts : IQuery<XeroLedgerCounts>;

/// <summary>One count per allocation status, for the allocation page's tab bar.</summary>
public sealed record XeroLedgerCounts(int Unallocated, int Allocated, int Bucketed, int Ignored, int Disputed = 0)
{
    public int For(XeroAllocationStatus status) => status switch
    {
        XeroAllocationStatus.Unallocated => Unallocated,
        XeroAllocationStatus.Allocated   => Allocated,
        XeroAllocationStatus.Bucketed    => Bucketed,
        XeroAllocationStatus.Ignored     => Ignored,
        XeroAllocationStatus.Disputed    => Disputed,
        _ => 0
    };

    public static readonly XeroLedgerCounts Empty = new(0, 0, 0, 0, 0);
}

/// <summary>
/// One message in a disputed line's discussion — the director and the accountant
/// talking a contested cost through on the allocation page. Oldest first when
/// carried on <see cref="XeroLedgerLine.DisputeMessages"/>. The thread survives
/// resolution, so re-disputing a line continues the same conversation.
/// </summary>
public sealed record XeroDisputeMessage(string Author, string Body, DateTimeOffset SentAtUtc);

/// <summary>
/// The allocated ledger lines coded to one project, newest first. Serves the labour tab's
/// "mark invoice lines as covered" panel, which needs a hundred rows for a single project and used
/// to fetch the entire company ledger to find them.
/// </summary>
public sealed record ListXeroLedgerLinesForProject(string ProjectId, int Take = 100)
    : IQuery<IReadOnlyList<XeroLedgerLine>>;

/// <summary>
/// One stored Xero purchase-invoice line. Amounts are net (pre-VAT, normalised
/// for VAT-inclusive invoices) and positive; <see cref="Type"/> distinguishes
/// bills (ACCPAY) from supplier credit notes (ACCPAYCREDIT), which subtract in
/// any spend view. Suggested* fields are the server's best guess from the
/// line's Xero Sites / Cost Code tracking — never applied automatically.
/// </summary>
public sealed record XeroLedgerLine(
    string XeroLedgerLineId,
    string XeroInvoiceId,
    string Type,
    string? InvoiceNumber,
    string? Reference,
    string? ContactName,
    DateTime? Date,
    string InvoiceStatus,
    string? Description,
    decimal Net,
    decimal Tax,
    string? AccountCode,
    string? AccountName,
    string? XeroSite,
    string? XeroCostCode,
    XeroAllocationStatus AllocationStatus,
    string? ProjectId,
    string? CostCenterCode,
    string? Bucket,
    string? AllocatedBy,
    DateTimeOffset? AllocatedAtUtc,
    string? Note,
    string? SuggestedProjectId,
    string? SuggestedCostCenterCode,
    string? SuggestedBucket,
    DateTimeOffset FirstSeenAtUtc,
    DateTimeOffset LastSyncedAtUtc,
    // Split for allocated lines. Null/empty = the whole line sits on this line's
    // ProjectId + CostCenterCode; entries = the line's net is shared across those
    // projects/centres (CostCenterCode is then null, ProjectId holds the common
    // project or null when the split spans projects, and the split nets sum to Net).
    IReadOnlyList<XeroCostSplit>? Splits = null,
    XeroWriteBackStatus WriteBackStatus = XeroWriteBackStatus.None,
    string? WriteBackError = null,
    DateTimeOffset? WriteBackAtUtc = null,
    // Whether Xero holds attachments for this line's invoice (the supplier's
    // document, published by Dext) — arms the invoice viewer on the allocation
    // page. Refreshed on every sync like the other Xero facts.
    bool HasAttachments = false,
    // The dispute discussion, oldest first — populated only on Disputed lines
    // (the only place the thread renders); null elsewhere.
    IReadOnlyList<XeroDisputeMessage>? DisputeMessages = null,
    // Labour recognition (Labour scope §6): set on UNALLOCATED lines whose supplier is one of
    // the labour-only workers — the bill is settlement of approved timesheets, so it belongs to
    // the allocation page's Labour section and the settlement machinery, not the ordinary queue.
    // Matched by supplier name against the worker registry (the worker's own name, or the linked
    // subcontractor company's) server-side, next to the tracking suggester — never guessed from
    // account codes. MatchedSubcontractorId is null when the worker has no linked subcontractor
    // company (covering needs one — the settlement schedule reconciles by subcontractor).
    string? MatchedWorkerId = null,
    string? MatchedWorkerName = null,
    string? MatchedSubcontractorId = null,
    // Whether a XeroLineTimesheetCover row claims this line (scope §6): the approved timesheet
    // is the actual, this line is settlement of it, and the covered value is excluded from the
    // cost-of-sales aggregations. CoveredPeriodStart is the cover's month.
    bool CoveredByTimesheets = false,
    DateTimeOffset? CoveredPeriodStart = null,
    // Work Order bill recognition (2026-09-08): set on UNALLOCATED lines whose bill matched an
    // open work order — the bill belongs on the allocation page's Work Order bills tab, coded
    // from the order. Computed on every unallocated read beside labour recognition, so Sync and
    // Re-check both re-run it. WorkOrderExceptionReason is the other outcome: a candidate order
    // existed but the rule refused (over value, several fit, labour registry), shown on the
    // queue row so the FD sees why it stayed. Both null when no order comes into it.
    WorkOrderBillMatch? WorkOrderMatch = null,
    string? WorkOrderExceptionReason = null,
    // Set on lines approved as a Work Order bill and still standing: who approved, against
    // which order, by which rule. Null on lines allocated any other way.
    WorkOrderBillApprovalStamp? WorkOrderApproval = null);

/// <summary>
/// The attachments Xero holds for one purchase invoice or credit note — the
/// supplier's actual document(s), listed live from Xero (nothing stored in
/// JPMS). <paramref name="IsCreditNote"/> picks Xero's CreditNotes collection
/// (line Type ACCPAYCREDIT) over Invoices.
/// </summary>
public sealed record ListXeroInvoiceAttachments(string XeroInvoiceId, bool IsCreditNote = false)
    : IQuery<IReadOnlyList<XeroInvoiceAttachment>>;

/// <summary>One attachment as Xero holds it; the bytes are streamed on demand by file name.</summary>
public sealed record XeroInvoiceAttachment(
    string AttachmentId,
    string FileName,
    string MimeType,
    long ContentLength);

/// <summary>
/// Pulls the latest purchase invoices + credit notes from Xero (bypassing the
/// read cache) and upserts them into the stored ledger. Allocations survive.
/// </summary>
public sealed record SyncXeroLedger : ICommand<XeroLedgerSyncResult>;

public sealed record XeroLedgerSyncResult(
    bool IsConfigured,
    string? Error,
    int NewLines,
    int UpdatedLines,
    int RemovedLines,
    int TotalLines,
    int UnallocatedLines);

/// <summary>
/// Allocates every unallocated line whose suggestions resolved BOTH a project
/// and a cost centre (recomputed server-side at execution time, so what gets
/// applied is exactly what the queue shows as pre-filled). Allocations are
/// noted as auto-matched so they can be found and reviewed later. Returns how
/// many lines were allocated.
/// </summary>
public sealed record AllocateSuggestedXeroLines(string? AllocatedBy = null) : ICommand<int>;

/// <summary>
/// Applies one allocation action to a batch of ledger lines. Allocate requires
/// either ProjectId + CostCenterCode (whole line to one project + centre) or
/// Splits — two or more shares, each with its own project and cost centre,
/// whose nets must sum exactly to the line's net (splits therefore apply to a
/// single line, never a batch). A split entry without a ProjectId falls back
/// to the command's ProjectId. AllocateToBucket requires a Bucket from
/// <see cref="XeroBuckets.All"/>; Ignore takes an optional Note (reason);
/// Reset returns lines to Unallocated. SetProject applies to Unallocated and
/// Disputed lines: the project (and, when supplied, CostCenterCode) is saved
/// without leaving the current status; on queued lines the project's Xero Site
/// tracking is written without approving the bill (disputed lines wait for
/// resolution); a null ProjectId unsets — the saved coding is cleared and the
/// Site tracking removed from the bill. Dispute (Note = optional opening
/// message) parks queued or allocated lines in the Disputed bucket;
/// AddDisputeMessage (Note required) appends to a disputed line's thread;
/// ResolveDispute returns disputed lines to Unallocated keeping their agreed
/// coding, writing the agreed Site tracking to Xero. AllocatedBy is stamped
/// server-side from the signed-in user — any client-supplied value is ignored.
/// </summary>
public sealed record SetXeroAllocation(
    IReadOnlyList<string> XeroLedgerLineIds,
    XeroAllocationAction Action,
    string? ProjectId = null,
    string? CostCenterCode = null,
    string? Bucket = null,
    string? Note = null,
    string? AllocatedBy = null,
    IReadOnlyList<XeroCostSplit>? Splits = null) : ICommand<int>;

/// <summary>
/// Re-attempts the Xero write-back (tracking + approval) for one invoice whose
/// previous attempt failed — e.g. an unmapped Sites option or a Xero outage.
/// Succeeds silently when the invoice has since been approved in Xero.
/// </summary>
public sealed record RetryXeroWriteBack(string XeroInvoiceId) : ICommand<XeroWriteBackOutcome>;

public sealed record XeroWriteBackOutcome(bool Succeeded, string? Error);

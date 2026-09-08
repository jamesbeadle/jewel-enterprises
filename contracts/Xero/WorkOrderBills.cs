using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Xero;

// Work Order bills (2026-09-08, the accountant's ask): a supplier bill from a supplier with an
// open work order was decided when the order was approved — project, cost code(s), the lot. It
// must not wait in Unallocated for someone to re-decide it. The unallocated read matches each
// bill to its order (WorkOrderBillMatch rides the bill's lines), the allocation page shows the
// bill as ONE card pre-filled from the order, and Approve allocates every line, links the order,
// writes the tracking to Xero and approves the bill there in one go. Nobody codes by hand.

/// <summary>Which rule matched a bill to its order — the audit reads it back.</summary>
public enum WorkOrderMatchRule { ByReference = 0, BySupplier = 1 }

/// <summary>
/// The open work order a queued bill pays, as the read found it. Rides every line of the bill.
/// ProposedSplits are THIS line's shares across the order's cost codes, pro rata to the order's
/// lines (a one-code order gives a single share) — the starting point the card lets the user
/// edit before approving. OrderValue and InvoicedToDate are the order's figures before this
/// bill; the card derives "remaining after this bill" from the bill's own net.
/// </summary>
public sealed record WorkOrderBillMatch(
    string WorkOrderId,
    string WorkOrderReference,
    string WorkOrderTitle,
    string ProjectId,
    WorkOrderMatchRule Rule,
    string Detail,
    decimal OrderValue,
    decimal InvoicedToDate,
    IReadOnlyList<XeroCostSplit> ProposedSplits);

/// <summary>Who approved a Work Order bill against which order, shown on its allocated lines.</summary>
public sealed record WorkOrderBillApprovalStamp(
    string WorkOrderId,
    string WorkOrderReference,
    WorkOrderMatchRule Rule,
    string ApprovedBy,
    DateTimeOffset ApprovedAtUtc);

/// <summary>One line's final coding as approved: shares across the order's cost codes, summing to the line's net.</summary>
public sealed record WorkOrderBillLineCoding(string XeroLedgerLineId, IReadOnlyList<XeroCostSplit> Splits);

/// <summary>
/// The one action on a Work Order bill: allocates every line of the bill to the order's project
/// and cost code(s), links each line to the order for its full net, records who approved it and
/// which rule matched, then confirms the tracking to Xero and approves the bill there (the
/// existing draft write-back). The server re-runs the match and refuses an order the rule would
/// not give the bill. ApprovedBy is stamped server-side from the signed-in user.
/// </summary>
public sealed record ApproveWorkOrderBill(
    string XeroInvoiceId,
    string WorkOrderId,
    IReadOnlyList<WorkOrderBillLineCoding> Lines,
    string? ApprovedBy = null) : ICommand<WorkOrderBillApprovalOutcome>;

/// <summary>The allocation is saved whatever Xero said; XeroError carries Xero's refusal when there was one.</summary>
public sealed record WorkOrderBillApprovalOutcome(int LinesAllocated, bool ApprovedInXero, string? XeroError);

/// <summary>
/// Reverses a Work Order bill approval in one save: every line back to Unallocated, its split
/// rows, work-order links and package cost slices removed, the approval marked undone. Xero's
/// Sites and Cost Code tracking is then cleared off the bill's lines — but Xero cannot un-approve
/// a bill, so an approved bill stays approved (awaiting payment) there; the outcome says so.
/// UndoneBy is stamped server-side from the signed-in user.
/// </summary>
public sealed record UndoWorkOrderBillApproval(string XeroInvoiceId, string? UndoneBy = null)
    : ICommand<WorkOrderBillUndoOutcome>;

public sealed record WorkOrderBillUndoOutcome(
    int LinesReturned,
    bool TrackingCleared,
    string? XeroError,
    string XeroStatus);

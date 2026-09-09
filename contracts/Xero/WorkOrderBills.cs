using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Xero;

// Work Order bills (2026-09-08, the accountant's ask): a supplier bill from a supplier with an
// open work order was decided when the order was approved — project, cost code(s), the lot. It
// must not wait in Unallocated for someone to re-decide it. The unallocated read matches each
// bill to its order (WorkOrderBillMatch rides the bill's lines), the allocation page shows the
// bill as ONE card pre-filled from the order, and Approve allocates every line, links the order,
// writes the tracking to Xero and approves the bill there in one go. Nobody codes by hand.
//
// Since 2026-09-09 a bill may pay SEVERAL of the supplier's open orders (the accountant's ask:
// one bill, £1,748 to WO-0055 and £1,344 to WO-0056). A line's coding is a list of shares, each
// on one order and one of that order's cost codes; the read proposes the per-line split when
// each line names its own order, and the card lets any line be split across the supplier's
// open orders by hand. Approve is still one press for the whole bill.

/// <summary>Which rule matched a bill to its order(s) — the audit reads it back.</summary>
public enum WorkOrderMatchRule { ByReference = 0, BySupplier = 1, ByLineReference = 2 }

/// <summary>One share of one bill line: this much of its net, on this order, on this cost code.</summary>
public sealed record WorkOrderBillShare(string WorkOrderId, string CostCenterCode, decimal Net);

/// <summary>
/// An open order of the bill's supplier the card may put a share on — its figures before this
/// bill and the cost codes its lines carry (a share on it sits on one of those).
/// </summary>
public sealed record WorkOrderBillOrderOption(
    string WorkOrderId,
    string Reference,
    string Title,
    string ProjectId,
    string ProjectName,
    decimal OrderValue,
    decimal InvoicedToDate,
    IReadOnlyList<string> CostCodes)
{
    public decimal Remaining => OrderValue - InvoicedToDate;
}

/// <summary>
/// The open work order a queued line pays, as the read found it. Rides every line of the bill —
/// the order can differ line by line when each line names its own. ProposedShares are THIS
/// line's shares, pro rata to the order's lines (a one-code order gives a single share) — the
/// starting point the card lets the user edit before approving. SupplierOrders are every open
/// order of the bill's supplier, the matched one included, so the card can split across them.
/// </summary>
public sealed record WorkOrderBillMatch(
    string WorkOrderId,
    string WorkOrderReference,
    string WorkOrderTitle,
    string ProjectId,
    WorkOrderMatchRule Rule,
    string Detail,
    IReadOnlyList<WorkOrderBillShare> ProposedShares,
    IReadOnlyList<WorkOrderBillOrderOption> SupplierOrders);

/// <summary>One order a Work Order bill was approved against, and how much of the bill it took.</summary>
public sealed record WorkOrderBillApprovedOrder(string WorkOrderId, string WorkOrderReference, decimal Net);

/// <summary>Who approved a Work Order bill against which order(s), shown on its allocated lines.</summary>
public sealed record WorkOrderBillApprovalStamp(
    IReadOnlyList<WorkOrderBillApprovedOrder> Orders,
    WorkOrderMatchRule Rule,
    string ApprovedBy,
    DateTimeOffset ApprovedAtUtc)
{
    public string OrdersLabel => string.Join(" + ", Orders.Select(order => order.WorkOrderReference));
}

/// <summary>One line's final coding as approved: its shares, summing to the line's net.</summary>
public sealed record WorkOrderBillLineCoding(string XeroLedgerLineId, IReadOnlyList<WorkOrderBillShare> Shares);

/// <summary>
/// The one action on a Work Order bill: allocates every line of the bill to its shares' projects
/// and cost codes, links each share to its order, records who approved it against each order
/// and which rule matched, then confirms the tracking to Xero and approves the bill there (the
/// existing draft write-back). The server re-runs the match and refuses an order that is not an
/// open order of the bill's supplier, and a share that takes its order over its value.
/// ApprovedBy is stamped server-side from the signed-in user.
/// </summary>
public sealed record ApproveWorkOrderBill(
    string XeroInvoiceId,
    IReadOnlyList<WorkOrderBillLineCoding> Lines,
    string? ApprovedBy = null) : ICommand<WorkOrderBillApprovalOutcome>;

/// <summary>The allocation is saved whatever Xero said; XeroError carries Xero's refusal when there was one.</summary>
public sealed record WorkOrderBillApprovalOutcome(
    int LinesAllocated,
    IReadOnlyList<string> WorkOrderReferences,
    bool ApprovedInXero,
    string? XeroError);

/// <summary>
/// Reverses a Work Order bill approval in one save: every line back to Unallocated, its split
/// rows, work-order links and package cost slices removed, every order's approval marked undone.
/// Xero's Sites and Cost Code tracking is then cleared off the bill's lines — but Xero cannot
/// un-approve a bill, so an approved bill stays approved (awaiting payment) there; the outcome
/// says so. UndoneBy is stamped server-side from the signed-in user.
/// </summary>
public sealed record UndoWorkOrderBillApproval(string XeroInvoiceId, string? UndoneBy = null)
    : ICommand<WorkOrderBillUndoOutcome>;

public sealed record WorkOrderBillUndoOutcome(
    int LinesReturned,
    bool TrackingCleared,
    string? XeroError,
    string XeroStatus);

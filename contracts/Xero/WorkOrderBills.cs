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
// one bill, £1,748 to WO-0055 and £1,344 to WO-0056). The split is a figure per order off the
// BILL TOTAL — never off the Xero lines, which are the supplier's own CIS labour/materials
// split and are left exactly as raised. The read proposes the figures (the whole bill on the
// matched order; per order when each line names its own), the card lets them be changed and
// checks they tie to the bill, and Approve spreads each order's slice over the bill's lines
// and each line's portion over the order's cost codes, to the penny, portal-side only.

/// <summary>Which rule matched a bill to its order(s) — the audit reads it back.</summary>
public enum WorkOrderMatchRule { ByReference = 0, BySupplier = 1, ByLineReference = 2 }

/// <summary>This much of the bill's net on this order — the figure the card takes per open order.</summary>
public sealed record WorkOrderBillOrderSlice(string WorkOrderId, decimal Net);

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
/// The open work order(s) a queued bill pays, as the read found them. Rides every line of the
/// bill, identical on each. WorkOrderId etc. name the leading order; ProposedSlices are the
/// bill's net across the orders as the read proposes it — the starting point the card lets the
/// user change before approving; SupplierOrders are every open order of the bill's supplier,
/// the matched ones included, so the card can put a figure on any of them.
/// </summary>
public sealed record WorkOrderBillMatch(
    string WorkOrderId,
    string WorkOrderReference,
    string WorkOrderTitle,
    string ProjectId,
    WorkOrderMatchRule Rule,
    string Detail,
    IReadOnlyList<WorkOrderBillOrderSlice> ProposedSlices,
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

/// <summary>
/// The one action on a Work Order bill: takes the bill's net as a figure per order (the slices
/// must add up to the bill), spreads each order's slice over the bill's lines pro rata and each
/// line's portion over the order's cost codes, allocates every line accordingly, links each
/// order for its slice, records who approved it against each order and which rule matched,
/// then stamps each Xero line's tracking WHOLE (never splitting a line) and approves the bill
/// there. The server re-runs the match and refuses an order that is not an open order of the
/// bill's supplier, and a slice that takes its order over its value. ApprovedBy is stamped
/// server-side from the signed-in user.
/// </summary>
public sealed record ApproveWorkOrderBill(
    string XeroInvoiceId,
    IReadOnlyList<WorkOrderBillOrderSlice> Slices,
    string? ApprovedBy = null) : ICommand<WorkOrderBillApprovalOutcome>;

/// <summary>The allocation is saved whatever Xero said; XeroError carries Xero's refusal when
/// there was one; TrackingNote says so when the bill was approved with no tracking written.</summary>
public sealed record WorkOrderBillApprovalOutcome(
    int LinesAllocated,
    IReadOnlyList<string> WorkOrderReferences,
    bool ApprovedInXero,
    string? XeroError,
    string? TrackingNote = null);

/// <summary>
/// The one wording for a Work Order bill whose tracking Xero cannot carry (2026-09-09, the
/// accountant's rule): the order split lives on the portal's work-order links, Xero tracking is
/// a convenience, and a supplier's line is never split to make it fit — so the bill is approved
/// with no tracking, and the card and the ledger say exactly that.
/// </summary>
public static class WorkOrderBillTracking
{
    public const string NotWrittenNote =
        "Xero tracking not written for this bill — the order split would need two tracking values on one of the supplier's lines, and a line is never split to fit. The split lives on the portal's work-order links.";

    /// <summary>Tracking can be written only when every line lands on one centre — one project and one cost code across every order the bill pays.</summary>
    public static bool CanBeWritten(IEnumerable<WorkOrderBillOrderOption> ordersPaid) =>
        ordersPaid.SelectMany(order => order.CostCodes.Select(code => (order.ProjectId, code)))
            .Distinct()
            .Count() == 1;
}

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

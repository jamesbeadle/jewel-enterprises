using Jewel.JPMS.Api.Features.Xero.Ledger.WorkOrderBills;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>
/// The Work Order bills tab's two buttons as connector actions (2026-09-09, the accountant's
/// ask): a supplier's bill that matched its open order(s) is approved against them — figures per
/// order, lines coded whole, tracking to Xero where it fits, bill approved in Xero — or that
/// approval is undone. Same commands, same handlers, same guards as the card; the connector
/// reads the card's data off list_xero_ledger_lines (workOrderBill on each Unallocated line).
/// </summary>
internal sealed partial class CommercialActions
{
    // Replica of WorkOrderBillRoles.AllowedToApprove — the FD's button, not the whole queue's.
    private static readonly RoleSet WorkOrderBillApprovers =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector);

    private static IEnumerable<AiAction> WorkOrderBillActions() => new AiAction[]
    {
        new AiAction(
            Name: "approve_work_order_bill",
            Area: "Commercial",
            Description: "Approves a supplier bill against the supplier's open work order(s) — the "
                + "Work Order bills tab's Approve. slices give the bill's net as a figure per order "
                + "(workOrderId + net; they must add up to the bill's net exactly, and an order can "
                + "take at most its remaining value). Every line of the bill is allocated to the "
                + "orders' projects and cost codes pro rata, each order is linked for its slice with "
                + "who approved and which rule matched, then WRITES TO XERO: the bill's Sites + Cost "
                + "Code tracking is stamped where every line lands on one centre and the bill is "
                + "APPROVED (DRAFT → AUTHORISED). A supplier's line is never split to fit Xero: when "
                + "the split would need two tracking values on one line, the bill is approved with "
                + "NO tracking written and the outcome says so (trackingNote) — the order split then "
                + "lives on the portal's work-order links only. Refused when any line of the bill "
                + "has already moved, when the bill is PAID/VOIDED in Xero, when an order named is "
                + "not an open order of this supplier, or when a slice takes its order over value.",
            CommandType: typeof(ApproveWorkOrderBill),
            ResultType: typeof(WorkOrderBillApprovalOutcome),
            AuthorisationType: typeof(ApproveWorkOrderBillAuthorisation),
            ValidationType: typeof(ApproveWorkOrderBillValidation),
            VisibleTo: WorkOrderBillApprovers,
            EmailStamps: new[] { nameof(ApproveWorkOrderBill.ApprovedBy) },
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Read list_xero_ledger_lines with status Unallocated first: a bill's lines carry "
                + "xeroInvoiceId and workOrderBill — the matched order, proposedSlices (the read's "
                + "split of the bill across orders; one slice when one order fits) and "
                + "supplierOpenOrders (every open order of the supplier with its remaining value and "
                + "cost codes). Start from proposedSlices; move money between the supplier's open "
                + "orders only on the user's word. In the confirm turn show the bill (supplier, "
                + "invoice number, net), each order with the figure it takes, whether Xero tracking "
                + "will be written (workOrderBill.xeroTrackingWritableForProposedSlices — say plainly "
                + "when it will NOT be), and that the bill will be approved in Xero. The approval in "
                + "Xero cannot be undone from here (undo_work_order_bill_approval reverses the portal "
                + "side and clears the tracking, but Xero keeps the bill approved). Lines NOT carrying "
                + "workOrderBill are not Work Order bills — set_xero_allocation codes those."),

        new AiAction(
            Name: "undo_work_order_bill_approval",
            Area: "Commercial",
            Description: "Reverses a Work Order bill approval — the card's Undo: every line of the "
                + "bill back to Unallocated, its splits, work-order links and package cost slices "
                + "removed, each order's approval marked undone, then WRITES TO XERO: the Sites + "
                + "Cost Code tracking is cleared off the bill's lines. Xero cannot un-approve a bill, "
                + "so an approved bill stays approved (awaiting payment) there — the outcome's "
                + "xeroStatus says where it stands. Refused when no Work Order bill approval stands "
                + "on the bill, or when the bill is paid in Xero (a paid cost stays allocated).",
            CommandType: typeof(UndoWorkOrderBillApproval),
            ResultType: typeof(WorkOrderBillUndoOutcome),
            AuthorisationType: typeof(UndoWorkOrderBillApprovalAuthorisation),
            ValidationType: typeof(UndoWorkOrderBillApprovalValidation),
            VisibleTo: WorkOrderBillApprovers,
            EmailStamps: new[] { nameof(UndoWorkOrderBillApproval.UndoneBy) },
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "xeroInvoiceId comes from list_xero_ledger_lines (status Allocated; the bill's "
                + "lines carry workOrderApproval with the orders and who approved). Confirm the bill "
                + "and the orders it comes off with the user first, and say that Xero will keep the "
                + "bill approved — only its tracking is cleared."),
    };
}

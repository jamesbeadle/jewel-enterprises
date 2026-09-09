using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The ledger line as list_xero_ledger_lines hands it to the model: trimmed to what an
/// allocation decision needs (the full record carries sync bookkeeping the model never uses),
/// plus — since 2026-09-09 — the Work Order bill facts the allocation page's Work Order bills
/// tab shows on its card, so approving a supplier's bill against its order(s) is one
/// approve_work_order_bill call from here, exactly as it is one press there.
/// </summary>
internal static partial class AiFinanceTools
{
    private static object Line(XeroLedgerLine line) => new
    {
        line.XeroLedgerLineId,
        line.XeroInvoiceId,
        line.Type,
        line.InvoiceNumber,
        line.Reference,
        line.ContactName,
        line.Date,
        line.Description,
        line.Net,
        line.AccountCode,
        line.AccountName,
        status = line.AllocationStatus.ToString(),
        line.ProjectId,
        line.CostCenterCode,
        line.Bucket,
        line.SuggestedProjectId,
        line.SuggestedCostCenterCode,
        line.SuggestedBucket,
        line.Note,
        splits = line.Splits,
        xeroStatus = line.InvoiceStatus,
        writeBackStatus = line.WriteBackStatus.ToString(),
        line.WriteBackError,
        line.WriteBackFailedAtUtc,
        workOrderBill = line.WorkOrderMatch is null ? null : WorkOrderBill(line.WorkOrderMatch),
        line.WorkOrderExceptionReason,
        workOrderApproval = line.WorkOrderApproval is null ? null : WorkOrderApproval(line.WorkOrderApproval)
    };

    /// <summary>The card's data: which order(s) matched and why, the split the read proposes,
    /// and every open order of the supplier a slice may go on — with whether Xero tracking can
    /// be written for the proposed split (it cannot when a line would need two values).</summary>
    private static object WorkOrderBill(WorkOrderBillMatch match) => new
    {
        matchedWorkOrderId = match.WorkOrderId,
        matchedWorkOrderReference = match.WorkOrderReference,
        matchedWorkOrderTitle = match.WorkOrderTitle,
        match.ProjectId,
        rule = match.Rule.ToString(),
        match.Detail,
        proposedSlices = match.ProposedSlices.Select(slice => new
        {
            slice.WorkOrderId,
            reference = match.SupplierOrders.FirstOrDefault(order => order.WorkOrderId == slice.WorkOrderId)?.Reference,
            slice.Net
        }),
        supplierOpenOrders = match.SupplierOrders.Select(order => new
        {
            order.WorkOrderId,
            order.Reference,
            order.Title,
            order.ProjectId,
            order.ProjectName,
            order.OrderValue,
            order.InvoicedToDate,
            order.Remaining,
            order.CostCodes
        }),
        xeroTrackingWritableForProposedSlices = WorkOrderBillTracking.CanBeWritten(
            match.SupplierOrders.Where(order => match.ProposedSlices.Any(slice => slice.WorkOrderId == order.WorkOrderId)))
    };

    private static object WorkOrderApproval(WorkOrderBillApprovalStamp stamp) => new
    {
        orders = stamp.Orders,
        ordersLabel = stamp.OrdersLabel,
        rule = stamp.Rule.ToString(),
        stamp.ApprovedBy,
        stamp.ApprovedAtUtc
    };
}

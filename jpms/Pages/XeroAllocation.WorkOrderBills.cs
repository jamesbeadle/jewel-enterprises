using Jewel.JPMS.Features.Xero;
using static Jewel.JPMS.Features.Xero.XeroLedgerDisplay;

namespace Jewel.JPMS.Pages;

public partial class XeroAllocation
{
    // -- Work Order bills section (2026-09-08) ------------------------------------------------
    // workOrderBillsTab is a sub-view of the Unallocated status, like labourTab: the tab the
    // bills matched to an open work order fall in instead of the queue. Recognition is the
    // server's (WorkOrderMatch rides the line, recomputed on every unallocated read); the page
    // only partitions on it and groups the lines into one card per bill. notWorkOrderBillInvoiceIds
    // is this visit's escape hatch for a wrong match — the bill rejoins the plain queue for a
    // hand allocation. slicesByInvoiceId holds each bill's editable figure per open order of the
    // supplier (2026-09-09 — off the bill total, never the Xero lines), seeded from the server's
    // proposal the first time the card is drawn.
    private bool workOrderBillsTab;
    private readonly HashSet<string> notWorkOrderBillInvoiceIds = new();
    private readonly Dictionary<string, List<WorkOrderBillSliceDraft>> slicesByInvoiceId = new();
    private string? approvingInvoiceId;
    private string? workOrderBillError;
    private string? workOrderBillErrorInvoiceId;
    private string? undoWorkOrderBillInvoiceId;

    private string? WorkOrderBillErrorFor(IReadOnlyList<XeroLedgerLine> bill) =>
        workOrderBillErrorInvoiceId == bill[0].XeroInvoiceId ? workOrderBillError : null;

    private bool IsWorkOrderBillLine(XeroLedgerLine line) =>
        line.WorkOrderMatch is not null
        && !IsLabourLine(line)
        && !notWorkOrderBillInvoiceIds.Contains(line.XeroInvoiceId);

    /// <summary>The tab's number: bills (cards), not lines.</summary>
    private int WorkOrderBillCount =>
        UnallocatedLines.Where(IsWorkOrderBillLine).Select(line => line.XeroInvoiceId).Distinct().Count();

    /// <summary>One card per bill, newest first, out of the tab's visible lines.</summary>
    private IEnumerable<IReadOnlyList<XeroLedgerLine>> WorkOrderBillCards =>
        Visible.GroupBy(line => line.XeroInvoiceId)
               .OrderByDescending(bill => bill.Max(line => line.Date))
               .Select(bill => (IReadOnlyList<XeroLedgerLine>)bill.ToList());

    /// <summary>The bill's figure per open order, as magnitudes; the read's proposal seeds it.</summary>
    private List<WorkOrderBillSliceDraft> SlicesFor(IReadOnlyList<XeroLedgerLine> bill)
    {
        if (slicesByInvoiceId.TryGetValue(bill[0].XeroInvoiceId, out var slices)) return slices;
        slices = (bill[0].WorkOrderMatch?.ProposedSlices ?? Array.Empty<WorkOrderBillOrderSlice>())
            .Select(slice => new WorkOrderBillSliceDraft { WorkOrderId = slice.WorkOrderId, Amount = Math.Abs(slice.Net) })
            .ToList();
        slicesByInvoiceId[bill[0].XeroInvoiceId] = slices;
        return slices;
    }

    private async Task ApproveWorkOrderBillAsync(IReadOnlyList<XeroLedgerLine> bill)
    {
        if (isBusy || bill.Count == 0 || bill[0].WorkOrderMatch is null) return;
        var sign = bill.Sum(SignedNet) < 0m ? -1m : 1m;
        var command = new ApproveWorkOrderBill(bill[0].XeroInvoiceId,
            SlicesFor(bill).Where(slice => slice.Amount is > 0m)
                .Select(slice => new WorkOrderBillOrderSlice(slice.WorkOrderId, sign * slice.Amount!.Value)).ToList());
        isApplying = true; approvingInvoiceId = bill[0].XeroInvoiceId; workOrderBillError = null; workOrderBillErrorInvoiceId = null; errorMessage = null;
        try
        {
            var outcome = await Ledger.ApproveWorkOrderBillAsync(command);
            slicesByInvoiceId.Remove(bill[0].XeroInvoiceId);
            var orders = string.Join(" + ", outcome.WorkOrderReferences);
            syncMessage = outcome.ApprovedInXero
                ? $"{bill[0].ContactName} {bill[0].InvoiceNumber} · {Money(bill.Sum(SignedNet))} approved against {orders} — {outcome.LinesAllocated} line(s) allocated and linked, approved in Xero"
                  + (outcome.TrackingNote is null ? "." : " with no tracking written (the order split crosses the supplier's lines; the portal's links hold it).")
                : $"{bill[0].ContactName} {bill[0].InvoiceNumber} allocated and linked to {orders}, but Xero said: {outcome.XeroError} — retry from the Allocated tab.";
        }
        catch (CommandFailedException failure) { workOrderBillError = failure.Message; workOrderBillErrorInvoiceId = bill[0].XeroInvoiceId; }
        finally { isApplying = false; approvingInvoiceId = null; }
    }

    private void MarkNotWorkOrderBill(IReadOnlyList<XeroLedgerLine> bill)
    {
        if (bill.Count == 0) return;
        notWorkOrderBillInvoiceIds.Add(bill[0].XeroInvoiceId);
        slicesByInvoiceId.Remove(bill[0].XeroInvoiceId);
    }

    // -- Undo, from the Allocated tab -------------------------------------------------------
    // A line approved as a Work Order bill is undone as a BILL — the whole approval reverses in
    // one go — so the row's Undo opens the confirm instead of resetting the one line.

    private void OpenUndoWorkOrderBill(XeroLedgerLine line) => undoWorkOrderBillInvoiceId = line.XeroInvoiceId;

    private void CloseUndoWorkOrderBill() => undoWorkOrderBillInvoiceId = null;

    private async Task ConfirmUndoWorkOrderBillAsync()
    {
        if (isBusy || undoWorkOrderBillInvoiceId is not { } invoiceId) return;
        isApplying = true; errorMessage = null;
        try
        {
            var outcome = await Ledger.UndoWorkOrderBillApprovalAsync(invoiceId);
            syncMessage = $"Approval undone — {outcome.LinesReturned} line(s) back in the Work Order bills tab, links removed"
                + (outcome.TrackingCleared ? $", Xero tracking cleared. The bill is {outcome.XeroStatus} in Xero" : $". Xero said: {outcome.XeroError}")
                + (outcome.XeroStatus.Equals("AUTHORISED", StringComparison.OrdinalIgnoreCase) ? " — Xero cannot un-approve a bill, so it stays awaiting payment there." : ".");
            undoWorkOrderBillInvoiceId = null;
        }
        catch (CommandFailedException failure) { errorMessage = failure.Message; }
        finally { isApplying = false; }
    }
}

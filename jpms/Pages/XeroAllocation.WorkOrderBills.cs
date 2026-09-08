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
    // hand allocation. sharesByLineId holds the editable coding of a multi-code order's bill,
    // seeded from the server's pro rata proposal the first time the card is drawn.
    private bool workOrderBillsTab;
    private readonly HashSet<string> notWorkOrderBillInvoiceIds = new();
    private readonly Dictionary<string, List<XeroSplitDraft>> sharesByLineId = new();
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

    private List<XeroSplitDraft> SharesFor(XeroLedgerLine line)
    {
        if (sharesByLineId.TryGetValue(line.XeroLedgerLineId, out var shares)) return shares;
        shares = (line.WorkOrderMatch?.ProposedSplits ?? Array.Empty<XeroCostSplit>())
            .Select(split => new XeroSplitDraft { ProjectId = split.ProjectId ?? "", Code = split.CostCenterCode, Amount = split.Net })
            .ToList();
        sharesByLineId[line.XeroLedgerLineId] = shares;
        return shares;
    }

    private async Task ApproveWorkOrderBillAsync(IReadOnlyList<XeroLedgerLine> bill)
    {
        if (isBusy || bill.Count == 0 || bill[0].WorkOrderMatch is not { } match) return;
        var command = new ApproveWorkOrderBill(bill[0].XeroInvoiceId, match.WorkOrderId,
            bill.Select(line => new WorkOrderBillLineCoding(line.XeroLedgerLineId,
                SharesFor(line).Select(share => new XeroCostSplit(share.Code, share.Amount ?? 0m, match.ProjectId)).ToList())).ToList());
        isApplying = true; approvingInvoiceId = bill[0].XeroInvoiceId; workOrderBillError = null; workOrderBillErrorInvoiceId = null; errorMessage = null;
        try
        {
            var outcome = await Ledger.ApproveWorkOrderBillAsync(command);
            foreach (var line in bill) sharesByLineId.Remove(line.XeroLedgerLineId);
            syncMessage = outcome.ApprovedInXero
                ? $"{bill[0].ContactName} {bill[0].InvoiceNumber} · {Money(bill.Sum(SignedNet))} approved against {match.WorkOrderReference} — {outcome.LinesAllocated} line(s) allocated and linked, approved in Xero."
                : $"{bill[0].ContactName} {bill[0].InvoiceNumber} allocated and linked to {match.WorkOrderReference}, but Xero said: {outcome.XeroError} — retry from the Allocated tab.";
        }
        catch (CommandFailedException failure) { workOrderBillError = failure.Message; workOrderBillErrorInvoiceId = bill[0].XeroInvoiceId; }
        finally { isApplying = false; approvingInvoiceId = null; }
    }

    private void MarkNotWorkOrderBill(IReadOnlyList<XeroLedgerLine> bill)
    {
        if (bill.Count == 0) return;
        notWorkOrderBillInvoiceIds.Add(bill[0].XeroInvoiceId);
        foreach (var line in bill) sharesByLineId.Remove(line.XeroLedgerLineId);
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

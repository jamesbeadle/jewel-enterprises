using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Xero;
using static Jewel.JPMS.Api.Features.Labour.Commands.XeroCodingWording;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed partial class RunXeroCodingHandler
{
    private async Task<IReadOnlyList<XeroCodingRunResult>> RecodeExistingBillAsync(
        CodingParty party, string billId, bool wasCovered, List<CodedLine> codedLines, CancellationToken cancellationToken)
    {
        // The bill's stored lines, TRACKED — a recode replaces them with the fresh lines (and moves
        // the cover) in the same SaveChanges as the run record, so the reconciliation the cover
        // holds is never half-done: verdict, covered total and difference read the same before
        // and after (item B).
        var storedLines = await StoredLinesAsync(billId, cancellationToken);
        var allocated = storedLines.Count(line => line.AllocationStatus == (int)XeroAllocationStatus.Allocated);
        if (allocated > 0)
            return party.Each(worker => worker.Skip(
                $"Bill {LedgerBillLabel(storedLines[0])} has {allocated} line(s) already allocated by hand on the Cost "
                + "allocation page — recoding would orphan that allocation. Unallocate them (or leave the bill as coded) and re-run.", billId));

        var (target, refusals) = await ReadBillNowAsync(party, billId, storedLines, cancellationToken);
        if (target is null) return refusals!;
        var bill = target.Bill;
        // A re-issue's own stored lines (if the ledger has caught up) go first: they are the
        // template for the fresh rows, the predecessor's only the fallback.
        if (target.PredecessorBillId is not null)
            storedLines = (await StoredLinesAsync(bill.InvoiceId, cancellationToken)).Concat(storedLines).ToList();

        if (party.DryRun)
            return party.Each(worker => worker.Outcome(XeroCodingOutcome.WouldRecodeBill,
                WouldRecodeSentence(party, worker, bill, codedLines, wasCovered), bill.InvoiceId));

        var xeroLines = codedLines.Select(coded => coded.Line).ToList();
        var recode = await xero.RecodeBillAsync(new XeroBillCodingRequest(bill.InvoiceId, xeroLines), cancellationToken);
        if (!recode.Succeeded)
            return party.Each(worker => worker.Failed(recode.Error ?? "Xero refused the recode.", bill.InvoiceId));

        var coveredByWorker = await RepointCoverAndLedgerAsync(party, bill, storedLines, codedLines, recode, cancellationToken);
        return party.Each(worker => worker.Outcome(XeroCodingOutcome.BillRecoded,
            RecodedSentence(party, worker, bill, recode, wasCovered, coveredByWorker[worker.WorkerId]), bill.InvoiceId));
    }

    private Task<List<XeroLedgerLineEntity>> StoredLinesAsync(string billId, CancellationToken cancellationToken) =>
        context.XeroLedgerLines.Where(line => line.XeroInvoiceId == billId).ToListAsync(cancellationToken);
}

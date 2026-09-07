using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Contracts.Xero;
using static Jewel.JPMS.Api.Features.Labour.Commands.XeroCodingWording;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed partial class RunXeroCodingHandler
{
    private async Task<XeroCodingRunResult> RecodeExistingBillAsync(
        WorkerRun run, string billId, bool wasCovered, List<XeroScheduleLine> xeroLines, string preface, CancellationToken cancellationToken)
    {
        var schedule = run.Schedule;

        // The bill's stored lines, TRACKED — a recode replaces them with the fresh lines (and moves
        // the cover) in the same SaveChanges as the run record, so the reconciliation the cover
        // holds is never half-done: verdict, covered total and difference read the same before
        // and after (item B).
        var storedLines = await context.XeroLedgerLines
            .Where(line => line.XeroInvoiceId == billId)
            .ToListAsync(cancellationToken);
        var allocated = storedLines.Count(line => line.AllocationStatus == (int)XeroAllocationStatus.Allocated);
        if (allocated > 0)
            return run.Skip($"Bill {LedgerBillLabel(storedLines[0])} has {allocated} line(s) already allocated by hand on the Cost "
                + "allocation page — recoding would orphan that allocation. Unallocate them (or leave the bill as coded) and re-run.", billId);

        var (bill, unreadable) = await ReadBillNowAsync(run, billId, preface, cancellationToken);
        if (unreadable is not null) return unreadable;

        var totals = TotalsSentence(bill!, schedule.GrossTotal);
        if (run.DryRun)
            return run.Outcome(XeroCodingOutcome.WouldRecodeBill,
                preface + $"Would recode {(wasCovered ? "the covered bill" : "the bill recognised by contact + period")} to {xeroLines.Count} line(s), "
                + "keeping its status, total and VAT" + (wasCovered ? " and moving the cover onto the new lines. " : " and marking it as settlement of the month. ")
                + totals + " " + LinesSummary(xeroLines), billId);

        var recode = await xero.RecodeBillAsync(new XeroBillCodingRequest(billId, xeroLines), cancellationToken);
        if (!recode.Succeeded)
            return run.Failed(preface + (recode.Error ?? "Xero refused the recode."), billId);

        var moved = await RepointCoverAndLedgerAsync(run, billId, storedLines, bill!, recode, cancellationToken);
        return run.Outcome(XeroCodingOutcome.BillRecoded,
            preface + $"Recoded bill {BillLabel(bill!)} to {recode.Lines.Count} line(s); left {recode.Status} in Xero. {TotalsProof(bill!, recode)}. "
            + (wasCovered ? $"Cover moved onto {moved} line(s). " : $"Marked as settlement of {run.MonthStart:MMM yyyy} ({moved} line(s)). ")
            + totals, billId);
    }

    /// <summary>Decide on what Xero holds NOW, not on last night's sync — or say why that could
    /// not be read, or why the bill as it stands cannot be recoded.</summary>
    private async Task<(XeroBillSummary? Bill, XeroCodingRunResult? Refusal)> ReadBillNowAsync(
        WorkerRun run, string billId, string preface, CancellationToken cancellationToken)
    {
        XeroBillSummary? bill;
        try { bill = await xero.GetBillAsync(billId, cancellationToken); }
        catch (XeroCallFailedException failure)
        {
            return (null, run.Failed(preface + $"Couldn't read bill {billId} from Xero: {failure.Message}", billId));
        }
        if (bill is null)
            return (null, run.Skip(preface + $"Bill {billId} is marked as {run.Schedule.WorkerName}'s for {run.MonthStart:MMM yyyy} but Xero no longer has it "
                + "— sync from Xero (which clears its cover) and re-run.", billId));
        if (!bill.IsRecodable)
            return (null, run.Skip(preface + $"Bill {BillLabel(bill)} for {run.MonthStart:MMM yyyy} can't be recoded — {bill.NotRecodableReason}. "
                + "Nothing was written and no second bill was staged; the site and cost-code split stays portal-side "
                + "(the approved timesheets carry it).", billId));
        return (bill, null);
    }

    private static string TotalsSentence(XeroBillSummary bill, decimal scheduleTotal)
    {
        var difference = decimal.Round(bill.SubTotal - scheduleTotal, 2);
        return $"Bill {BillLabel(bill)}: {bill.Status}, net £{bill.SubTotal:N2}, VAT £{bill.TotalTax:N2} "
            + $"({bill.TaxType ?? "account default"}, {bill.LineAmountTypes}), total £{bill.Total:N2}. Schedule £{scheduleTotal:N2}"
            + (difference == 0m ? " — matches." : $" — differs by £{difference:N2}: the bill's money is split in the schedule's proportions; post a settlement variance for the difference.");
    }

    private static string TotalsProof(XeroBillSummary before, XeroBillRecodeResult recode) =>
        recode.Total == before.Total && recode.TotalTax == before.TotalTax
            ? $"Total £{recode.Total:N2} and VAT £{recode.TotalTax:N2} unchanged"
            : $"WARNING — total moved from £{before.Total:N2} to £{recode.Total:N2} (VAT £{before.TotalTax:N2} → £{recode.TotalTax:N2}): check the bill in Xero";
}

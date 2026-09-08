using Jewel.JPMS.Api.Features.Xero;
using static Jewel.JPMS.Api.Features.Labour.Commands.XeroCodingWording;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

/// <summary>What a recode says to each worker on the bill — a sole trader's own bill in the
/// words the run has always used, a company bill naming who it is shared with.</summary>
public sealed partial class RunXeroCodingHandler
{
    private static string WouldRecodeSentence(CodingParty party, WorkerRun worker, XeroBillSummary bill, List<CodedLine> codedLines, bool wasCovered)
    {
        var which = wasCovered ? "covered bill" : "bill recognised by contact + period";
        if (!party.IsCompanyBill)
            return $"Would recode the {which} to {codedLines.Count} line(s), keeping its status, total and VAT"
                + (wasCovered ? " and moving the cover onto the new lines. " : " and marking it as settlement of the month. ")
                + TotalsSentence(party, bill) + " " + WorkersLinesSummary(party, worker, codedLines);
        var others = party.Workers.Where(other => other.WorkerId != worker.WorkerId).ToList();
        var own = codedLines.Count(coded => coded.Worker.WorkerId == worker.WorkerId);
        return $"Would recode {party.CounterpartyName}'s {which} — shared with {party.NamesOf(others)} — to {codedLines.Count} line(s) "
            + $"({own} of them {worker.WorkerName}'s), keeping its status, total and VAT and marking cover per worker. "
            + TotalsSentence(party, bill) + " " + WorkersLinesSummary(party, worker, codedLines);
    }

    private static string RecodedSentence(
        CodingParty party, WorkerRun worker, XeroBillSummary bill, XeroBillRecodeResult recode, bool wasCovered, int coveredForWorker)
    {
        if (!party.IsCompanyBill)
            return $"Recoded bill {BillLabel(bill)} to {recode.Lines.Count} line(s); left {recode.Status} in Xero. {TotalsProof(bill, recode)}. "
                + (wasCovered ? $"Cover moved onto {coveredForWorker} line(s). " : $"Marked as settlement of {party.MonthStart:MMM yyyy} ({coveredForWorker} line(s)). ")
                + TotalsSentence(party, bill);
        return $"Recoded {party.CounterpartyName}'s bill {BillLabel(bill)} to {recode.Lines.Count} line(s) for {party.Workers.Count} workers "
            + $"({party.WorkerNames}); left {recode.Status} in Xero. {TotalsProof(bill, recode)}. "
            + $"Cover marked per worker — {coveredForWorker} line(s) are {worker.WorkerName}'s. " + TotalsSentence(party, bill);
    }

    private static string TotalsSentence(CodingParty party, XeroBillSummary bill)
    {
        var difference = decimal.Round(bill.SubTotal - party.ScheduleTotal, 2);
        var schedule = party.IsCompanyBill ? $"Combined schedule ({party.Workers.Count} workers)" : "Schedule";
        return $"Bill {BillLabel(bill)}: {bill.Status}, net £{bill.SubTotal:N2}, VAT £{bill.TotalTax:N2} "
            + $"({bill.TaxType ?? "account default"}, {bill.LineAmountTypes}), total £{bill.Total:N2}. {schedule} £{party.ScheduleTotal:N2}"
            + (difference == 0m ? " — matches." : $" — differs by £{difference:N2}: the bill's money is split in the schedule's proportions; post a settlement variance for the difference.");
    }

    private static string TotalsProof(XeroBillSummary before, XeroBillRecodeResult recode) =>
        recode.Total == before.Total && recode.TotalTax == before.TotalTax
            ? $"Total £{recode.Total:N2} and VAT £{recode.TotalTax:N2} unchanged"
            : $"WARNING — total moved from £{before.Total:N2} to £{recode.Total:N2} (VAT £{before.TotalTax:N2} → £{recode.TotalTax:N2}): check the bill in Xero";

    /// <summary>A sole trader's outcome lists the bill's lines; a worker on a company bill sees
    /// their own.</summary>
    private static string WorkersLinesSummary(CodingParty party, WorkerRun worker, List<CodedLine> codedLines)
    {
        if (!party.IsCompanyBill) return XeroCodingWording.LinesSummary(codedLines.Select(coded => coded.Line).ToList());
        var own = codedLines.Where(coded => coded.Worker.WorkerId == worker.WorkerId).Select(coded => coded.Line).ToList();
        return XeroCodingWording.LinesSummary(own, $"{worker.WorkerName}'s lines");
    }
}

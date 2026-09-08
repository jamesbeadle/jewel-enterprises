using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero;
using static Jewel.JPMS.Api.Features.Labour.Commands.XeroCodingWording;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed partial class RunXeroCodingHandler
{
    /// <summary>The bill a recode is decided on, as Xero holds it now — and, when the bill the
    /// ledger or a cover named has been voided and keyed again, the predecessor whose ledger
    /// lines and cover move onto the re-issue (K, 2026-09-08).</summary>
    private sealed record RecodeTarget(XeroBillSummary Bill, string? PredecessorBillId);

    /// <summary>Decide on what Xero holds NOW, not on last night's sync — or say why that could
    /// not be read, or why the bill as it stands cannot be recoded. No target means refusals.</summary>
    private async Task<(RecodeTarget? Target, IReadOnlyList<XeroCodingRunResult>? Refusals)> ReadBillNowAsync(
        CodingParty party, string billId, List<XeroLedgerLineEntity> storedLines, CancellationToken cancellationToken)
    {
        XeroBillSummary? bill;
        try { bill = await xero.GetBillAsync(billId, cancellationToken); }
        catch (XeroCallFailedException failure)
        {
            return (null, party.Each(worker => worker.Failed($"Couldn't read bill {billId} from Xero: {failure.Message}", billId)));
        }
        var live = bill is null || IsGone(bill.Status) ? null : bill;
        string? predecessorBillId = null;
        if (live is null)
        {
            var number = bill?.InvoiceNumber ?? storedLines.FirstOrDefault()?.InvoiceNumber;
            var (reissue, refusal) = await FindReissueAsync(party, number, billId, cancellationToken);
            if (refusal is not null) return (null, party.Each(worker => worker.Skip(refusal, billId)));
            if (reissue is null) return (null, party.Each(worker => worker.Skip(GoneWithoutReissue(party, bill, billId), billId)));
            foreach (var worker in party.Workers) worker.Preface += ReissuedPreface(bill, billId, reissue);
            live = reissue;
            predecessorBillId = billId;
        }
        if (!live.IsRecodable) return (null, party.Each(worker => worker.Skip(CannotRecode(party, live), live.InvoiceId)));
        return (new RecodeTarget(live, predecessorBillId), null);
    }

    /// <summary>
    /// The live re-issue of a bill that is voided, deleted or gone: the DRAFT / SUBMITTED /
    /// AUTHORISED bill under the same number, from the party's contact, for the same month —
    /// what the accountant keys after voiding one. One is the bill; several is a skip; none
    /// leaves the month where it was. No number, nothing to look for.
    /// </summary>
    private async Task<(XeroBillSummary? Reissue, string? Refusal)> FindReissueAsync(
        CodingParty party, string? invoiceNumber, string predecessorBillId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber)) return (null, null);
        IReadOnlyList<XeroBillSummary> sameNumber;
        try { sameNumber = await xero.FindBillsByNumberAsync(invoiceNumber, cancellationToken); }
        catch (XeroCallFailedException failure)
        {
            return (null, $"Bill \"{invoiceNumber}\" ({predecessorBillId}) is gone from Xero and Xero couldn't be asked for its re-issue: {failure.Message}");
        }
        var live = sameNumber
            .Where(bill => bill.InvoiceId != predecessorBillId && !IsGone(bill.Status)
                && bill.ContactName is not null && IsOneOf(bill.ContactName, party.ContactNames)
                && IsForMonth(bill.InvoiceNumber, bill.Reference, bill.Date, party.Month))
            .ToList();
        if (live.Count > 1)
            return (null, $"{live.Count} live bills from {party.Owner} carry the number \"{invoiceNumber}\": "
                + string.Join(", ", live.Select(bill => $"{bill.InvoiceId} ({bill.Status}, £{bill.Total:N2})"))
                + ". Mark the right one as settlement on the Cost allocation page's Labour tab, then re-run.");
        return (live.SingleOrDefault(), null);
    }

    private static string ReissuedPreface(XeroBillSummary? gone, string goneBillId, XeroBillSummary reissue) =>
        (gone is null ? $"Bill {goneBillId} is no longer in Xero" : $"Bill {BillLabel(gone)} ({goneBillId}) is {gone.Status.ToLowerInvariant()}")
        + $"; its live re-issue {BillLabel(reissue)} ({reissue.InvoiceId}, {reissue.Status}) is the bill. ";

    private static string CannotRecode(CodingParty party, XeroBillSummary bill) =>
        $"Bill {BillLabel(bill)} for {party.MonthStart:MMM yyyy} can't be recoded — {bill.NotRecodableReason}. "
        + "Nothing was written and no second bill was staged; the site and cost-code split stays portal-side (the approved timesheets carry it).";

    private static string GoneWithoutReissue(CodingParty party, XeroBillSummary? gone, string billId) =>
        gone is null
            ? $"Bill {billId} is marked as {party.Owner}'s for {party.MonthStart:MMM yyyy} but Xero no longer has it "
              + "— sync from Xero (which clears its cover) and re-run."
            : $"Bill {BillLabel(gone)} for {party.MonthStart:MMM yyyy} can't be recoded — {gone.NotRecodableReason}, and no live bill "
              + "under the same number has replaced it. Nothing was written and no second bill was staged; the site and cost-code "
              + "split stays portal-side (the approved timesheets carry it).";
}

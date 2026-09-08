using static Jewel.JPMS.Api.Features.Labour.Commands.XeroCodingWording;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed partial class RunXeroCodingHandler
{
    /// <summary>
    /// The party's bill for the month (item A). Covered bills first — the accountant's explicit
    /// mark — then bills recognised by contact + period that nobody has marked yet. Several
    /// recognised bills are read fresh from Xero (K: a voided bill the ledger has not yet dropped
    /// must not shadow its re-issue); several LIVE ones sharing a number are chosen between
    /// (<see cref="ReissueChoice"/>); several with different numbers are a skip that says which.
    /// </summary>
    private async Task<(string? BillId, bool WasCovered, string? TooMany)> FindPartysBillAsync(CodingParty party, CancellationToken cancellationToken)
    {
        var coveredBillIds = party.Month.CoversBySub[party.CounterpartyId!]
            .Select(cover => party.Month.CoveredLinesById.TryGetValue(cover.XeroLedgerLineId, out var line) ? line.XeroInvoiceId : null)
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => id!)
            .Distinct()
            .ToList();
        if (coveredBillIds.Count > 1)
            return (null, false, $"{coveredBillIds.Count} different bills are marked as covering this month — resolve that on the settlement view first.");
        if (coveredBillIds.Count == 1) return (coveredBillIds[0], true, null);

        var recognised = RecognisedBills(party);
        if (recognised.Count == 1) return (recognised[0].Key, false, null);
        if (recognised.Count == 0) return (null, false, null);

        var standing = await StillStandingAsync(recognised, cancellationToken);
        if (standing.Count == 1) return (standing[0].Lines.Key, false, null);
        if (standing.Count == 0) return (null, false, null);
        if (ChooseAmongSameNumber(party, standing) is { } chosenBillId) return (chosenBillId, false, null);
        return (null, false, $"{standing.Count} bills in Xero look like {party.Owner}'s for {party.MonthStart:MMM yyyy}: "
            + string.Join(", ", standing.Select(bill => $"{LedgerBillLabel(bill.Lines.First())} ({bill.Lines.First().InvoiceStatus}, £{bill.Lines.First().InvoiceTotal:N2})"))
            + ". Mark the right one as settlement on the Cost allocation page's Labour tab, then re-run.");
    }
}

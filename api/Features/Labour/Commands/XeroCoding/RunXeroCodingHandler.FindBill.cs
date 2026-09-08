using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero;
using static Jewel.JPMS.Api.Features.Labour.Commands.XeroCodingWording;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed partial class RunXeroCodingHandler
{
    /// <summary>
    /// The party's bill for the month (item A). Covered bills first — the accountant's explicit
    /// mark — then bills recognised by contact + period that nobody has marked yet. More than one
    /// of either is a skip that says which — once Xero has been asked which of them still stand
    /// (K, 2026-09-08: a voided bill the ledger has not yet dropped must not shadow its re-issue).
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
        if (recognised.Count > 1) recognised = await StillStandingAsync(recognised, cancellationToken);
        if (recognised.Count > 1)
            return (null, false, $"{recognised.Count} bills in Xero look like {party.Owner}'s for {party.MonthStart:MMM yyyy}: "
                + string.Join(", ", recognised.Select(bill => $"{LedgerBillLabel(bill.First())} ({bill.First().InvoiceStatus}, £{bill.First().InvoiceTotal:N2})"))
                + ". Mark the right one as settlement on the Cost allocation page's Labour tab, then re-run.");
        return (recognised.SingleOrDefault()?.Key, false, null);
    }

    /// <summary>The candidates Xero still holds live — a bill voided since the ledger last saw
    /// it drops out here. A bill Xero cannot be asked about stays a candidate; the fresh read
    /// before any write reports that properly.</summary>
    private async Task<List<IGrouping<string, XeroLedgerLineEntity>>> StillStandingAsync(
        List<IGrouping<string, XeroLedgerLineEntity>> candidates, CancellationToken cancellationToken)
    {
        var standing = new List<IGrouping<string, XeroLedgerLineEntity>>();
        foreach (var candidate in candidates)
        {
            if (await StillStandsAsync(candidate.Key, cancellationToken)) standing.Add(candidate);
        }
        return standing;
    }

    private async Task<bool> StillStandsAsync(string billId, CancellationToken cancellationToken)
    {
        try
        {
            var bill = await xero.GetBillAsync(billId, cancellationToken);
            return bill is not null && !IsGone(bill.Status);
        }
        catch (XeroCallFailedException)
        {
            return true;
        }
    }
}

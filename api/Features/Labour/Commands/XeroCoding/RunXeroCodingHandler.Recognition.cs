using Jewel.JPMS.Api.Data.Entities;
using static Jewel.JPMS.Api.Features.Labour.Commands.XeroCodingWording;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed partial class RunXeroCodingHandler
{
    /// <summary>
    /// Bills in the ledger that read as the party's for the month: contact matches one of the
    /// party's names (the allocation page's rule), and the period matches — the month the
    /// invoice number / reference states where it states one ("Aug 2026", "August 2026",
    /// "08/2026", "2026-08"), else a bill dated from a week before the month to two weeks after
    /// it. Voided/deleted bills never count.
    /// </summary>
    private static List<IGrouping<string, XeroLedgerLineEntity>> RecognisedBills(CodingParty party) =>
        party.Month.NearbyLines
            .Where(line => !string.IsNullOrWhiteSpace(line.ContactName)
                && !IsGone(line.InvoiceStatus)
                && IsOneOf(line.ContactName!, party.ContactNames))
            .GroupBy(line => line.XeroInvoiceId)
            .Where(bill => IsForMonth(bill.First().InvoiceNumber, bill.First().Reference, bill.First().Date, party.Month))
            .OrderBy(bill => bill.First().Date)
            .ToList();

    private static bool IsOneOf(string contactName, IReadOnlyList<string> names) =>
        names.Any(name => WorkerDirectoryMatcher.Matches(contactName, name));

    private static bool IsForMonth(string? invoiceNumber, string? reference, DateTime? date, CodingMonth month)
    {
        var stated = StatedMonth(invoiceNumber) ?? StatedMonth(reference);
        if (stated is not null)
            return stated.Value.Year == month.Start.Year && stated.Value.Month == month.Start.Month;
        var dateFrom = month.Start.AddDays(-7).UtcDateTime;
        var dateTo = month.End.AddDays(14).UtcDateTime;
        return date is { } billDate && billDate >= dateFrom && billDate < dateTo;
    }
}

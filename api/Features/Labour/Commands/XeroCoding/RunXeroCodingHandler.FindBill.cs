using Jewel.JPMS.Api.Data.Entities;
using static Jewel.JPMS.Api.Features.Labour.Commands.XeroCodingWording;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed partial class RunXeroCodingHandler
{
    /// <summary>
    /// The worker's bill for the month (item A). Covered bills first — the accountant's explicit
    /// mark — then bills recognised by contact + period that nobody has marked yet. More than one
    /// of either is a skip that says which.
    /// </summary>
    private (string? BillId, bool WasCovered, string? TooMany) FindWorkersBill(WorkerRun run)
    {
        var schedule = run.Schedule;
        var coveredBillIds = run.Month.CoversBySub[schedule.SubcontractorId!]
            .Select(cover => run.Month.CoveredLinesById.TryGetValue(cover.XeroLedgerLineId, out var line) ? line.XeroInvoiceId : null)
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => id!)
            .Distinct()
            .ToList();
        if (coveredBillIds.Count > 1)
            return (null, false, $"{coveredBillIds.Count} different bills are marked as covering this month — resolve that on the settlement view first.");
        if (coveredBillIds.Count == 1) return (coveredBillIds[0], true, null);

        var recognised = RecognisedBills(run);
        if (recognised.Count > 1)
            return (null, false, $"{recognised.Count} bills in Xero look like {schedule.WorkerName}'s for {run.MonthStart:MMM yyyy}: "
                + string.Join(", ", recognised.Select(bill => $"{LedgerBillLabel(bill.First())} ({bill.First().InvoiceStatus}, £{bill.First().InvoiceTotal:N2})"))
                + ". Mark the right one as settlement on the Cost allocation page's Labour tab, then re-run.");
        return (recognised.SingleOrDefault()?.Key, false, null);
    }

    /// <summary>
    /// Bills in the ledger that read as this worker's for the month: contact matches the worker
    /// (their name or their linked company, the allocation page's rule), and the period matches —
    /// the month the invoice number / reference states where it states one ("Aug 2026",
    /// "August 2026", "08/2026", "2026-08"), else a bill dated from a week before the month to
    /// two weeks after it. Voided/deleted bills never count.
    /// </summary>
    private static List<IGrouping<string, XeroLedgerLineEntity>> RecognisedBills(WorkerRun run)
    {
        var names = new[] { run.Schedule.WorkerName, run.Schedule.SubcontractorName }
            .Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToList();
        return run.Month.NearbyLines
            .Where(line => !string.IsNullOrWhiteSpace(line.ContactName)
                && !IsGone(line.InvoiceStatus)
                && names.Any(name => WorkerDirectoryMatcher.Matches(line.ContactName!, name)))
            .GroupBy(line => line.XeroInvoiceId)
            .Where(bill => IsForMonth(bill.First(), run))
            .OrderBy(bill => bill.First().Date)
            .ToList();
    }

    private static bool IsForMonth(XeroLedgerLineEntity first, WorkerRun run)
    {
        var stated = StatedMonth(first.InvoiceNumber) ?? StatedMonth(first.Reference);
        if (stated is not null)
            return stated.Value.Year == run.MonthStart.Year && stated.Value.Month == run.MonthStart.Month;
        var dateFrom = run.MonthStart.AddDays(-7).UtcDateTime;
        var dateTo = run.MonthEnd.AddDays(14).UtcDateTime;
        return first.Date is { } date && date >= dateFrom && date < dateTo;
    }
}

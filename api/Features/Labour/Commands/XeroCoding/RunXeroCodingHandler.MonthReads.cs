using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed partial class RunXeroCodingHandler
{
    /// <summary>Everything the run reads once for the month and every worker then consults.</summary>
    private sealed record CodingMonth(
        DateTimeOffset Start,
        DateTimeOffset End,
        ILookup<string, XeroLineTimesheetCoverEntity> CoversBySub,
        IReadOnlyDictionary<string, XeroLedgerLineEntity> CoveredLinesById,
        IReadOnlyList<XeroLedgerLineEntity> NearbyLines,
        IReadOnlyDictionary<string, XeroCodingRunEntity> LatestRuns);

    private List<SiteXeroMappingEntity> siteMappingsCache = new();
    private List<CostCodeXeroMappingEntity> codeMappingsCache = new();
    private List<string> knownContacts = new();

    private async Task<CodingMonth> ReadMonthAsync(int year, int month, CancellationToken cancellationToken)
    {
        var monthStart = new DateTimeOffset(new DateTime(year, month, 1), TimeSpan.Zero);
        var monthEnd = monthStart.AddMonths(1);
        await ReadMappingsAsync(monthStart, monthEnd, cancellationToken);
        knownContacts = await ReadKnownContactsAsync(cancellationToken);
        var (coversBySub, coveredLinesById) = await ReadCoversAsync(monthStart, monthEnd, cancellationToken);
        var nearbyLines = await ReadNearbyBillLinesAsync(monthStart, monthEnd, cancellationToken);
        var latestRuns = (await context.XeroCodingRuns.AsNoTracking()
                .Where(run => run.Month == monthStart).OrderBy(run => run.RunAt).ToListAsync(cancellationToken))
            .GroupBy(run => run.WorkerId).ToDictionary(group => group.Key, group => group.Last());
        return new CodingMonth(monthStart, monthEnd, coversBySub, coveredLinesById, nearbyLines, latestRuns);
    }

    /// <summary>The rows effective for THIS month, oldest first — FindSiteMapping/FindCodeMapping
    /// take the last (latest-effective) match, so a mid-month re-map codes the month the new way.</summary>
    private async Task ReadMappingsAsync(DateTimeOffset monthStart, DateTimeOffset monthEnd, CancellationToken cancellationToken)
    {
        siteMappingsCache = await context.SiteXeroMappings
            .Where(row => row.EffectiveFrom < monthEnd && (row.EffectiveTo == null || row.EffectiveTo >= monthStart))
            .OrderBy(row => row.EffectiveFrom).ToListAsync(cancellationToken);
        codeMappingsCache = await context.CostCodeXeroMappings
            .Where(row => row.EffectiveFrom < monthEnd && (row.EffectiveTo == null || row.EffectiveTo >= monthStart))
            .OrderBy(row => row.EffectiveFrom).ToListAsync(cancellationToken);
    }

    /// <summary>Covers are read TRACKED: a recode re-points them (removes the old rows, adds new
    /// ones) in the same SaveChanges as the run's own record.</summary>
    private async Task<(ILookup<string, XeroLineTimesheetCoverEntity>, IReadOnlyDictionary<string, XeroLedgerLineEntity>)> ReadCoversAsync(
        DateTimeOffset monthStart, DateTimeOffset monthEnd, CancellationToken cancellationToken)
    {
        var covers = await context.XeroLineTimesheetCovers
            .Where(cover => cover.PeriodStart < monthEnd && cover.PeriodEnd > monthStart)
            .ToListAsync(cancellationToken);
        var coveredLineIds = covers.Select(cover => cover.XeroLedgerLineId).Distinct().ToList();
        var coveredLines = coveredLineIds.Count == 0
            ? new List<XeroLedgerLineEntity>()
            : await context.XeroLedgerLines.AsNoTracking()
                .Where(line => coveredLineIds.Contains(line.XeroLedgerLineId))
                .ToListAsync(cancellationToken);
        var coversBySub = covers.ToLookup(cover => cover.SubcontractorId);
        return (coversBySub, coveredLines.ToDictionary(line => line.XeroLedgerLineId));
    }

    /// <summary>The bills that could be a worker's for this month — item A's "match on contact +
    /// period": every ACCPAY line dated near the month (the period is taken from the bill's own
    /// stated month where Dext/the invoice number carries one, else a window either side of month
    /// end), recognised per worker by the ONE name rule the allocation page already uses. Read
    /// wide once, filtered in memory per worker.</summary>
    private Task<List<XeroLedgerLineEntity>> ReadNearbyBillLinesAsync(DateTimeOffset monthStart, DateTimeOffset monthEnd, CancellationToken cancellationToken)
    {
        var windowStart = monthStart.AddDays(-45).UtcDateTime;
        var windowEnd = monthEnd.AddDays(45).UtcDateTime;
        return context.XeroLedgerLines.AsNoTracking()
            .Where(line => line.Type == "ACCPAY" && line.Date != null && line.Date >= windowStart && line.Date < windowEnd)
            .ToListAsync(cancellationToken);
    }

    /// <summary>The contact name Xero already holds for each supplier (latest bill wins) — a
    /// staged draft goes to THAT contact rather than to a near-miss spelling that would create a
    /// second contact.</summary>
    private async Task<List<string>> ReadKnownContactsAsync(CancellationToken cancellationToken)
    {
        var contactsByLastBill = await context.XeroLedgerLines.AsNoTracking()
            .Where(line => line.Type == "ACCPAY" && line.ContactName != null && line.ContactName != "")
            .GroupBy(line => line.ContactName!)
            .Select(group => new { Name = group.Key, Last = group.Max(line => line.Date) })
            .ToListAsync(cancellationToken);
        return contactsByLastBill.OrderByDescending(row => row.Last).Select(row => row.Name).ToList();
    }

    private SiteXeroMappingEntity? FindSiteMapping(string projectId) =>
        siteMappingsCache.LastOrDefault(row => row.ProjectId == projectId);

    private CostCodeXeroMappingEntity? FindCodeMapping(string costCode) =>
        codeMappingsCache.LastOrDefault(row => string.Equals(row.CostCode, costCode, StringComparison.OrdinalIgnoreCase));
}

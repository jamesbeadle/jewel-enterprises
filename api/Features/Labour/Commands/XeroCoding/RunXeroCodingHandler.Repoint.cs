using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Xero;
using static Jewel.JPMS.Api.Features.Labour.Commands.XeroCodingWording;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed partial class RunXeroCodingHandler
{
    /// <summary>
    /// Item B: Xero issued new line ids, so the stored ledger lines are replaced with the fresh
    /// ones and every cover on the old lines is re-created on the new labour lines — same
    /// counterparty, same period, same project scope — all in the tracked context, saved with
    /// the run record. Only cost-of-sales lines are stored (the sync's own rule), so a cover
    /// never points at a line the next sync would drop. Returns how many lines now carry cover.
    /// </summary>
    private async Task<int> RepointCoverAndLedgerAsync(
        WorkerRun run, string billId, List<XeroLedgerLineEntity> storedLines, XeroBillSummary before,
        XeroBillRecodeResult recode, CancellationToken cancellationToken)
    {
        // EVERY cover on the bill's old lines, whatever month or project it was marked under —
        // a cover left pointing at a line Xero no longer has would silently drop the covered
        // total. (Instances already tracked from the month's read come back as the same objects.)
        var storedIds = storedLines.Select(line => line.XeroLedgerLineId).ToList();
        var oldCovers = storedIds.Count == 0
            ? new List<XeroLineTimesheetCoverEntity>()
            : await context.XeroLineTimesheetCovers
                .Where(cover => storedIds.Contains(cover.XeroLedgerLineId))
                .ToListAsync(cancellationToken);
        var coverTemplate = oldCovers
            .OrderByDescending(cover => cover.SubcontractorId == run.Schedule.SubcontractorId)
            .ThenBy(cover => cover.PeriodStart)
            .FirstOrDefault();
        var now = DateTimeOffset.UtcNow;

        foreach (var cover in oldCovers) context.XeroLineTimesheetCovers.Remove(cover);
        foreach (var line in storedLines) context.XeroLedgerLines.Remove(line);

        var covered = 0;
        foreach (var line in recode.Lines)
        {
            if (string.IsNullOrEmpty(line.LineItemId) || !IsCostOfSales(line.AccountCode)) continue;
            var entity = RecodedLedgerLine(billId, line, storedLines, before, recode, now);
            context.XeroLedgerLines.Add(entity);
            context.XeroLineTimesheetCovers.Add(CoverFor(run, entity, coverTemplate, now));
            covered++;
        }
        return covered;
    }

    /// <summary>Every recoded line settles the month. A bill that was covered keeps the cover's
    /// own scope (project, period, who marked it); a bill found by recognition is marked
    /// worker-month scoped — ProjectId "" — the same mark the Labour tab's "Mark as settlement"
    /// makes.</summary>
    private static XeroLineTimesheetCoverEntity CoverFor(
        WorkerRun run, XeroLedgerLineEntity line, XeroLineTimesheetCoverEntity? coverTemplate, DateTimeOffset now) => new()
    {
        XeroLineTimesheetCoverId = LabourIdentifierFactory.NextXeroLineTimesheetCoverId(),
        XeroLedgerLineId = line.XeroLedgerLineId,
        ProjectId = coverTemplate?.ProjectId ?? "",
        SubcontractorId = coverTemplate?.SubcontractorId ?? run.Schedule.SubcontractorId ?? "",
        PeriodStart = coverTemplate?.PeriodStart ?? run.MonthStart,
        PeriodEnd = coverTemplate?.PeriodEnd ?? run.MonthEnd,
        CreatedByEmail = coverTemplate?.CreatedByEmail ?? run.RunByEmail,
        CreatedAt = now,
    };

    private bool IsCostOfSales(string? accountCode)
    {
        if (xeroOptions.CostOfSalesAccountPrefixes.Count == 0) return true;
        if (string.IsNullOrWhiteSpace(accountCode)) return false;
        return xeroOptions.CostOfSalesAccountPrefixes.Any(prefix =>
            accountCode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}

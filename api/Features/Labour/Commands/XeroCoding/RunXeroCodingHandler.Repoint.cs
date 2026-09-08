using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed partial class RunXeroCodingHandler
{
    /// <summary>
    /// Item B: Xero issued new line ids, so the stored ledger lines are replaced with the fresh
    /// ones and every cover on the old lines is re-created on the new labour lines — same
    /// counterparty, same period, same project scope, and now the worker each line settles
    /// (J: a company bill's cover is marked per worker) — all in the tracked context, saved with
    /// the run record. The old lines include a voided predecessor's (K), so nothing stays pointed
    /// at a bill Xero has dropped. Only cost-of-sales lines are stored (the sync's own rule), so a
    /// cover never points at a line the next sync would drop. Returns how many lines now carry
    /// each worker's cover.
    /// </summary>
    private async Task<Dictionary<string, int>> RepointCoverAndLedgerAsync(
        CodingParty party, XeroBillSummary bill, List<XeroLedgerLineEntity> storedLines, List<CodedLine> codedLines,
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
            .OrderByDescending(cover => cover.SubcontractorId == party.CounterpartyId)
            .ThenBy(cover => cover.PeriodStart)
            .FirstOrDefault();
        var now = DateTimeOffset.UtcNow;

        foreach (var cover in oldCovers) context.XeroLineTimesheetCovers.Remove(cover);
        foreach (var line in storedLines) context.XeroLedgerLines.Remove(line);

        var coveredByWorker = party.Workers.ToDictionary(worker => worker.WorkerId, _ => 0);
        foreach (var (line, position) in recode.Lines.Select((line, position) => (line, position)))
        {
            if (string.IsNullOrEmpty(line.LineItemId) || !IsCostOfSales(line.AccountCode)) continue;
            var entity = RecodedLedgerLine(bill.InvoiceId, line, storedLines, bill, recode, now);
            var worker = WorkerOf(codedLines, line, position);
            context.XeroLedgerLines.Add(entity);
            context.XeroLineTimesheetCovers.Add(CoverFor(party, worker, entity, coverTemplate, now));
            if (worker is not null) coveredByWorker[worker.WorkerId]++;
        }
        return coveredByWorker;
    }

    /// <summary>The worker a fresh line settles: the line the run sent with that description,
    /// else the line sent in that position; a line neither answers for is covered for the
    /// counterparty as a whole rather than attributed to the wrong worker.</summary>
    private static WorkerRun? WorkerOf(List<CodedLine> codedLines, XeroRecodedLine line, int position) =>
        codedLines.FirstOrDefault(coded => coded.Line.Description == line.Description)?.Worker
        ?? (position < codedLines.Count ? codedLines[position].Worker : null);

    /// <summary>Every recoded line settles the month. A bill that was covered keeps the cover's
    /// own scope (project, period, who marked it); a bill found by recognition is marked
    /// worker-month scoped — ProjectId "" — the same mark the Labour tab's "Mark as settlement"
    /// makes.</summary>
    private static XeroLineTimesheetCoverEntity CoverFor(
        CodingParty party, WorkerRun? worker, XeroLedgerLineEntity line, XeroLineTimesheetCoverEntity? coverTemplate, DateTimeOffset now) => new()
    {
        XeroLineTimesheetCoverId = LabourIdentifierFactory.NextXeroLineTimesheetCoverId(),
        XeroLedgerLineId = line.XeroLedgerLineId,
        ProjectId = coverTemplate?.ProjectId ?? "",
        SubcontractorId = coverTemplate?.SubcontractorId ?? party.CounterpartyId ?? "",
        WorkerId = worker?.WorkerId,
        PeriodStart = coverTemplate?.PeriodStart ?? party.MonthStart,
        PeriodEnd = coverTemplate?.PeriodEnd ?? party.Month.End,
        CreatedByEmail = coverTemplate?.CreatedByEmail ?? party.Workers[0].RunByEmail,
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

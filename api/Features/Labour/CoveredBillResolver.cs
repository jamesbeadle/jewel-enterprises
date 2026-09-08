using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>
/// The bill each worker's month is covered by — read off the covers' ledger lines — and whether
/// it can be approved from the portal (2026-09-08): still DRAFT or SUBMITTED the last time the
/// ledger saw it, and every worker it covers reading Matches. A worker whose covers span two
/// bills has no single covered bill and nothing to approve.
/// </summary>
public static class CoveredBillResolver
{
    public static IReadOnlyList<WorkerSettlementSchedule> Attach(
        IReadOnlyList<WorkerSettlementSchedule> rows,
        ILookup<string, XeroLineTimesheetCoverEntity> coversBySub,
        IReadOnlyDictionary<string, XeroLedgerLineEntity> coveredLinesById)
    {
        var lineByWorker = rows.ToDictionary(row => row.WorkerId, row => CoveredLineOf(row, coversBySub, coveredLinesById));
        var rowsByBill = rows
            .Where(row => lineByWorker[row.WorkerId] is not null)
            .ToLookup(row => lineByWorker[row.WorkerId]!.XeroInvoiceId);
        return rows.Select(row => row with { CoveredBill = Describe(lineByWorker[row.WorkerId], rowsByBill) }).ToList();
    }

    /// <summary>One of the ledger lines the worker's covers point at — when they all point at the
    /// same bill. Per-worker covers count for their worker; a cover without a worker is the
    /// counterparty's as a whole.</summary>
    private static XeroLedgerLineEntity? CoveredLineOf(
        WorkerSettlementSchedule row,
        ILookup<string, XeroLineTimesheetCoverEntity> coversBySub,
        IReadOnlyDictionary<string, XeroLedgerLineEntity> coveredLinesById)
    {
        if (row.SubcontractorId is null) return null;
        var lines = coversBySub[row.SubcontractorId]
            .Where(cover => cover.WorkerId is null || cover.WorkerId == row.WorkerId)
            .Select(cover => coveredLinesById.TryGetValue(cover.XeroLedgerLineId, out var line) ? line : null)
            .Where(line => line is not null)
            .Select(line => line!)
            .ToList();
        var bills = lines.Select(line => line.XeroInvoiceId).Distinct().Count();
        return bills == 1 ? lines[0] : null;
    }

    private static CoveredBill? Describe(XeroLedgerLineEntity? line, ILookup<string, WorkerSettlementSchedule> rowsByBill)
    {
        if (line is null) return null;
        var workersOnBill = rowsByBill[line.XeroInvoiceId].ToList();
        var isApprovable = IsAwaitingApproval(line.InvoiceStatus)
            && workersOnBill.All(worker => worker.Verdict == ScheduleVerdict.Matches);
        return new CoveredBill(
            line.XeroInvoiceId,
            BillLabel(line),
            line.InvoiceStatus,
            line.InvoiceTotal,
            workersOnBill.Select(worker => worker.WorkerName).ToList(),
            isApprovable);
    }

    public static bool IsAwaitingApproval(string status) =>
        status.Equals("DRAFT", StringComparison.OrdinalIgnoreCase)
        || status.Equals("SUBMITTED", StringComparison.OrdinalIgnoreCase);

    private static string BillLabel(XeroLedgerLineEntity line) =>
        !string.IsNullOrWhiteSpace(line.InvoiceNumber) ? line.InvoiceNumber
        : !string.IsNullOrWhiteSpace(line.Reference) ? line.Reference
        : line.XeroInvoiceId;
}

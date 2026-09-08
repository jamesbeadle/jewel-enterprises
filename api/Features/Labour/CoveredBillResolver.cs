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
        var linesByWorker = rows.ToDictionary(row => row.WorkerId, row => CoveredLinesOf(row, coversBySub, coveredLinesById));
        var rowsByBill = rows
            .Where(row => linesByWorker[row.WorkerId].Count > 0)
            .ToLookup(row => linesByWorker[row.WorkerId][0].XeroInvoiceId);
        return rows.Select(row => row with { CoveredBill = Describe(linesByWorker[row.WorkerId], rowsByBill) }).ToList();
    }

    /// <summary>The ledger lines the worker's covers point at — when they all point at the same
    /// bill; empty otherwise. Per-worker covers count for their worker; a cover without a worker
    /// is the counterparty's as a whole.</summary>
    private static List<XeroLedgerLineEntity> CoveredLinesOf(
        WorkerSettlementSchedule row,
        ILookup<string, XeroLineTimesheetCoverEntity> coversBySub,
        IReadOnlyDictionary<string, XeroLedgerLineEntity> coveredLinesById)
    {
        if (row.SubcontractorId is null) return new List<XeroLedgerLineEntity>();
        var lines = coversBySub[row.SubcontractorId]
            .Where(cover => cover.WorkerId is null || cover.WorkerId == row.WorkerId)
            .Select(cover => coveredLinesById.TryGetValue(cover.XeroLedgerLineId, out var line) ? line : null)
            .Where(line => line is not null)
            .Select(line => line!)
            .ToList();
        var bills = lines.Select(line => line.XeroInvoiceId).Distinct().Count();
        return bills == 1 ? lines : new List<XeroLedgerLineEntity>();
    }

    private static CoveredBill? Describe(List<XeroLedgerLineEntity> lines, ILookup<string, WorkerSettlementSchedule> rowsByBill)
    {
        if (lines.Count == 0) return null;
        var line = lines[0];
        var workersOnBill = rowsByBill[line.XeroInvoiceId].ToList();
        var isApprovable = IsAwaitingApproval(line.InvoiceStatus)
            && workersOnBill.All(worker => worker.Verdict == ScheduleVerdict.Matches);
        return new CoveredBill(
            line.XeroInvoiceId,
            BillLabel(line),
            line.InvoiceStatus,
            line.InvoiceTotal,
            workersOnBill.Select(worker => worker.WorkerName).ToList(),
            isApprovable,
            lines.Select(covered => covered.XeroLedgerLineId).OrderBy(id => id).ToList());
    }

    public static bool IsAwaitingApproval(string status) =>
        status.Equals("DRAFT", StringComparison.OrdinalIgnoreCase)
        || status.Equals("SUBMITTED", StringComparison.OrdinalIgnoreCase);

    private static string BillLabel(XeroLedgerLineEntity line) =>
        !string.IsNullOrWhiteSpace(line.InvoiceNumber) ? line.InvoiceNumber
        : !string.IsNullOrWhiteSpace(line.Reference) ? line.Reference
        : line.XeroInvoiceId;
}

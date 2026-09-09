using Microsoft.EntityFrameworkCore;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

internal static partial class XeroLedgerReads
{
    /// <summary>
    /// Work Order bill recognition for every unallocated line in the read, keyed by line id. The
    /// decision is per bill, so the lines are grouped by invoice first; labour recognition's
    /// answer and the bill's site go in with them. The site is the project already decided on
    /// the bill in the portal (the queue's "Set project" half-step) when there is one, else the
    /// suggester's project for its Xero Sites tracking — the same precedence the sweep uses
    /// (2026-09-09: a WO number open on two projects was dropped even after the accountant had
    /// set the project). Null entries are lines no order came into.
    /// </summary>
    public static Dictionary<string, WorkOrderBillRecognition.LineVerdict> WorkOrderBillsFor(
        WorkOrderBillRecognition? recognition,
        IReadOnlyList<XeroLedgerLineEntity> entities,
        LabourSupplierRecognition? labour,
        XeroAllocationSuggester? suggester)
    {
        var verdicts = new Dictionary<string, WorkOrderBillRecognition.LineVerdict>(StringComparer.OrdinalIgnoreCase);
        if (recognition is null) return verdicts;

        var bills = entities
            .Where(entity => entity.AllocationStatus == (int)XeroAllocationStatus.Unallocated)
            .GroupBy(entity => entity.XeroInvoiceId, StringComparer.OrdinalIgnoreCase);
        foreach (var bill in bills)
        {
            var billLines = bill.ToList();
            var isLabour = billLines.Any(line => labour?.For(line) is { } recognised
                                                 && (recognised.MatchedWorkerId is not null || recognised.CoveredByTimesheets));
            var hintedProjectId = billLines
                .Select(line => line.ProjectId ?? suggester?.SuggestProject(line.XeroSite))
                .FirstOrDefault(projectId => projectId is not null);
            foreach (var line in billLines)
                if (recognition.ForLine(line, billLines, isLabour, hintedProjectId) is { } verdict)
                    verdicts[line.XeroLedgerLineId] = verdict;
        }
        return verdicts;
    }

    /// <summary>
    /// The standing Work Order bill approval behind each ALLOCATED line's bill, keyed by invoice
    /// id — who approved, against which order(s) and for how much each, by which rule. Only
    /// allocated lines carry one, so an unallocated page issues no approval query at all.
    /// </summary>
    public static async Task<Dictionary<string, WorkOrderBillApprovalStamp>> WorkOrderApprovalsForAsync(
        JpmsContext context, IReadOnlyList<XeroLedgerLineEntity> entities, CancellationToken cancellationToken)
    {
        var invoiceIds = entities
            .Where(entity => entity.AllocationStatus == (int)XeroAllocationStatus.Allocated)
            .Select(entity => entity.XeroInvoiceId)
            .Distinct()
            .ToList();
        if (invoiceIds.Count == 0) return new Dictionary<string, WorkOrderBillApprovalStamp>();

        var approvals = await context.WorkOrderBillApprovals.AsNoTracking()
            .Where(approval => invoiceIds.Contains(approval.XeroInvoiceId) && approval.UndoneAtUtc == null)
            .ToListAsync(cancellationToken);
        if (approvals.Count == 0) return new Dictionary<string, WorkOrderBillApprovalStamp>();

        var orderIds = approvals.Select(approval => approval.WorkOrderId).Distinct().ToList();
        var references = await context.WorkOrders.AsNoTracking()
            .Where(order => orderIds.Contains(order.WorkOrderId))
            .ToDictionaryAsync(order => order.WorkOrderId, order => order.Reference, cancellationToken);

        return approvals
            .GroupBy(approval => approval.XeroInvoiceId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group =>
            {
                var latest = group.OrderByDescending(row => row.ApprovedAtUtc).First();
                var orders = group
                    .Select(row => new WorkOrderBillApprovedOrder(
                        row.WorkOrderId,
                        references.TryGetValue(row.WorkOrderId, out var reference) ? reference : row.WorkOrderId,
                        row.BillNet))
                    .OrderBy(order => order.WorkOrderReference)
                    .ToList();
                return new WorkOrderBillApprovalStamp(orders, (WorkOrderMatchRule)latest.MatchRule, latest.ApprovedByEmail, latest.ApprovedAtUtc);
            }, StringComparer.OrdinalIgnoreCase);
    }
}

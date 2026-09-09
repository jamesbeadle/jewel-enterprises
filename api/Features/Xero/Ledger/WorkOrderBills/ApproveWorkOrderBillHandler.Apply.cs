using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Commercial;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger.WorkOrderBills;

public sealed partial class ApproveWorkOrderBillHandler
{
    /// <summary>Queued lines carry no split rows, but a stale one must not survive beside the new coding.</summary>
    private async Task ClearStaleSplitsAsync(List<XeroLedgerLineEntity> lines, CancellationToken cancellationToken)
    {
        var ids = lines.Select(line => line.XeroLedgerLineId).ToList();
        var stale = await context.XeroCostSplits.Where(split => ids.Contains(split.XeroLedgerLineId)).ToListAsync(cancellationToken);
        context.XeroCostSplits.RemoveRange(stale);
    }

    /// <summary>
    /// What one line is left with: allocated whole to one project and centre when every share
    /// agrees, else its centres in XeroCostSplits (one row per project + code, shares on the
    /// same centre summed) — and one work-order link per share, order and code, for its signed
    /// amount, so each order is invoiced by exactly its slice. The line itself — its account,
    /// its net — is untouched: the splits are the portal's, never Xero's.
    /// </summary>
    private void Allocate(XeroLedgerLineEntity line, IReadOnlyList<WorkOrderBillLineShare> shares, Dictionary<string, PaidOrder> orders, string approvedBy, DateTimeOffset now)
    {
        var centres = shares
            .GroupBy(share => (ProjectId: orders[share.WorkOrderId].ProjectId, share.CostCenterCode), CentreComparer.Instance)
            .Select(group => (group.Key.ProjectId, group.Key.CostCenterCode, Net: group.Sum(share => share.Net)))
            .ToList();
        var projects = centres.Select(centre => centre.ProjectId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        line.AllocationStatus = (int)XeroAllocationStatus.Allocated;
        line.ProjectId = projects.Count == 1 ? projects[0] : null;
        line.CostCenterCode = centres.Count == 1 ? centres[0].CostCenterCode : null;
        line.Bucket = null;
        line.AllocatedBy = approvedBy;
        line.AllocatedAtUtc = now;
        line.Note = NoteFor(shares, orders);
        if (centres.Count > 1)
            foreach (var centre in centres)
                context.XeroCostSplits.Add(new XeroCostSplitEntity
                {
                    XeroCostSplitId = $"{line.XeroLedgerLineId}:{centre.ProjectId}:{centre.CostCenterCode}",
                    XeroLedgerLineId = line.XeroLedgerLineId,
                    ProjectId = centre.ProjectId,
                    CostCenterCode = centre.CostCenterCode,
                    Net = centre.Net
                });
        foreach (var share in shares)
            context.XeroLineWorkOrderLinks.Add(new XeroLineWorkOrderLinkEntity
            {
                XeroLineWorkOrderLinkId = CommercialIdentifierFactory.NextXeroLineWorkOrderLinkId(),
                XeroLedgerLineId = line.XeroLedgerLineId,
                WorkOrderId = share.WorkOrderId,
                ProjectId = orders[share.WorkOrderId].ProjectId,
                CostCenterCode = share.CostCenterCode,
                Amount = line.Type == "ACCPAYCREDIT" ? -share.Net : share.Net
            });
    }

    private static string NoteFor(IReadOnlyList<WorkOrderBillLineShare> shares, Dictionary<string, PaidOrder> orders)
    {
        var references = shares.Select(share => orders[share.WorkOrderId].Reference).Distinct().OrderBy(reference => reference).ToList();
        return references.Count == 1 ? $"Work order {references[0]}" : $"Work orders {string.Join(", ", references)}";
    }

    private void RecordApproval(ApproveWorkOrderBill command, PaidOrder order, WorkOrderBillMatch match, decimal sliceNet, DateTimeOffset now) =>
        context.WorkOrderBillApprovals.Add(new WorkOrderBillApprovalEntity
        {
            WorkOrderBillApprovalId = Guid.NewGuid().ToString("N"),
            XeroInvoiceId = command.XeroInvoiceId,
            WorkOrderId = order.WorkOrderId,
            ProjectId = order.ProjectId,
            MatchRule = (int)match.Rule,
            MatchDetail = match.Detail.Length <= 512 ? match.Detail : match.Detail[..512],
            BillNet = sliceNet,
            ApprovedByEmail = command.ApprovedBy ?? "",
            ApprovedAtUtc = now
        });

    private sealed class CentreComparer : IEqualityComparer<(string ProjectId, string CostCenterCode)>
    {
        public static readonly CentreComparer Instance = new();
        public bool Equals((string ProjectId, string CostCenterCode) x, (string ProjectId, string CostCenterCode) y) =>
            string.Equals(x.ProjectId, y.ProjectId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.CostCenterCode, y.CostCenterCode, StringComparison.OrdinalIgnoreCase);
        public int GetHashCode((string ProjectId, string CostCenterCode) key) =>
            HashCode.Combine(key.ProjectId.ToUpperInvariant(), key.CostCenterCode.ToUpperInvariant());
    }
}

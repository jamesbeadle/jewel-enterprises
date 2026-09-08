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
    /// What one line is left with: allocated to the order's project — the whole line on one
    /// centre, or its shares in XeroCostSplits when the order spans several — and linked to the
    /// order for its full (signed) net, the same slice the WO Allocation tab would have written.
    /// </summary>
    private void Allocate(XeroLedgerLineEntity line, WorkOrderEntity order, IReadOnlyList<XeroCostSplit> shares, string approvedBy, DateTimeOffset now)
    {
        line.AllocationStatus = (int)XeroAllocationStatus.Allocated;
        line.ProjectId = order.ProjectId;
        line.CostCenterCode = shares.Count == 1 ? shares[0].CostCenterCode : null;
        line.Bucket = null;
        line.AllocatedBy = approvedBy;
        line.AllocatedAtUtc = now;
        line.Note = $"Work order {order.Reference}";
        if (shares.Count > 1)
            foreach (var share in shares)
                context.XeroCostSplits.Add(new XeroCostSplitEntity
                {
                    XeroCostSplitId = $"{line.XeroLedgerLineId}:{order.ProjectId}:{share.CostCenterCode}",
                    XeroLedgerLineId = line.XeroLedgerLineId,
                    ProjectId = order.ProjectId,
                    CostCenterCode = share.CostCenterCode,
                    Net = share.Net
                });
        context.XeroLineWorkOrderLinks.Add(new XeroLineWorkOrderLinkEntity
        {
            XeroLineWorkOrderLinkId = CommercialIdentifierFactory.NextXeroLineWorkOrderLinkId(),
            XeroLedgerLineId = line.XeroLedgerLineId,
            WorkOrderId = order.WorkOrderId,
            ProjectId = order.ProjectId,
            Amount = line.Type == "ACCPAYCREDIT" ? -line.Net : line.Net
        });
    }

    private void RecordApproval(ApproveWorkOrderBill command, WorkOrderEntity order, WorkOrderBillMatch match, List<XeroLedgerLineEntity> lines, DateTimeOffset now) =>
        context.WorkOrderBillApprovals.Add(new WorkOrderBillApprovalEntity
        {
            WorkOrderBillApprovalId = Guid.NewGuid().ToString("N"),
            XeroInvoiceId = command.XeroInvoiceId,
            WorkOrderId = order.WorkOrderId,
            ProjectId = order.ProjectId,
            MatchRule = (int)match.Rule,
            MatchDetail = match.Detail.Length <= 512 ? match.Detail : match.Detail[..512],
            BillNet = BillNet(lines),
            ApprovedByEmail = command.ApprovedBy ?? "",
            ApprovedAtUtc = now
        });
}

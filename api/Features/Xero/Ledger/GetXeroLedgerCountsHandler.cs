using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

/// <summary>
/// One GROUP BY for the allocation page's tab bar. The page shows a count against every status but
/// only holds one status' lines at a time, so the counts can't come from counting what was
/// downloaded any more — and shouldn't have, since that meant downloading the whole ledger to
/// render four numbers.
///
/// Since 2026-09-10 the Unallocated figure also comes back partitioned the way the page will
/// partition it (lines to code / Work Order bills / labour outstanding / labour covered), from
/// <see cref="XeroLedgerReads.UnallocatedPartitionAsync"/>. The home tile read the raw status
/// count and said 44 against a tab bar that added up to 15 — 27 of those were worker bills
/// already covered by timesheets, and 4 were two Work Order bills. The partition costs one more
/// read of the (small, indexed) unallocated queue plus the registry and order lookups recognition
/// already does on every visit to the page.
/// </summary>
public sealed class GetXeroLedgerCountsHandler : IQueryHandler<GetXeroLedgerCounts, XeroLedgerCounts>
{
    private readonly JpmsContext context;

    public GetXeroLedgerCountsHandler(JpmsContext context) { this.context = context; }

    public async Task<XeroLedgerCounts> HandleAsync(GetXeroLedgerCounts query, CancellationToken cancellationToken)
    {
        var byStatus = await context.XeroLedgerLines.AsNoTracking()
            .GroupBy(line => line.AllocationStatus)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.Status, row => row.Count, cancellationToken);

        int CountOf(XeroAllocationStatus status) =>
            byStatus.TryGetValue((int)status, out var count) ? count : 0;

        var partition = CountOf(XeroAllocationStatus.Unallocated) == 0
            ? default
            : await XeroLedgerReads.UnallocatedPartitionAsync(context, cancellationToken);

        return new XeroLedgerCounts(
            Unallocated: CountOf(XeroAllocationStatus.Unallocated),
            Allocated:   CountOf(XeroAllocationStatus.Allocated),
            Bucketed:    CountOf(XeroAllocationStatus.Bucketed),
            Ignored:     CountOf(XeroAllocationStatus.Ignored),
            Disputed:    CountOf(XeroAllocationStatus.Disputed),
            ToCode:            partition.ToCode,
            WorkOrderBills:    partition.WorkOrderBills,
            LabourOutstanding: partition.LabourOutstanding,
            LabourCovered:     partition.LabourCovered);
    }
}

using Microsoft.EntityFrameworkCore;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

internal static partial class XeroLedgerReads
{
    /// <summary>
    /// The Unallocated status split the way the allocation page splits it (2026-09-10): the
    /// ordinary queue (lines to code, project-suggested or not), the Work Order bills tab
    /// (counted as bills, one Approve each) and the Labour section (recognised worker bills —
    /// outstanding, or already covered by timesheets and therefore nothing to do).
    /// </summary>
    public readonly record struct UnallocatedPartition(int ToCode, int WorkOrderBills, int LabourOutstanding, int LabourCovered);

    /// <summary>
    /// Partitions every unallocated line with the same recognition the unallocated read runs —
    /// labour first, then Work Order bills, the rest to code — so a count shown anywhere else
    /// (the home tile) matches what the page's tab bar will say when it opens. The rule per line
    /// is the page's own: a line the labour registry claims belongs to Labour even when its bill
    /// also matched an order; a Work Order bill is a bill whose lines carry a match, counted once
    /// per invoice; everything else is a line someone has to code. The per-visit escape hatches
    /// ("not labour", "not a work-order bill") are the browser's and do not exist here.
    ///
    /// Reads only the unallocated lines (an indexed status filter — the queue is small) plus the
    /// registry and order tables recognition needs, and nothing at all when the queue is empty.
    /// </summary>
    public static async Task<UnallocatedPartition> UnallocatedPartitionAsync(JpmsContext context, CancellationToken cancellationToken)
    {
        var entities = await context.XeroLedgerLines.AsNoTracking()
            .Where(line => line.AllocationStatus == (int)XeroAllocationStatus.Unallocated)
            .ToListAsync(cancellationToken);
        if (entities.Count == 0) return default;

        var suggester = await SuggesterForAsync(context, entities, cancellationToken);
        var labour = await LabourSupplierRecognition.ForAsync(context, entities, cancellationToken);
        var workOrderBills = WorkOrderBillsFor(
            await WorkOrderBillRecognition.ForAsync(context, entities, cancellationToken), entities, labour, suggester);

        var toCode = 0;
        var labourOutstanding = 0;
        var labourCovered = 0;
        var billsToApprove = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entity in entities)
        {
            if (labour?.For(entity) is { } recognised && (recognised.MatchedWorkerId is not null || recognised.CoveredByTimesheets))
            {
                if (recognised.CoveredByTimesheets) labourCovered++;
                else labourOutstanding++;
                continue;
            }

            if (workOrderBills.TryGetValue(entity.XeroLedgerLineId, out var verdict) && verdict.Match is not null)
            {
                billsToApprove.Add(entity.XeroInvoiceId);
                continue;
            }

            toCode++;
        }

        return new UnallocatedPartition(toCode, billsToApprove.Count, labourOutstanding, labourCovered);
    }
}

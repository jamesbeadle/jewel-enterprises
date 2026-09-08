using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

public sealed class ListXeroLedgerLinesHandler : IQueryHandler<ListXeroLedgerLines, IReadOnlyList<XeroLedgerLine>>
{
    private readonly JpmsContext context;

    public ListXeroLedgerLinesHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<XeroLedgerLine>> HandleAsync(ListXeroLedgerLines query, CancellationToken cancellationToken)
    {
        // Filter in SQL. The allocation page asks for one status at a time — it is a tab per
        // status — so this is the difference between reading the tab someone is looking at and
        // reading every line the business has ever received. (XeroLedgerLines already carries an
        // index on AllocationStatus.)
        var lines = context.XeroLedgerLines.AsNoTracking();
        if (query.Status is { } status)
            lines = lines.Where(line => line.AllocationStatus == (int)status);

        var entities = await lines
            .OrderByDescending(line => line.Date)
            .ToListAsync(cancellationToken);

        var splitsByLine = await XeroLedgerReads.SplitsForAsync(context, entities, cancellationToken);
        var suggester = await XeroLedgerReads.SuggesterForAsync(context, entities, cancellationToken);
        var messagesByLine = await XeroLedgerReads.DisputeMessagesForAsync(context, entities, cancellationToken);
        // Labour recognition rides the same read as the suggestions (and, like them, is only
        // computed while unallocated lines are in the response) so the queue, the Labour section
        // and the "re-check" refresh all see one rule.
        var labour = await LabourSupplierRecognition.ForAsync(context, entities, cancellationToken);
        // Work Order bill recognition rides the same read too (2026-09-08): a bill whose supplier
        // has an open order is decided per bill, labour recognition's answer and the bill's Sites
        // hint in hand, and the verdict lands on each of its lines.
        var workOrderBills = XeroLedgerReads.WorkOrderBillsFor(
            await WorkOrderBillRecognition.ForAsync(context, entities, cancellationToken), entities, labour, suggester);
        var approvalsByInvoice = await XeroLedgerReads.WorkOrderApprovalsForAsync(context, entities, cancellationToken);

        return entities.Select(entity => XeroLedgerReads.ToModel(
            entity,
            splitsByLine.TryGetValue(entity.XeroLedgerLineId, out var splits) ? splits : null,
            suggester,
            messagesByLine.TryGetValue(entity.XeroLedgerLineId, out var messages) ? messages : null,
            labour?.For(entity),
            workOrderBills.TryGetValue(entity.XeroLedgerLineId, out var workOrderBill) ? workOrderBill : null,
            approvalsByInvoice.TryGetValue(entity.XeroInvoiceId, out var approval) ? approval : null)).ToList();
    }
}

using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger.WorkOrderBills;

public sealed partial class ApproveWorkOrderBillHandler
{
    private static readonly HashSet<string> LockedStatuses = new(StringComparer.OrdinalIgnoreCase) { "PAID", "VOIDED", "DELETED" };

    /// <summary>An order the bill's slices pay: the stored order plus the read's figures for it.</summary>
    private sealed record PaidOrder(WorkOrderEntity Entity, WorkOrderBillOrderOption Option)
    {
        public string WorkOrderId => Entity.WorkOrderId;
        public string Reference => Entity.Reference;
        public string ProjectId => Entity.ProjectId;
    }

    /// <summary>The bill as stored must be whole and still queued.</summary>
    private static void GuardBill(List<XeroLedgerLineEntity> lines)
    {
        if (lines.Count == 0)
            throw new InvalidOperationException("No stored ledger lines for this bill — sync from Xero and try again.");
        if (lines.Any(line => line.AllocationStatus != (int)XeroAllocationStatus.Unallocated))
            throw new InvalidOperationException("Every line of the bill must still be unallocated — it has moved since the card was drawn.");
        if (LockedStatuses.Contains(lines[0].InvoiceStatus))
            throw new InvalidOperationException($"The bill is {lines[0].InvoiceStatus} in Xero and can't be approved from here.");
    }

    /// <summary>The rule re-run at the moment of approval — the same recogniser the read used,
    /// over the bill's lines alone — must still give the bill to the supplier's orders.</summary>
    private async Task<(WorkOrderBillMatch Match, WorkOrderBillRecognition Recognition)> RequireMatchAsync(
        List<XeroLedgerLineEntity> lines, CancellationToken cancellationToken)
    {
        var recognition = await WorkOrderBillRecognition.ForAsync(context, lines, cancellationToken);
        var labour = await LabourSupplierRecognition.ForAsync(context, lines, cancellationToken);
        var suggester = await XeroLedgerReads.SuggesterForAsync(context, lines, cancellationToken);
        var verdicts = XeroLedgerReads.WorkOrderBillsFor(recognition, lines, labour, suggester);
        if (!verdicts.TryGetValue(lines[0].XeroLedgerLineId, out var verdict) || verdict.Match is null)
            throw new InvalidOperationException(verdict.ExceptionReason
                ?? "The bill no longer matches a work order — re-check matches and try again.");
        return (verdict.Match, recognition!);
    }

    /// <summary>Every order the slices name must be an open order of the bill's supplier.</summary>
    private async Task<Dictionary<string, PaidOrder>> RequireOrdersAsync(
        ApproveWorkOrderBill command, WorkOrderBillMatch match, CancellationToken cancellationToken)
    {
        var optionsById = match.SupplierOrders.ToDictionary(option => option.WorkOrderId, StringComparer.OrdinalIgnoreCase);
        var named = command.Slices.Select(slice => slice.WorkOrderId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (named.Any(id => !optionsById.ContainsKey(id)))
            throw new InvalidOperationException("A slice names an order that is not an open order of this supplier — re-check matches and try again.");

        var entities = await context.WorkOrders.Where(order => named.Contains(order.WorkOrderId)).ToListAsync(cancellationToken);
        return entities.ToDictionary(order => order.WorkOrderId, order => new PaidOrder(order, optionsById[order.WorkOrderId]), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The slices are the bill: they add up to its net, and each carries the bill's sign.</summary>
    private static void RequireSlicesTieToTheBill(ApproveWorkOrderBill command, List<XeroLedgerLineEntity> lines)
    {
        var billNet = BillNet(lines);
        var placed = command.Slices.Sum(slice => slice.Net);
        if (placed != billNet)
            throw new InvalidOperationException(
                $"The order figures must add up to the bill's net of £{billNet:N2} — they add up to £{placed:N2}.");
        if (command.Slices.Any(slice => Math.Sign(slice.Net) != Math.Sign(billNet)))
            throw new InvalidOperationException("Every order's figure must carry the bill's own sign — a credit note gives value back to every order it names.");
    }

    /// <summary>Each order's slice of the bill must fit inside what is left to invoice on it.</summary>
    private static void RequireEachOrderWithinValue(ApproveWorkOrderBill command, Dictionary<string, PaidOrder> orders)
    {
        foreach (var slice in command.Slices)
        {
            var order = orders[slice.WorkOrderId];
            if (slice.Net <= order.Option.Remaining) continue;
            throw new InvalidOperationException(
                $"The bill would take {order.Reference} over its value by £{slice.Net - order.Option.Remaining:N2} — "
                + $"£{order.Option.Remaining:N2} of £{order.Option.OrderValue:N2} is left to invoice.");
        }
    }
}

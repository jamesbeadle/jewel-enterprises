using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger.WorkOrderBills;

public sealed partial class ApproveWorkOrderBillHandler
{
    private static readonly HashSet<string> LockedStatuses = new(StringComparer.OrdinalIgnoreCase) { "PAID", "VOIDED", "DELETED" };

    /// <summary>An order the bill's shares pay: the stored order plus the read's figures for it.</summary>
    private sealed record PaidOrder(WorkOrderEntity Entity, WorkOrderBillOrderOption Option)
    {
        public string WorkOrderId => Entity.WorkOrderId;
        public string Reference => Entity.Reference;
        public string ProjectId => Entity.ProjectId;
    }

    /// <summary>The bill as stored must be whole, still queued, and coded line for line.</summary>
    private static void GuardBill(ApproveWorkOrderBill command, List<XeroLedgerLineEntity> lines)
    {
        if (lines.Count == 0)
            throw new InvalidOperationException("No stored ledger lines for this bill — sync from Xero and try again.");
        if (lines.Any(line => line.AllocationStatus != (int)XeroAllocationStatus.Unallocated))
            throw new InvalidOperationException("Every line of the bill must still be unallocated — it has moved since the card was drawn.");
        if (LockedStatuses.Contains(lines[0].InvoiceStatus))
            throw new InvalidOperationException($"The bill is {lines[0].InvoiceStatus} in Xero and can't be approved from here.");

        var stored = lines.Select(line => line.XeroLedgerLineId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var coded = command.Lines.Select(line => line.XeroLedgerLineId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!stored.SetEquals(coded))
            throw new InvalidOperationException("Approve codes every line of the bill at once — the lines sent don't match the bill's lines.");
    }

    /// <summary>The rule re-run at the moment of approval — the same recogniser the read used,
    /// over the bill's lines alone — must still give the bill to the supplier's orders.</summary>
    private async Task<WorkOrderBillMatch> RequireMatchAsync(List<XeroLedgerLineEntity> lines, CancellationToken cancellationToken)
    {
        var recognition = await WorkOrderBillRecognition.ForAsync(context, lines, cancellationToken);
        var labour = await LabourSupplierRecognition.ForAsync(context, lines, cancellationToken);
        var suggester = await XeroLedgerReads.SuggesterForAsync(context, lines, cancellationToken);
        var verdicts = XeroLedgerReads.WorkOrderBillsFor(recognition, lines, labour, suggester);
        if (!verdicts.TryGetValue(lines[0].XeroLedgerLineId, out var verdict) || verdict.Match is null)
            throw new InvalidOperationException(verdict.ExceptionReason
                ?? "The bill no longer matches a work order — re-check matches and try again.");
        return verdict.Match;
    }

    /// <summary>Every order the shares name must be an open order of the bill's supplier.</summary>
    private async Task<Dictionary<string, PaidOrder>> RequireOrdersAsync(
        ApproveWorkOrderBill command, WorkOrderBillMatch match, CancellationToken cancellationToken)
    {
        var optionsById = match.SupplierOrders.ToDictionary(option => option.WorkOrderId, StringComparer.OrdinalIgnoreCase);
        var named = command.Lines.SelectMany(line => line.Shares).Select(share => share.WorkOrderId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var strangers = named.Where(id => !optionsById.ContainsKey(id)).ToList();
        if (strangers.Count > 0)
            throw new InvalidOperationException("A share names an order that is not an open order of this supplier — re-check matches and try again.");

        var entities = await context.WorkOrders.Where(order => named.Contains(order.WorkOrderId)).ToListAsync(cancellationToken);
        return entities.ToDictionary(order => order.WorkOrderId, order => new PaidOrder(order, optionsById[order.WorkOrderId]), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Each line's shares must add up to its net, each on one of its order's own cost
    /// codes, and name active centres. Returns the shares keyed by line.</summary>
    private async Task<Dictionary<string, IReadOnlyList<WorkOrderBillShare>>> RequireCodingsAsync(
        ApproveWorkOrderBill command, List<XeroLedgerLineEntity> lines, Dictionary<string, PaidOrder> orders, CancellationToken cancellationToken)
    {
        var codes = orders.Values.SelectMany(order => order.Option.CostCodes).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var activeCodes = await context.CostCenters.AsNoTracking()
            .Where(centre => centre.IsActive && codes.Contains(centre.Code))
            .Select(centre => centre.Code)
            .ToListAsync(cancellationToken);

        var codings = new Dictionary<string, IReadOnlyList<WorkOrderBillShare>>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            var coding = command.Lines.First(candidate => candidate.XeroLedgerLineId.Equals(line.XeroLedgerLineId, StringComparison.OrdinalIgnoreCase));
            var keys = coding.Shares.Select(share => $"{share.WorkOrderId}:{share.CostCenterCode}".ToUpperInvariant()).ToList();
            if (keys.Distinct().Count() != keys.Count)
                throw new InvalidOperationException("Each order's cost code can appear only once on a line.");
            if (coding.Shares.Any(share => share.Net <= 0m))
                throw new InvalidOperationException("Every share must be greater than zero.");
            foreach (var share in coding.Shares)
                RequireCodeOfOrder(share, orders[share.WorkOrderId], activeCodes);
            var total = coding.Shares.Sum(share => share.Net);
            if (total != line.Net)
                throw new InvalidOperationException(
                    $"The shares of \"{line.Description}\" must add up to its net of {line.Net:0.00} — they add up to {total:0.00}.");
            codings[line.XeroLedgerLineId] = coding.Shares;
        }
        return codings;
    }

    private static void RequireCodeOfOrder(WorkOrderBillShare share, PaidOrder order, List<string> activeCodes)
    {
        if (!order.Option.CostCodes.Contains(share.CostCenterCode, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{order.Reference} carries no {share.CostCenterCode} line — a Work Order bill is coded to the order's own cost codes.");
        if (!activeCodes.Contains(share.CostCenterCode, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Not an active cost centre: {share.CostCenterCode}.");
    }

    /// <summary>Each order's slice of the bill must fit inside what is left to invoice on it.</summary>
    private static void RequireEachOrderWithinValue(
        List<XeroLedgerLineEntity> lines, Dictionary<string, IReadOnlyList<WorkOrderBillShare>> codings, Dictionary<string, PaidOrder> orders)
    {
        foreach (var order in orders.Values)
        {
            var slice = SliceNet(lines, codings, order.WorkOrderId);
            if (slice <= order.Option.Remaining) continue;
            throw new InvalidOperationException(
                $"The bill would take {order.Reference} over its value by £{slice - order.Option.Remaining:N2} — "
                + $"£{order.Option.Remaining:N2} of £{order.Option.OrderValue:N2} is left to invoice.");
        }
    }
}

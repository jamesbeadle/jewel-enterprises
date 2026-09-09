using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

public sealed partial class WorkOrderBillRecognition
{
    /// <summary>The match as every line of the bill carries it: the leading order, the bill's
    /// rule, the proposed slice per order, and every open order of the supplier.</summary>
    private static WorkOrderBillMatch MatchFor(BillVerdict verdict)
    {
        var leading = verdict.Slices![0].Order;
        return new WorkOrderBillMatch(
            leading.WorkOrderId, leading.Reference, leading.Title, leading.ProjectId, verdict.Rule, verdict.Detail!,
            verdict.Slices.Select(slice => new WorkOrderBillOrderSlice(slice.Order.WorkOrderId, slice.Net)).ToList(),
            verdict.SupplierOrders.Select(order => order.ToOption()).ToList());
    }

    /// <summary>
    /// An amount shared across an order's cost codes in proportion to the order's own lines
    /// (penny-safe: the shares sum to the amount exactly). A one-code order — or an order whose
    /// lines carry no weight — gives the whole amount to its first code; a code whose share
    /// rounds to nothing is left out rather than posted as a zero.
    /// </summary>
    public static IReadOnlyList<XeroCostSplit> ProposedSplitsFor(
        IReadOnlyList<KeyValuePair<string, decimal>> codeWeights, string projectId, decimal amount)
    {
        if (codeWeights.Count == 0) return Array.Empty<XeroCostSplit>();
        var weights = codeWeights.Select(pair => Math.Max(pair.Value, 0m)).ToList();
        if (codeWeights.Count == 1 || weights.Sum() == 0m)
            return new[] { new XeroCostSplit(codeWeights[0].Key, amount, projectId) };

        var shares = XeroSplitMaths.ProportionalShares(amount, weights);
        return codeWeights
            .Select((pair, index) => new XeroCostSplit(pair.Key, shares[index], projectId))
            .Where(split => split.Net != 0m)
            .ToList();
    }

    /// <summary>The order's code weights, for the approve handler's pro rata coding — by order id, from the supplier's open orders.</summary>
    public IReadOnlyList<KeyValuePair<string, decimal>>? CodeWeightsOf(string workOrderId) =>
        ordersBySubcontractor.Values.SelectMany(orders => orders)
            .FirstOrDefault(order => order.WorkOrderId.Equals(workOrderId, StringComparison.OrdinalIgnoreCase))?.CodeWeights;
}

using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

public sealed partial class WorkOrderBillRecognition
{
    /// <summary>The match as one line carries it: the order's figures plus this line's proposed
    /// shares across the order's cost codes.</summary>
    private static WorkOrderBillMatch MatchFor(OpenOrder order, WorkOrderMatchRule rule, string detail, XeroLedgerLineEntity line) =>
        new(order.WorkOrderId, order.Reference, order.Title, order.ProjectId, rule, detail,
            order.Value, order.InvoicedToDate, ProposedSplitsFor(order, line));

    /// <summary>
    /// The line's net shared across the order's cost codes in proportion to the order's own
    /// lines (penny-safe: the shares sum to the net exactly). A one-code order — or an order
    /// whose lines carry no weight — gives the whole line to its first code; a code whose share
    /// rounds to nothing is left out rather than posted as a zero.
    /// </summary>
    public static IReadOnlyList<XeroCostSplit> ProposedSplitsFor(
        IReadOnlyList<KeyValuePair<string, decimal>> codeWeights, string projectId, decimal lineNet)
    {
        if (codeWeights.Count == 0) return Array.Empty<XeroCostSplit>();
        var weights = codeWeights.Select(pair => Math.Max(pair.Value, 0m)).ToList();
        if (codeWeights.Count == 1 || weights.Sum() == 0m)
            return new[] { new XeroCostSplit(codeWeights[0].Key, lineNet, projectId) };

        var shares = XeroSplitMaths.ProportionalShares(lineNet, weights);
        return codeWeights
            .Select((pair, index) => new XeroCostSplit(pair.Key, shares[index], projectId))
            .Where(split => split.Net != 0m)
            .ToList();
    }

    private static IReadOnlyList<XeroCostSplit> ProposedSplitsFor(OpenOrder order, XeroLedgerLineEntity line) =>
        ProposedSplitsFor(order.CodeWeights, order.ProjectId, line.Net);
}

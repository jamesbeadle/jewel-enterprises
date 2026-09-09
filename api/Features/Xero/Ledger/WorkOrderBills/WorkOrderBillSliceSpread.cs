using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger.WorkOrderBills;

/// <summary>One line's money on one order and one of its cost codes — what the ledger stores.</summary>
public sealed record WorkOrderBillLineShare(string WorkOrderId, string CostCenterCode, decimal Net);

/// <summary>
/// Turns the accountant's figures — a slice of the bill per order — into what the ledger holds
/// per LINE, without touching the lines themselves (2026-09-09: the Xero lines are the
/// supplier's own CIS labour / materials split and stay exactly as raised). Each line's net is
/// shared across the orders in proportion to their slices, penny-safe per line so every line
/// still adds up; the pennies that leaves each order short or over are settled on the largest
/// line, so every order's links add up to its slice exactly too. Each line's portion of an
/// order is then shared across that order's cost codes pro rata to the order's own lines.
/// </summary>
public static class WorkOrderBillSliceSpread
{
    public static Dictionary<string, IReadOnlyList<WorkOrderBillLineShare>> Spread(
        IReadOnlyList<XeroLedgerLineEntity> lines,
        IReadOnlyList<WorkOrderBillOrderSlice> slices,
        Func<string, IReadOnlyList<KeyValuePair<string, decimal>>> codeWeightsOf,
        Func<string, string> projectOf)
    {
        var magnitudes = slices.Select(slice => Math.Abs(slice.Net)).ToList();
        var portions = lines.ToDictionary(
            line => line.XeroLedgerLineId,
            line => XeroSplitMaths.ProportionalShares(line.Net, magnitudes).ToList(),
            StringComparer.OrdinalIgnoreCase);
        SettleOrderDriftOnTheLargestLine(lines, magnitudes, portions);

        var sharesByLine = new Dictionary<string, IReadOnlyList<WorkOrderBillLineShare>>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
            sharesByLine[line.XeroLedgerLineId] = SharesOf(line, slices, portions[line.XeroLedgerLineId], codeWeightsOf, projectOf);
        return sharesByLine;
    }

    /// <summary>Penny-safe per line means an order can be a penny or two off its slice across
    /// the bill; the differences sum to zero, so moving them all onto one line keeps that line
    /// whole and makes every order exact.</summary>
    private static void SettleOrderDriftOnTheLargestLine(
        IReadOnlyList<XeroLedgerLineEntity> lines, List<decimal> magnitudes, Dictionary<string, List<decimal>> portions)
    {
        var largest = lines.OrderByDescending(line => line.Net).First().XeroLedgerLineId;
        for (var index = 0; index < magnitudes.Count; index++)
        {
            var placed = portions.Values.Sum(portion => portion[index]);
            portions[largest][index] += magnitudes[index] - placed;
        }
    }

    private static IReadOnlyList<WorkOrderBillLineShare> SharesOf(
        XeroLedgerLineEntity line,
        IReadOnlyList<WorkOrderBillOrderSlice> slices,
        List<decimal> portions,
        Func<string, IReadOnlyList<KeyValuePair<string, decimal>>> codeWeightsOf,
        Func<string, string> projectOf)
    {
        var shares = new List<WorkOrderBillLineShare>();
        for (var index = 0; index < slices.Count; index++)
        {
            if (portions[index] == 0m) continue;
            var orderId = slices[index].WorkOrderId;
            foreach (var split in WorkOrderBillRecognition.ProposedSplitsFor(codeWeightsOf(orderId), projectOf(orderId), portions[index]))
                shares.Add(new WorkOrderBillLineShare(orderId, split.CostCenterCode, split.Net));
        }
        return shares;
    }
}

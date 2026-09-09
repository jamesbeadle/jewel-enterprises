using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

public sealed partial class WorkOrderBillRecognition
{
    /// <summary>
    /// The per-line rule (2026-09-09, the accountant's ask): when the bill's lines name at least
    /// two different open orders in their descriptions, each named line pays the order it names
    /// and the bill is proposed as that split. A line naming nothing goes with the bill's own
    /// choice (its reference, else the supplier's only order), failing that the first named
    /// order — and the detail says so, since the card is where it gets checked. Null when fewer
    /// than two orders are named, leaving the bill to the whole-bill rules.
    /// </summary>
    private static Assignment? ChooseByLine(List<OpenOrder> orders, IReadOnlyList<XeroLedgerLineEntity> billLines, string? hintedProjectId)
    {
        var named = new Dictionary<string, OpenOrder>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in billLines)
        {
            var numbers = WorkOrderBillReference.NumbersIn(line.Description);
            if (numbers.Count != 1) continue;
            var fit = FitFor(orders, numbers[0], hintedProjectId, out _);
            if (fit is not null) named[line.XeroLedgerLineId] = fit;
        }
        var distinctOrders = named.Values.DistinctBy(order => order.WorkOrderId).ToList();
        if (distinctOrders.Count < 2) return null;

        var unnamed = billLines.Where(line => !named.ContainsKey(line.XeroLedgerLineId)).ToList();
        var fallback = unnamed.Count == 0 ? null : FallbackOrderFor(orders, billLines, hintedProjectId) ?? distinctOrders[0];
        var orderByLineId = new Dictionary<string, OpenOrder>(named, StringComparer.OrdinalIgnoreCase);
        foreach (var line in unnamed) orderByLineId[line.XeroLedgerLineId] = fallback!;

        return new Assignment(orderByLineId, WorkOrderMatchRule.ByLineReference, ByLineDetail(orderByLineId, unnamed, fallback), null);
    }

    private static OpenOrder? FallbackOrderFor(List<OpenOrder> orders, IReadOnlyList<XeroLedgerLineEntity> billLines, string? hintedProjectId)
    {
        var bill = billLines[0];
        var numbers = WorkOrderBillReference.NumbersIn(bill.Reference);
        if (numbers.Count == 1) return FitFor(orders, numbers[0], hintedProjectId, out _);
        return orders.Count == 1 ? orders[0] : null;
    }

    private static string ByLineDetail(IReadOnlyDictionary<string, OpenOrder> orderByLineId, List<XeroLedgerLineEntity> unnamed, OpenOrder? fallback)
    {
        var counts = orderByLineId.Values
            .GroupBy(order => order.WorkOrderId)
            .Select(group => $"{group.First().Reference} ({group.Count()} line{(group.Count() == 1 ? "" : "s")})");
        var detail = $"Each line names its own order — {string.Join(", ", counts)}.";
        if (unnamed.Count == 0) return detail;
        return detail + $" {unnamed.Count} line{(unnamed.Count == 1 ? "" : "s")} name{(unnamed.Count == 1 ? "s" : "")} no order and "
             + $"{(unnamed.Count == 1 ? "is" : "are")} put on {fallback!.Reference} — check before approving.";
    }
}

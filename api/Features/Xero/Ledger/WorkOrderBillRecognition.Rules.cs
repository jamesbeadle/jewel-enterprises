using System.Globalization;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

public sealed partial class WorkOrderBillRecognition
{
    private static readonly CultureInfo Gbp = CultureInfo.GetCultureInfo("en-GB");

    /// <summary>The decision for one bill, rule by rule; null when no order comes into it.</summary>
    private BillVerdict? Evaluate(IReadOnlyList<XeroLedgerLineEntity> billLines, bool isLabour, string? hintedProjectId)
    {
        var bill = billLines[0];
        var orders = OrdersFor(bill.ContactName);
        if (orders is null) return null;
        if (isLabour)
            return Stays("The supplier is on the labour registry — the bill settles through the Labour tab, not a work order.", orders);

        var assignment = ChooseByLine(orders, billLines, hintedProjectId) ?? ChooseForBill(orders, billLines, hintedProjectId);
        if (assignment.OrderByLineId is null) return Stays(assignment.Reason!, orders);

        var slices = assignment.Unplaced
            ? assignment.Pool!.Select(order => (order, 0m)).ToList()
            : SlicesOf(assignment.OrderByLineId, billLines);
        var overValue = assignment.Pool is null ? FirstOrderOverValue(slices) : PoolOverValue(assignment.Pool, billLines);
        if (overValue is not null) return Stays(overValue, orders);

        return new BillVerdict(slices, assignment.Rule, assignment.Detail, null, orders);
    }

    /// <summary>The bill's net as a figure per order: each line's signed net on the order it was assigned to.</summary>
    private static IReadOnlyList<(OpenOrder Order, decimal Net)> SlicesOf(
        IReadOnlyDictionary<string, OpenOrder> orderByLineId, IReadOnlyList<XeroLedgerLineEntity> billLines) =>
        billLines
            .GroupBy(line => orderByLineId[line.XeroLedgerLineId])
            .Select(slice => (slice.Key, slice.Sum(SignedNet)))
            .ToList();

    private static BillVerdict Stays(string reason, IReadOnlyList<OpenOrder> orders) => new(null, default, null, reason, orders);

    private static decimal SignedNet(XeroLedgerLineEntity line) =>
        line.Type == "ACCPAYCREDIT" ? -line.Net : line.Net;

    /// <summary>Each order's slice of the bill must fit inside what is left to invoice on it.</summary>
    private static string? FirstOrderOverValue(IReadOnlyList<(OpenOrder Order, decimal Net)> slices)
    {
        foreach (var (order, sliceNet) in slices)
        {
            if (sliceNet <= order.Remaining) continue;
            return $"The bill would take {order.Reference} over its value by "
                 + $"{(sliceNet - order.Remaining).ToString("C2", Gbp)} — "
                 + $"{order.Remaining.ToString("C2", Gbp)} of {order.Value.ToString("C2", Gbp)} is left to invoice.";
        }
        return null;
    }

    /// <summary>A bill going to the card to be split across several orders must fit inside what they have left between them.</summary>
    private static string? PoolOverValue(IReadOnlyList<OpenOrder> pool, IReadOnlyList<XeroLedgerLineEntity> billLines)
    {
        var billNet = billLines.Sum(SignedNet);
        var remaining = pool.Sum(order => order.Remaining);
        if (billNet <= remaining) return null;
        return $"The bill would take {string.Join(" and ", pool.Select(order => order.Reference))} over their value by "
             + $"{(billNet - remaining).ToString("C2", Gbp)} — {remaining.ToString("C2", Gbp)} is left to invoice between them.";
    }

    /// <summary>The bill as a whole names one order (or none): every line pays the same order.</summary>
    private static Assignment ChooseForBill(List<OpenOrder> orders, IReadOnlyList<XeroLedgerLineEntity> billLines, string? hintedProjectId)
    {
        var bill = billLines[0];
        var numbers = WorkOrderBillReference.NumbersOn(bill.Reference, billLines.Select(line => line.Description), bill.InvoiceNumber);
        if (numbers.Count > 1) return ChooseFirstOfSeveral(orders, numbers, hintedProjectId, billLines);
        if (numbers.Count == 0) return ChooseBySupplier(orders, hintedProjectId, billLines);
        var chosen = ChooseByReference(orders, numbers[0], hintedProjectId);
        if (chosen.Order is null) return Assignment.Refused(chosen.Reason!);
        return new Assignment(EveryLineOn(chosen.Order, billLines), chosen.Rule, chosen.Detail, null);
    }

    private static IReadOnlyDictionary<string, OpenOrder> EveryLineOn(OpenOrder order, IReadOnlyList<XeroLedgerLineEntity> billLines) =>
        billLines.ToDictionary(line => line.XeroLedgerLineId, _ => order, StringComparer.OrdinalIgnoreCase);

    /// <summary>A number on the bill names the order — supplier + number, since numbers are per
    /// project; a number that fits orders on two projects falls back to the bill's site — the
    /// project set on it in the portal, else its Xero Sites tracking.</summary>
    private static (OpenOrder? Order, WorkOrderMatchRule Rule, string? Detail, string? Reason) ChooseByReference(
        List<OpenOrder> orders, int number, string? hintedProjectId)
    {
        var fit = FitFor(orders, number, hintedProjectId, out var reason);
        if (fit is null) return (null, default, null, reason);
        return (fit, WorkOrderMatchRule.ByReference, $"Matched by the reference {fit.Reference} on the bill.", null);
    }

    /// <summary>The bill names several of the supplier's open orders: it reaches the card on the
    /// first one named, for its lines to be split across them by hand (2026-09-09).</summary>
    private static Assignment ChooseFirstOfSeveral(
        List<OpenOrder> orders, IReadOnlyList<int> numbers, string? hintedProjectId, IReadOnlyList<XeroLedgerLineEntity> billLines)
    {
        var fits = new List<OpenOrder>();
        foreach (var number in numbers)
        {
            var fit = FitFor(orders, number, hintedProjectId, out var reason);
            if (fit is null) return Assignment.Refused(reason!);
            fits.Add(fit);
        }
        var named = string.Join(" and ", fits.Select(fit => fit.Reference));
        return new Assignment(EveryLineOn(fits[0], billLines), WorkOrderMatchRule.ByReference,
            $"The bill names {named} — split its lines across them on the card.", null, fits);
    }

    /// <summary>The one open order a number means for this supplier, or null with the reason.</summary>
    private static OpenOrder? FitFor(List<OpenOrder> orders, int number, string? hintedProjectId, out string? reason)
    {
        reason = null;
        var fits = orders.Where(order => order.Number == number).ToList();
        if (fits.Count == 0)
        {
            reason = $"The bill references WO-{number:0000}, but the supplier has no open order with that number.";
            return null;
        }
        if (fits.Count > 1 && hintedProjectId is not null)
            fits = fits.Where(order => order.ProjectId.Equals(hintedProjectId, StringComparison.OrdinalIgnoreCase)).ToList();
        if (fits.Count == 1) return fits[0];
        reason = $"WO-{number:0000} is open for this supplier on more than one project "
               + $"({string.Join(", ", orders.Where(order => order.Number == number).Select(order => order.ProjectName))}) "
               + "and the bill carries no site — set the project on the bill and re-check.";
        return null;
    }

    /// <summary>No number on the bill: a supplier with exactly one open order matches on that
    /// alone — on the project the bill's site names when it names one, else anywhere. A supplier
    /// with several (2026-09-10, the accountant's ask — the Sussex Tiling bill against two By
    /// France orders): the bill still reaches the card, every open order listed with NO figure
    /// proposed, for the accountant to key the split — the same card a referenced bill gets, so
    /// the work-order approval is never lost to the plain queue for want of a number on the bill.
    /// Only a site the supplier has no order on refuses the bill.</summary>
    private static Assignment ChooseBySupplier(List<OpenOrder> orders, string? hintedProjectId, IReadOnlyList<XeroLedgerLineEntity> billLines)
    {
        var onSite = hintedProjectId is null
            ? orders
            : orders.Where(order => order.ProjectId.Equals(hintedProjectId, StringComparison.OrdinalIgnoreCase)).ToList();
        if (onSite.Count == 1)
            return new Assignment(EveryLineOn(onSite[0], billLines), WorkOrderMatchRule.BySupplier,
                $"Matched by supplier — {onSite[0].Reference} {onSite[0].Title} is their only open order"
                + (hintedProjectId is null ? "." : $" on {onSite[0].ProjectName}, the site on the bill."), null);
        if (onSite.Count > 1)
        {
            var listed = string.Join(", ", onSite.Select(order => order.Reference));
            return new Assignment(EveryLineOn(onSite[0], billLines), WorkOrderMatchRule.BySupplierOrders,
                $"The supplier has {onSite.Count} open orders ({listed})"
                + (hintedProjectId is null ? "" : $" on {onSite[0].ProjectName}, the site on the bill,")
                + " and the bill names none — set the figure on each order it pays.",
                null, onSite, Unplaced: true);
        }
        if (orders.Count == 1)
            return Assignment.Refused($"The supplier's only open order, {orders[0].Reference} on {orders[0].ProjectName}, "
                                      + "is not on the site the bill names.");
        return Assignment.Refused($"The supplier has {orders.Count} open work orders "
                                  + $"({string.Join(", ", orders.Select(order => $"{order.Reference} {order.ProjectName}"))}), "
                                  + "none on the site the bill names — set the project on the bill and re-check.");
    }
}

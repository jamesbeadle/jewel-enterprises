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
            return Stays("The supplier is on the labour registry — the bill settles through the Labour tab, not a work order.");

        var numbers = WorkOrderBillReference.NumbersOn(
            bill.Reference, billLines.Select(line => line.Description), bill.InvoiceNumber);
        var chosen = numbers.Count > 0
            ? ChooseByReference(orders, numbers, hintedProjectId)
            : ChooseBySupplier(orders, hintedProjectId);
        if (chosen.Order is null) return Stays(chosen.Reason!);

        var billNet = billLines.Sum(SignedNet);
        if (billNet > chosen.Order.Remaining)
            return Stays($"The bill would take {chosen.Order.Reference} over its value by "
                         + $"{(billNet - chosen.Order.Remaining).ToString("C2", Gbp)} — "
                         + $"{chosen.Order.Remaining.ToString("C2", Gbp)} of {chosen.Order.Value.ToString("C2", Gbp)} is left to invoice.");

        return new BillVerdict(chosen.Order, chosen.Rule, chosen.Detail, null);
    }

    private static BillVerdict Stays(string reason) => new(null, default, null, reason);

    private static decimal SignedNet(XeroLedgerLineEntity line) =>
        line.Type == "ACCPAYCREDIT" ? -line.Net : line.Net;

    /// <summary>A number on the bill names the order — supplier + number, since numbers are per
    /// project; a number that fits orders on two projects falls back to the bill's site — the
    /// project set on it in the portal, else its Xero Sites tracking.</summary>
    private static (OpenOrder? Order, WorkOrderMatchRule Rule, string? Detail, string? Reason) ChooseByReference(
        List<OpenOrder> orders, IReadOnlyList<int> numbers, string? hintedProjectId)
    {
        if (numbers.Count > 1)
            return (null, default, null, $"The bill names more than one work order ({string.Join(", ", numbers.Select(number => $"WO-{number:0000}"))}) — link it by hand.");

        var number = numbers[0];
        var fits = orders.Where(order => order.Number == number).ToList();
        if (fits.Count == 0)
            return (null, default, null, $"The bill references WO-{number:0000}, but the supplier has no open order with that number.");
        if (fits.Count > 1 && hintedProjectId is not null)
            fits = fits.Where(order => order.ProjectId.Equals(hintedProjectId, StringComparison.OrdinalIgnoreCase)).ToList();
        if (fits.Count != 1)
            return (null, default, null, $"WO-{number:0000} is open for this supplier on more than one project "
                                         + $"({string.Join(", ", orders.Where(order => order.Number == number).Select(order => order.ProjectName))}) "
                                         + "and the bill carries no site — set the project on the bill and re-check.");
        return (fits[0], WorkOrderMatchRule.ByReference, $"Matched by the reference {fits[0].Reference} on the bill.", null);
    }

    /// <summary>No number on the bill: only a supplier with exactly one open order matches — on
    /// the project the bill's own Sites tracking names when it names one, else anywhere.</summary>
    private static (OpenOrder? Order, WorkOrderMatchRule Rule, string? Detail, string? Reason) ChooseBySupplier(
        List<OpenOrder> orders, string? hintedProjectId)
    {
        var onSite = hintedProjectId is null
            ? orders
            : orders.Where(order => order.ProjectId.Equals(hintedProjectId, StringComparison.OrdinalIgnoreCase)).ToList();
        if (onSite.Count == 1)
            return (onSite[0], WorkOrderMatchRule.BySupplier,
                $"Matched by supplier — {onSite[0].Reference} {onSite[0].Title} is their only open order"
                + (hintedProjectId is null ? "." : $" on {onSite[0].ProjectName}, the site on the bill."), null);
        if (orders.Count == 1)
            return (null, default, null, $"The supplier's only open order, {orders[0].Reference} on {orders[0].ProjectName}, "
                                         + "is not on the site the bill names.");
        return (null, default, null, $"The supplier has {orders.Count} open work orders "
                                     + $"({string.Join(", ", orders.Select(order => $"{order.Reference} {order.ProjectName}"))}) "
                                     + "and the bill carries no WO reference.");
    }
}

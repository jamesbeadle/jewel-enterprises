using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero.Ledger.WorkOrderBills;
using Jewel.JPMS.Contracts.Xero;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The accountant's figures per order, spread over the supplier's own lines (2026-09-09): every
/// line still adds up to its net, every order gets exactly its figure, the pennies land on the
/// largest line, and each line's portion of an order follows the order's cost codes.
/// </summary>
public sealed class WorkOrderBillSliceSpreadTests
{
    private static readonly IReadOnlyList<KeyValuePair<string, decimal>> OneCode = new[] { new KeyValuePair<string, decimal>("INT-PLS", 1m) };

    [Fact]
    public void LeesGreen_TwoOrdersOverTheCisSplit_EveryLineAndEveryOrderExact()
    {
        var lines = new[] { Line("a", 720m), Line("b", 2372m) };
        var slices = new[] { new WorkOrderBillOrderSlice("wo-55", 1748m), new WorkOrderBillOrderSlice("wo-56", 1344m) };

        var spread = WorkOrderBillSliceSpread.Spread(lines, slices, _ => OneCode, _ => "P");

        foreach (var line in lines)
            Assert.Equal(line.Net, spread[line.XeroLedgerLineId].Sum(share => share.Net));
        Assert.Equal(1748m, spread.Values.SelectMany(shares => shares).Where(share => share.WorkOrderId == "wo-55").Sum(share => share.Net));
        Assert.Equal(1344m, spread.Values.SelectMany(shares => shares).Where(share => share.WorkOrderId == "wo-56").Sum(share => share.Net));
    }

    [Fact]
    public void PenniesThatLeaveAnOrderShortAreSettledOnTheLargestLine()
    {
        var lines = new[] { Line("a", 1m), Line("b", 1m), Line("c", 1m) };
        var slices = new[] { new WorkOrderBillOrderSlice("wo-1", 2m), new WorkOrderBillOrderSlice("wo-2", 1m) };

        var spread = WorkOrderBillSliceSpread.Spread(lines, slices, _ => OneCode, _ => "P");

        foreach (var line in lines)
            Assert.Equal(1m, spread[line.XeroLedgerLineId].Sum(share => share.Net));
        Assert.Equal(2m, spread.Values.SelectMany(shares => shares).Where(share => share.WorkOrderId == "wo-1").Sum(share => share.Net));
        Assert.Equal(1m, spread.Values.SelectMany(shares => shares).Where(share => share.WorkOrderId == "wo-2").Sum(share => share.Net));
    }

    [Fact]
    public void ALinesPortionOfAnOrderFollowsTheOrdersCostCodes()
    {
        var twoCodes = new[] { new KeyValuePair<string, decimal>("INT-PLS", 6000m), new KeyValuePair<string, decimal>("INT-PLB", 4000m) };
        var lines = new[] { Line("a", 1000m) };
        var slices = new[] { new WorkOrderBillOrderSlice("wo-1", 1000m) };

        var spread = WorkOrderBillSliceSpread.Spread(lines, slices, _ => twoCodes, _ => "P");

        Assert.Equal(new[] { ("INT-PLS", 600m), ("INT-PLB", 400m) }, spread["a"].Select(share => (share.CostCenterCode, share.Net)));
    }

    private static XeroLedgerLineEntity Line(string id, decimal net) =>
        new() { XeroLedgerLineId = id, XeroInvoiceId = "inv", XeroLineItemId = id, Type = "ACCPAY", Net = net };
}

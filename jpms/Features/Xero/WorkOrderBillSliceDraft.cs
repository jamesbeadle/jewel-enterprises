namespace Jewel.JPMS.Features.Xero;

/// <summary>One open order's figure on a Work Order bill card: this much of the bill's net on
/// this order, blank when the order takes none of it. Page-owned, edited in place by the card's
/// order table so Approve reads the same list.</summary>
public sealed class WorkOrderBillSliceDraft
{
    public string WorkOrderId { get; set; } = "";
    public decimal? Amount { get; set; }
}

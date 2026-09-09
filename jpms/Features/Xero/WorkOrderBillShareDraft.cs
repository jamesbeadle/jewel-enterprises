namespace Jewel.JPMS.Features.Xero;

/// <summary>One editable share of a Work Order bill line on the allocation page: this much of
/// the line's net on this order, on one of that order's cost codes. Page-owned, edited in place
/// by the card's share editor so Approve reads the same list.</summary>
public sealed class WorkOrderBillShareDraft
{
    public string WorkOrderId { get; set; } = "";
    public string Code { get; set; } = "";
    public decimal? Amount { get; set; }
}

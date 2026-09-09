using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger.WorkOrderBills;

/// <summary>The shape check: ids present, at least one slice, each on an order named once.</summary>
public sealed class ApproveWorkOrderBillValidation
{
    public ValidationOutcome Check(ApproveWorkOrderBill command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.XeroInvoiceId)) errors.Add("XeroInvoiceId is required.");
        if (command.Slices is null || command.Slices.Count == 0) errors.Add("At least one order slice is required.");
        var slices = command.Slices ?? Array.Empty<WorkOrderBillOrderSlice>();
        if (slices.Any(slice => string.IsNullOrWhiteSpace(slice.WorkOrderId))) errors.Add("Every slice needs a work order.");
        if (slices.Any(slice => slice.Net == 0m)) errors.Add("Every slice must be an amount — leave an order out rather than giving it nothing.");
        var orderIds = slices.Select(slice => slice.WorkOrderId).ToList();
        if (orderIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != orderIds.Count)
            errors.Add("An order can only be given one slice of the bill.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger.WorkOrderBills;

/// <summary>The shape check: ids present, one coding per line, every share a positive amount on a code.</summary>
public sealed class ApproveWorkOrderBillValidation
{
    public ValidationOutcome Check(ApproveWorkOrderBill command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.XeroInvoiceId)) errors.Add("XeroInvoiceId is required.");
        if (string.IsNullOrWhiteSpace(command.WorkOrderId)) errors.Add("WorkOrderId is required.");
        if (command.Lines is null || command.Lines.Count == 0) errors.Add("At least one line coding is required.");
        foreach (var line in command.Lines ?? Array.Empty<WorkOrderBillLineCoding>())
        {
            if (string.IsNullOrWhiteSpace(line.XeroLedgerLineId)) errors.Add("Every line coding needs a ledger line id.");
            if (line.Splits is null || line.Splits.Count == 0) errors.Add("Every line needs at least one cost code share.");
            if (line.Splits?.Any(split => string.IsNullOrWhiteSpace(split.CostCenterCode)) == true)
                errors.Add("Every share needs a cost code.");
        }
        var lineIds = (command.Lines ?? Array.Empty<WorkOrderBillLineCoding>()).Select(line => line.XeroLedgerLineId).ToList();
        if (lineIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != lineIds.Count)
            errors.Add("A line can only be coded once.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

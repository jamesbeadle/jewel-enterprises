using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger.WorkOrderBills;

public sealed class UndoWorkOrderBillApprovalValidation
{
    public ValidationOutcome Check(UndoWorkOrderBillApproval command) =>
        string.IsNullOrWhiteSpace(command.XeroInvoiceId)
            ? ValidationOutcome.Failed("XeroInvoiceId is required.")
            : ValidationOutcome.Passed;
}

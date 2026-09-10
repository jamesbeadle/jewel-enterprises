using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

public sealed class RecordValuationInvoiceXeroNumberValidation
{
    public ValidationOutcome Check(RecordValuationInvoiceXeroNumber command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ValuationInvoiceId)) errors.Add("ValuationInvoiceId is required.");
        if (string.IsNullOrWhiteSpace(command.XeroInvoiceNumber)) errors.Add("XeroInvoiceNumber is required — the number Xero gave the invoice (e.g. INV-0227).");
        else if (command.XeroInvoiceNumber.Trim().Length > 64) errors.Add("XeroInvoiceNumber must be 64 characters or fewer.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

public sealed class IssueValuationInvoiceValidation
{
    public ValidationOutcome Check(IssueValuationInvoice command)
    {
        if (string.IsNullOrWhiteSpace(command.ValuationInvoiceId))
            return new ValidationOutcome(new[] { "ValuationInvoiceId is required." });
        if (command.XeroInvoiceNumber is { } number && number.Trim().Length > 64)
            return new ValidationOutcome(new[] { "XeroInvoiceNumber must be 64 characters or fewer." });
        return ValidationOutcome.Passed;
    }
}

using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.XeroRaise;

/// <summary>Raising in Xero is the issue step, so the same people who may issue may raise.</summary>
public sealed class RaiseValuationInvoiceInXeroAuthorisation
{
    public bool Allows(SignedInUser user, RaiseValuationInvoiceInXero command) =>
        ValuationInvoiceRoles.AllowedToManageValuationInvoices.IncludesAny(user.Roles);
}

public sealed class RaiseValuationInvoiceInXeroValidation
{
    public ValidationOutcome Check(RaiseValuationInvoiceInXero command) =>
        string.IsNullOrWhiteSpace(command.ValuationInvoiceId)
            ? ValidationOutcome.Failed("ValuationInvoiceId is required.")
            : ValidationOutcome.Passed;
}

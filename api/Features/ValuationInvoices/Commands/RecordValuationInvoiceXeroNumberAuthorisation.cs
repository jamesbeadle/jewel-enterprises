using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>Recording a hand-raised Xero number is part of issuing, so the same people who may issue may record it.</summary>
public sealed class RecordValuationInvoiceXeroNumberAuthorisation
{
    public bool Allows(SignedInUser user, RecordValuationInvoiceXeroNumber command) =>
        ValuationInvoiceRoles.AllowedToManageValuationInvoices.IncludesAny(user.Roles);
}

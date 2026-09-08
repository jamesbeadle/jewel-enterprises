using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger.WorkOrderBills;

public sealed class UndoWorkOrderBillApprovalAuthorisation
{
    public bool Allows(SignedInUser user, UndoWorkOrderBillApproval command) =>
        WorkOrderBillRoles.AllowedToApprove.IncludesAny(user.Roles);
}

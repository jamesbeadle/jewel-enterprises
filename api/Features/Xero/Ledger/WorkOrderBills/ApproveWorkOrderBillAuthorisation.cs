using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger.WorkOrderBills;

public sealed class ApproveWorkOrderBillAuthorisation
{
    public bool Allows(SignedInUser user, ApproveWorkOrderBill command) =>
        WorkOrderBillRoles.AllowedToApprove.IncludesAny(user.Roles);
}

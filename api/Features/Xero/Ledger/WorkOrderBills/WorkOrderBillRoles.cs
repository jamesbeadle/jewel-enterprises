namespace Jewel.JPMS.Api.Features.Xero.Ledger.WorkOrderBills;

/// <summary>
/// Who may approve a Work Order bill — and undo one. Approve is the FD's button: it commits the
/// bill to the order, writes the tracking and makes the bill payable in Xero in one press, so
/// the audience is the finance director and the directors (admins pass because Role.Admin is
/// included explicitly), not the whole allocation queue.
/// </summary>
internal static class WorkOrderBillRoles
{
    public static readonly RoleSet AllowedToApprove = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector);
}

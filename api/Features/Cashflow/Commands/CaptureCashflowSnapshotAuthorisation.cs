using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cashflow;

namespace Jewel.JPMS.Api.Features.Cashflow.Commands;

public sealed class CaptureCashflowSnapshotAuthorisation
{
    private static readonly RoleSet RolesThatMayCaptureCashflow =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector);

    public bool Allows(SignedInUser user, CaptureCashflowSnapshot command) =>
        RolesThatMayCaptureCashflow.IncludesAny(user.Roles);
}

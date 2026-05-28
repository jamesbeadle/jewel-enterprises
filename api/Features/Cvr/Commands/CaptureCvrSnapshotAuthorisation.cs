using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cvr;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class CaptureCvrSnapshotAuthorisation
{
    private static readonly RoleSet RolesThatMayCaptureSnapshots =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, CaptureCvrSnapshot command) =>
        RolesThatMayCaptureSnapshots.IncludesAny(user.Roles);
}

using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cvr;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class RecordCvrPackageRowAuthorisation
{
    private static readonly RoleSet RolesThatMayRecordPackageRows =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, RecordCvrPackageRow command) =>
        RolesThatMayRecordPackageRows.IncludesAny(user.Roles);
}

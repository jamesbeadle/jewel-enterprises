using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class RecordCisVerificationAuthorisation
{
    // The verification result is a field of the directory record, so the gate is
    // UpdateSubcontractorAuthorisation's: Admin (implicit), the managing and finance directors,
    // and project managers.
    private static readonly RoleSet RolesThatMayRecordCisVerification =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, RecordCisVerification command) => RolesThatMayRecordCisVerification.IncludesAny(user.Roles);
}

using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

// Recording or removing a variation's push is an edit of the programme, so the roles are the
// programme-editing roles (AddProgrammeTaskAuthorisation), shared across the pair.
public sealed class ProgrammeVariationEffectAuthorisation
{
    private static readonly RoleSet RolesThatMayEditProgramme = RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    private static bool Allowed(SignedInUser user) => RolesThatMayEditProgramme.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, RecordProgrammeVariationEffect command) => Allowed(user);
    public bool Allows(SignedInUser user, RemoveProgrammeVariationEffect command) => Allowed(user);
}

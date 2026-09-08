using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

// Every draft-programme command is an edit of the programme in waiting — opening one, mapping
// its lines, applying or discarding it — so the roles are the programme-editing roles
// (AddProgrammeTaskAuthorisation), shared across the five like the valuation-invoice workflow.
public sealed class ProgrammeDraftAuthorisation
{
    private static readonly RoleSet RolesThatMayEditProgramme = RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    private static bool Allowed(SignedInUser user) => RolesThatMayEditProgramme.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, DraftProgrammeFromValuation command) => Allowed(user);
    public bool Allows(SignedInUser user, SuggestProgrammeDraftMappings command) => Allowed(user);
    public bool Allows(SignedInUser user, ReviewProgrammeDraftLine command) => Allowed(user);
    public bool Allows(SignedInUser user, ApplyProgrammeDraft command) => Allowed(user);
    public bool Allows(SignedInUser user, DiscardProgrammeDraft command) => Allowed(user);
}

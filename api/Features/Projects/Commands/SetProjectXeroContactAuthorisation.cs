using Jewel.JPMS.Contracts.Projects;

namespace Jewel.JPMS.Api.Features.Projects.Commands;

public sealed class SetProjectXeroContactAuthorisation
{
    // Same gate as UpdateProjectDetails — the Xero contact is part of a project's details.
    private static readonly RoleSet RolesThatMayUpdateProjects =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, SetProjectXeroContact command) =>
        RolesThatMayUpdateProjects.IncludesAny(user.Roles);
}

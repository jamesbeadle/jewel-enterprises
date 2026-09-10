using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

/// <summary>Who may correct the expiry and public liability figure on a compliance document:
/// the same circle that may file one onto any record from the office side
/// (UploadComplianceDocumentFileAuthorisation) — the facts belong to the document, so whoever may
/// file it may correct it. Portal-scoped subcontractor logins are not in it: their own record's
/// facts are corrected by the office, never self-declared after the fact.</summary>
public sealed class SetComplianceDocumentDetailsAuthorisation
{
    private static readonly RoleSet AllowedToEdit = RoleSet.Of(
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator,
        JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing);

    public bool Allows(SignedInUser user, SetComplianceDocumentDetails command) => AllowedToEdit.IncludesAny(user.Roles);
}

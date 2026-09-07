using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Same people who may save a submission from an email (SaveExtractedQuoteAuthorisation): judging
// an email "not a tender" is the other half of that same review.
public sealed class SetBidPackageEmailDispositionAuthorisation
{
    private static readonly RoleSet RolesThatMayJudge =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing);

    public bool Allows(SignedInUser user, SetBidPackageEmailDisposition command) => RolesThatMayJudge.IncludesAny(user.Roles);
}

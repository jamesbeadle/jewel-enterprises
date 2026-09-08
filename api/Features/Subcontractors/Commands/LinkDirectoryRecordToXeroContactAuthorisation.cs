using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class LinkDirectoryRecordToXeroContactAuthorisation
{
    // A Xero link decides which supplier a company's bills reconcile against, so it is held to the
    // same gate as importing from Xero (Admin passes implicitly — admins pass every gate).
    public bool Allows(SignedInUser user, LinkDirectoryRecordToXeroContact command) =>
        DirectoryXeroLinkRoles.MayLink.IncludesAny(user.Roles);
}

public sealed class UnlinkDirectoryRecordFromXeroContactAuthorisation
{
    public bool Allows(SignedInUser user, UnlinkDirectoryRecordFromXeroContact command) =>
        DirectoryXeroLinkRoles.MayLink.IncludesAny(user.Roles);
}

internal static class DirectoryXeroLinkRoles
{
    // Mirrors ImportXeroSupplierAuthorisation — the two are the same decision made two ways.
    public static readonly RoleSet MayLink =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing);
}

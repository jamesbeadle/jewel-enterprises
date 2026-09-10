using Jewel.JPMS.Api.Features.Architects;
using Jewel.JPMS.Api.Features.Architects.Commands;
using Jewel.JPMS.Api.Features.Clients;
using Jewel.JPMS.Api.Features.Clients.Commands;
using Jewel.JPMS.Api.Features.Directory.Commands;
using Jewel.JPMS.Api.Features.Parties;
using Jewel.JPMS.Api.Features.Subcontractors.Commands;
using Jewel.JPMS.Contracts.Architects;
using Jewel.JPMS.Contracts.Clients;
using Jewel.JPMS.Contracts.Directory;
using Jewel.JPMS.Contracts.Parties;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>Subcontractor directory, party-contact and user-directory commands as connector
/// actions. Mirrors Features/Subcontractors, Features/Parties,
/// Features/Architects, Features/Clients and Features/Directory — each entry's VisibleTo copies
/// its Authorisation class's role set (replicated inline where the endpoint keeps the set in a
/// private field), and the stamps copy exactly what the endpoint stamps server-side.</summary>
internal sealed partial class SubcontractorsAndLeadsActions : IAiActionSource
{
    // Replicas of role sets held as PRIVATE fields inside the authorisation classes they mirror —
    // kept identical to the source lists named in each entry's AuthorisationType.
    private static readonly RoleSet DirectoryCurators =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing);

    private static readonly RoleSet DirectoryRecordEditors =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    // SetComplianceDocumentDetailsAuthorisation — the office circle that files compliance documents
    // (UploadComplianceDocumentFileAuthorisation).
    private static readonly RoleSet ComplianceDocumentEditors =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator,
            JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing);

    private static readonly RoleSet PartyContactManagers =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager);

    // AdminGate.Allows: Role.Admin or Role.FinanceDirector.
    private static readonly RoleSet UserAdministrators =
        RoleSet.Of(Role.Admin, JpmsRoles.FinanceDirector);

    public IEnumerable<AiAction> Build() =>
        SubcontractorsActions()
            // The Leads/CRM actions of the May prototype left with the Sales rebuild (2026-09-06)
            // — see SalesActions.
            .Concat(ContactsActions())
            .Concat(DirectoryUsersActions());

    // Skipped: InviteSubcontractorPortalUser — no command dispatch: the endpoint calls the
    //          SubcontractorPortalInviter service directly instead of an ICommandHandler.
    // Skipped: PrepareSubcontractorStatementEmailDraft — no HTTP endpoint dispatches it: the
    //          handler/authorisation/validation are registered but no [HttpTrigger] function
    //          exists for the client's /statement/draft-email route.
    // Skipped: AddComplianceDocumentVersion — constructed server-side by the multipart upload
    //          endpoints after the blob is stored; never sent by clients and has no endpoint of
    //          its own.
}

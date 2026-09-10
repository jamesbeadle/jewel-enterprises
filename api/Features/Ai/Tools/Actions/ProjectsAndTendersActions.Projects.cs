using Jewel.JPMS.Api.Features.ArchitectInstructions;
using Jewel.JPMS.Api.Features.BuildingControl;
using Jewel.JPMS.Api.Features.BuildingControl.Attachments;
using Jewel.JPMS.Api.Features.BuildingControl.Commands;
using Jewel.JPMS.Api.Features.Mobilisation.Commands;
using Jewel.JPMS.Api.Features.ProjectContracts;
using Jewel.JPMS.Api.Features.ProjectContracts.Commands;
using Jewel.JPMS.Api.Features.Projects.Commands;
using Jewel.JPMS.Api.Features.Projects.Contacts;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Contracts.ArchitectInstructions;
using Jewel.JPMS.Contracts.BuildingControl;
using Jewel.JPMS.Contracts.Mobilisation;
using Jewel.JPMS.Contracts.ProjectContracts;
using Jewel.JPMS.Contracts.Projects;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class ProjectsAndTendersActions
{
    private static IEnumerable<AiAction> ProjectsActions() => new AiAction[]
    {
        new AiAction(
            Name: "create_project_shell",
            Area: "Projects",
            Description: "Creates a new project with its reference, name, client, organisation, "
                + "project manager and stage — it appears across the portal immediately.",
            CommandType: typeof(CreateProjectShell),
            ResultType: typeof(Project),
            AuthorisationType: typeof(CreateProjectShellAuthorisation),
            ValidationType: typeof(CreateProjectShellValidation),
            VisibleTo: ProjectCreators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectManagerEmail is a portal user's email. Confirm the reference with the "
                + "user — it is how the whole team will know the job."),

        new AiAction(
            Name: "update_project_details",
            Area: "Projects",
            Description: "Overwrites a project's details wholesale — reference, name, client, "
                + "organisation, stage, project manager, correspondent party, site address, Xero "
                + "site name and the Xero contact mapping (xeroContactId + xeroContactName: the Xero "
                + "customer the project's sales invoices are raised on). Fields omitted are not kept: "
                + "read the project first and carry forward everything that should not change. The "
                + "party assignment decides where project emails (RFIs and other request documents) "
                + "are addressed.",
            CommandType: typeof(UpdateProjectDetails),
            ResultType: typeof(Project),
            AuthorisationType: typeof(UpdateProjectDetailsAuthorisation),
            ValidationType: typeof(UpdateProjectDetailsValidation),
            VisibleTo: ProjectEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. Echo current values for anything unchanged — "
                + "a null partyId clears the party assignment, and a null xeroContactId clears the Xero "
                + "contact mapping (raise_valuation_invoice_in_xero is blocked until it is set). "
                + "xeroContactId is Xero's ContactID and xeroContactName the name as Xero holds it, "
                + "from the Xero contacts list in Project settings — never guess an id; set them only "
                + "when the user has named the contact and said yes."),

        new AiAction(
            Name: "delete_project",
            Area: "Projects",
            Description: "PERMANENTLY DELETES a project and every record filed under it — requests, "
                + "variations, valuations, programme, drawings register, financial records, the lot — "
                + "in one transaction. There is no undo. Xero ledger lines are not deleted; their "
                + "allocation to the project is cleared, returning them to the unallocated queue. The "
                + "audit trail survives, gaining a ProjectDeleted event.",
            CommandType: typeof(DeleteProject),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteProjectAuthorisation),
            ValidationType: typeof(DeleteProjectValidation),
            VisibleTo: ProjectDeleters,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Irreversible. Confirm with the user, naming the project, before calling. "
                + "confirmName must match the project's name exactly — the server re-checks it and "
                + "refuses a mismatch."),

        new AiAction(
            Name: "set_next_valuation_date",
            Area: "Projects",
            Description: "Sets (or clears, with null) the date the next valuation is expected on a "
                + "project — the one field, without round-tripping the full project details.",
            CommandType: typeof(SetNextValuationDate),
            ResultType: typeof(Project),
            AuthorisationType: typeof(SetNextValuationDateAuthorisation),
            ValidationType: null,
            VisibleTo: ProjectEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. The date is ISO 8601."),

        new AiAction(
            Name: "set_expected_monthly_valuation",
            Area: "Projects",
            Description: "Sets (or clears, with null) the forecast assumption of roughly how much the "
                + "architect is expected to certify per valuation month on a project. Forecasting "
                + "only — the Cash Forecast claims left-to-claim at this rate; it never touches "
                + "valuations or invoices.",
            CommandType: typeof(SetExpectedMonthlyValuation),
            ResultType: typeof(Project),
            AuthorisationType: typeof(SetExpectedMonthlyValuationAuthorisation),
            ValidationType: null,
            VisibleTo: ProjectEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "expectedMonthlyValuation must be a positive amount, or null to clear — zero or "
                + "negative is not a rate."),

        new AiAction(
            Name: "upsert_project_contact",
            Area: "Projects",
            Description: "Adds or updates a person on a project's correspondence profile, including "
                + "how they join issued request documents (To/Cc/Bcc/None) — this decides who receives "
                + "the project's outbound request correspondence. A null/blank contactId inserts; a "
                + "populated one updates in place.",
            CommandType: typeof(UpsertProjectContact),
            ResultType: typeof(ProjectContact),
            AuthorisationType: typeof(ProjectContactAuthorisation),
            ValidationType: typeof(UpsertProjectContactValidation),
            VisibleTo: ProjectContactManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "contactId for an update comes from the project's contact list "
                + "(ListProjectContacts). partyContactId, when set, links the row to a person on the "
                + "party's contact book as a per-project routing override."),

        new AiAction(
            Name: "remove_project_contact",
            Area: "Projects",
            Description: "Deletes a person from a project's correspondence profile permanently — they "
                + "stop receiving the project's outbound request correspondence. There is no undo.",
            CommandType: typeof(RemoveProjectContact),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(ProjectContactAuthorisation),
            ValidationType: null,
            VisibleTo: ProjectContactManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which contact, by name and project, before calling. "
                + "contactId comes from the project's contact list (ListProjectContacts)."),

        // ── Project contracts ─────────────────────────────────────────────────────────────────

    };
}

using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.Requests.Commands;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class RequestsActions
{
    private static IEnumerable<AiAction> RequestActions() => new AiAction[]
    {
        new AiAction(
            Name: "raise_request",
            Area: "Requests & RFIs",
            Description: "Creates a new request (RFI, RFA, RFC, RFQ, RFP, NOD, EOT or General) on a "
                + "project's register immediately. Nothing is emailed — issuing the official document "
                + "is a separate, explicit step. Fails if the reference is already in use on the project.",
            CommandType: typeof(RaiseRequest),
            ResultType: typeof(Request),
            AuthorisationType: typeof(RaiseRequestAuthorisation),
            ValidationType: typeof(RaiseRequestValidation),
            VisibleTo: RoleSet.Of(
                JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager,
                JpmsRoles.Architect, JpmsRoles.Subcontractor),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. raisedByEmail should be the signed-in user's "
                + "portal email unless the user says the request was raised by someone else. Kind: "
                + "Rfi, Rfa, Rfc, NoticeOfDelay, Rfq, Rfp, ExtensionOfTime or General. Leave the "
                + "backfill fields (raisedAt, respondedAt, responseText, respondedByEmail, status) "
                + "null unless logging a historical record. For an ExtensionOfTime, eotDaysClaimed "
                + "(and eotDaysGranted once the architect has answered) are what the Programme tab "
                + "draws the extension from; relatedNodRequestId ties it to its Notice of Delay."),

        new AiAction(
            Name: "update_request_details",
            Area: "Requests & RFIs",
            Description: "Overwrites a request's register details — reference, title, description, "
                + "status, value, response, notes and dates — in one write. Fields omitted are not "
                + "kept: the command replaces the details wholesale, so read the request first and "
                + "carry forward everything that should not change.",
            CommandType: typeof(UpdateRequestDetails),
            ResultType: typeof(Request),
            AuthorisationType: typeof(UpdateRequestDetailsAuthorisation),
            ValidationType: typeof(UpdateRequestDetailsValidation),
            VisibleTo: RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Architect),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "requestId is the record id — find_by_reference resolves REQ-0123 / RFI-049. Use "
                + "get_request_context first and echo the current values for anything unchanged. "
                + "Editing the reference onto a number already in use on the project is rejected. "
                + "On an ExtensionOfTime, eotDaysClaimed / eotDaysGranted record the days sought "
                + "and awarded — the one way to write them; null leaves the recorded values as they are."),

        new AiAction(
            Name: "update_request_form",
            Area: "Requests & RFIs",
            Description: "Saves the structured body of the request's official document — the itemised "
                + "queries plus the basis-of-queries, response-action-required and impact-if-late "
                + "narrative sections. Replaces the form's content in one write.",
            CommandType: typeof(UpdateRequestForm),
            ResultType: typeof(Request),
            AuthorisationType: typeof(UpdateRequestFormAuthorisation),
            ValidationType: typeof(UpdateRequestFormValidation),
            VisibleTo: RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Architect),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "requestId via find_by_reference. The items list replaces the existing items — "
                + "read the request first and carry forward every item that should stay."),

        new AiAction(
            Name: "promote_request_to_rfi",
            Area: "Requests & RFIs",
            Description: "Promotes a General request to an official RFI: mints the project's next RFI "
                + "reference, re-opens it if it was closed, and unlocks the official document. Nothing "
                + "is emailed or drafted — promotion is a pure register action; preparing the email "
                + "draft is a separate, explicit step.",
            CommandType: typeof(PromoteRequestToRfi),
            ResultType: typeof(Request),
            AuthorisationType: typeof(PromoteRequestToRfiAuthorisation),
            ValidationType: typeof(PromoteRequestToRfiValidation),
            VisibleTo: RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "The minted RFI reference cannot be handed back — the request stays an RFI. "
                + "Confirm with the user before calling. requestId via find_by_reference."),

        new AiAction(
            Name: "enable_rfq_on_request",
            Area: "Requests & RFIs",
            Description: "Marks an RFI as also carrying a Request for Quotation, which unlocks creating "
                + "a Variation Order Quote (VOQ) from it. Only valid on a request that is already an "
                + "RFI. No email is sent.",
            CommandType: typeof(EnableRfqOnRequest),
            ResultType: typeof(Request),
            AuthorisationType: typeof(EnableRfqOnRequestAuthorisation),
            ValidationType: typeof(EnableRfqOnRequestValidation),
            VisibleTo: RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "requestId via find_by_reference (RFI-049)."),

        new AiAction(
            Name: "link_request_to_party",
            Area: "Requests & RFIs",
            Description: "Links a request to the external party it is corresponded with — a client or "
                + "an architect (optionally on behalf of a named client). Passing a null/empty partyId "
                + "unlinks the current party. Changes who the request's outbound documents resolve to.",
            CommandType: typeof(LinkRequestToParty),
            ResultType: typeof(Request),
            AuthorisationType: typeof(LinkRequestToPartyAuthorisation),
            ValidationType: typeof(LinkRequestToPartyValidation),
            VisibleTo: RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "partyKind: Client or Architect. onBehalfOfClientId only applies when the party is "
                + "an architect. requestId via find_by_reference."),

        new AiAction(
            Name: "merge_requests",
            Area: "Requests & RFIs",
            Description: "Merges one General request into another: the merged request's conversation, "
                + "itemised queries, description and tagged emails all move to the survivor, and the "
                + "merged request is closed permanently with an audit stamp. There is no unmerge.",
            CommandType: typeof(MergeRequests),
            ResultType: typeof(Request),
            AuthorisationType: typeof(MergeRequestsAuthorisation),
            ValidationType: typeof(MergeRequestsValidation),
            VisibleTo: RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Both requests must be General (not yet promoted) and on the same project. "
                + "survivorRequestId keeps its reference and title. Confirm with the user which "
                + "request survives before calling — the merge cannot be undone. Ids via "
                + "find_by_reference."),

        new AiAction(
            Name: "close_request",
            Area: "Requests & RFIs",
            Description: "Closes a request as at the chosen date — it drops off the open register "
                + "immediately. Recorded as closed by the signed-in user. No email is sent.",
            CommandType: typeof(CloseRequest),
            ResultType: typeof(RequestCloseOutcome),
            AuthorisationType: typeof(CloseRequestAuthorisation),
            ValidationType: typeof(CloseRequestValidation),
            VisibleTo: RoleSet.Of(
                JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.SiteManager),
            EmailStamps: new[] { "ClosedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "closedAt must be today or earlier; omit it to close as at now. Confirm with the "
                + "user before calling. requestId via find_by_reference."),

        new AiAction(
            Name: "return_request_to_triage",
            Area: "Requests & RFIs",
            Description: "Undoes a triage decision: clears the request's tags from its emails so they "
                + "re-enter the mailbox triage Inbox queue. The request itself and its conversation "
                + "history are kept untouched — only the email context goes back to triage.",
            CommandType: typeof(ReturnRequestToTriage),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(ReturnRequestToTriageAuthorisation),
            ValidationType: typeof(ReturnRequestToTriageValidation),
            VisibleTo: TriageRoles.AllowedToTriage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling — the emails must then be re-triaged by "
                + "hand. requestId via find_by_reference."),

        new AiAction(
            Name: "delete_request",
            Area: "Requests & RFIs",
            Description: "Deletes a request permanently, including its whole conversation history and "
                + "the official document's itemised queries. There is no undo. Administrator only.",
            CommandType: typeof(DeleteRequest),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteRequestAuthorisation),
            ValidationType: typeof(DeleteRequestValidation),
            VisibleTo: RoleSet.Of(Role.Admin),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Irreversible. Confirm with the user, naming the request's reference and title, "
                + "before calling. requestId via find_by_reference."),
    };
}

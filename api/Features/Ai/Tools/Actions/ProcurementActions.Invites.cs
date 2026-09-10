using Jewel.JPMS.Api.Features.Procurement.Commands;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class ProcurementActions
{
    private static IEnumerable<AiAction> InvitesActions() => new AiAction[]
    {
        new AiAction(
            Name: "invite_subcontractors_to_bid_package",
            Area: "Procurement",
            Description: "Adds one or more subcontractors to a bid package's tender list and moves "
                + "a Draft package to Inviting. This records the invites in the portal only — no "
                + "email is sent (the invite email is drafted separately with "
                + "prepare_bid_package_invite_draft). Idempotent per subcontractor. Returns the "
                + "package's full recipient list.",
            CommandType: typeof(InviteSubcontractorsToBidPackage),
            ResultType: typeof(IReadOnlyList<BidPackageRecipient>),
            AuthorisationType: typeof(InviteSubcontractorsToBidPackageAuthorisation),
            ValidationType: typeof(InviteSubcontractorsToBidPackageValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "bidPackageId comes from list_bid_packages; subcontractorIds from the "
                + "subcontractor directory."),

        new AiAction(
            Name: "remove_bid_package_recipient",
            Area: "Procurement",
            Description: "Removes one invited subcontractor from a bid package's tender list (the "
                + "invite row, not the directory entry). Returns the package's remaining "
                + "recipients.",
            CommandType: typeof(RemoveBidPackageRecipient),
            ResultType: typeof(IReadOnlyList<BidPackageRecipient>),
            AuthorisationType: typeof(RemoveBidPackageRecipientAuthorisation),
            ValidationType: typeof(RemoveBidPackageRecipientValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Over HTTP both ids are route parameters: bidPackageId from list_bid_packages, "
                + "recipientId from the tender list's row (get_bid_package_context, "
                + "tenderList[].recipientId — never the company name). Confirm with the user "
                + "before calling."),

        new AiAction(
            Name: "decline_bid_package_recipient",
            Area: "Procurement",
            Description: "Records that an invited subcontractor has declined to tender, or undoes "
                + "that (declined false) when recorded in error — undoing restores Responded when "
                + "they hold a live quote, otherwise Invited. The winning recipient cannot be "
                + "declined. Returns the package's full recipient list.",
            CommandType: typeof(DeclineBidPackageRecipient),
            ResultType: typeof(IReadOnlyList<BidPackageRecipient>),
            AuthorisationType: typeof(DeclineBidPackageRecipientAuthorisation),
            ValidationType: typeof(DeclineBidPackageRecipientValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "recipientId comes from the tender list's row (get_bid_package_context, "
                + "tenderList[].recipientId — never the company name)."),

        new AiAction(
            Name: "prepare_bid_package_invite_draft",
            Area: "Procurement",
            Description: "Creates the tender-invite email as a DRAFT in the shared mailbox — "
                + "NOTHING IS SENT; a person reviews and sends it from Outlook. BCC is every "
                + "tender-list recipient still in the running (status Invited, i.e. on the list, "
                + "or Responded) that has a directory email — Declined and Won rows are skipped — "
                + "or, when recipientIds is given, exactly those recipients. The draft attaches "
                + "the generated pricing schedule, the company T&Cs, the package's tender documents "
                + "and its linked drawings; the result's attachedFiles lists them by name, and "
                + "linkedFiles is ONLY the overflow (files too large to attach, sent as download "
                + "links) — an empty linkedFiles never means no attachments. The draft carries the "
                + "package's tag so the sent copy and replies group under the package. "
                + "Confirm-first: the first call is refused; re-call with confirm true after the "
                + "user's yes.",
            CommandType: typeof(PrepareBidPackageInviteDraft),
            ResultType: typeof(BidPackageInviteDraft),
            AuthorisationType: typeof(PrepareBidPackageInviteDraftAuthorisation),
            ValidationType: typeof(PrepareBidPackageInviteDraftValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "BEFORE drafting, read the record: get_bid_package_context for the tender list "
                + "(each row's recipientId and status), then read_record_emails (record_type "
                + "bid_package) to see whether an invite has ALREADY gone — the sent copy is "
                + "tagged to the package and its bcc lists who received it. If some already had "
                + "it, pass recipientIds for ONLY those who have not; never re-invite the whole "
                + "list because one firm was added. recipientIds are tenderList[].recipientId "
                + "from get_bid_package_context — never company names or subcontractorIds; an id "
                + "that is not a recipient with a directory email is ignored, and if none resolve "
                + "the call fails with a readable message. Without recipientIds the default set "
                + "applies (every Invited/Responded row with an email; Declined and Won skipped). "
                + "In the confirm turn show the user exactly who will be BCC'd (company and "
                + "email) and what will attach (the pricing schedule, the T&Cs, the tender "
                + "documents and linked drawings by name), and get their yes before re-calling "
                + "with confirm true. The command drafts exactly the subject and htmlBody it is "
                + "given — agree the wording first. Invite the subcontractors "
                + "(invite_subcontractors_to_bid_package) before drafting; a package with no "
                + "recipients in the running fails with a readable message. Report attachments "
                + "from the result's attachedFiles, never from linkedFiles. The result's "
                + "draftMessageId is the handle for delete_mailbox_draft if the draft has to be "
                + "withdrawn."),

    };
}

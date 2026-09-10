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
                + "NOTHING IS SENT; a person reviews and sends it from Outlook. Every invited "
                + "recipient with a directory email goes in BCC, the package's linked drawings are "
                + "attached, and the draft carries the package's tag so the sent copy and replies "
                + "group under the package.",
            CommandType: typeof(PrepareBidPackageInviteDraft),
            ResultType: typeof(BidPackageInviteDraft),
            AuthorisationType: typeof(PrepareBidPackageInviteDraftAuthorisation),
            ValidationType: typeof(PrepareBidPackageInviteDraftValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "The command drafts exactly the subject and htmlBody it is given — confirm the "
                + "wording with the user first. Invite the subcontractors "
                + "(invite_subcontractors_to_bid_package) before drafting; a package with no "
                + "recipients fails with a readable message. The result's draftMessageId is the "
                + "handle for delete_mailbox_draft if the draft has to be withdrawn."),

    };
}

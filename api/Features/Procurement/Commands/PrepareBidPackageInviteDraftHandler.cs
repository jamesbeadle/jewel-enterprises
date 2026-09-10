using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Drafts the reviewed tender-invite email in the shared mailbox — nothing is sent. The mailbox
// itself is the To (subcontractors must not see each other); BCC is every recipient still in the
// running (on the list or Responded — never Declined or Won) with a directory email, or exactly
// the RecipientIds the command names, and the draft carries the package's tag ("JPMS/BPI-0001")
// so the copy that is eventually sent from Outlook — and the replies triaged onto the same tag —
// group under the package. What travels with it (the generated pricing schedule, the company terms, tender
// documents, linked drawings, the 25 MB overflow-to-links rule) is planned by
// BidPackageInviteMailAssembler — one plan shared with the in-app send path, so the two can never
// disagree. Package status is untouched: inviting recipients already moved a Draft package to
// Inviting, and the actual send happens in Outlook.
public sealed class PrepareBidPackageInviteDraftHandler : ICommandHandler<PrepareBidPackageInviteDraft, BidPackageInviteDraft>
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient mailbox;
    private readonly MailboxIntakeOptions options;
    private readonly BidPackageInviteMailAssembler assembler;

    public PrepareBidPackageInviteDraftHandler(
        JpmsContext context, IMailboxGraphClient mailbox, MailboxIntakeOptions options,
        BidPackageInviteMailAssembler assembler)
    {
        this.context = context; this.mailbox = mailbox; this.options = options;
        this.assembler = assembler;
    }

    public async Task<BidPackageInviteDraft> HandleAsync(PrepareBidPackageInviteDraft command, CancellationToken cancellationToken)
    {
        var package = await context.BidPackages.FindAsync(new object[] { command.BidPackageId }, cancellationToken);
        if (package is null) throw new InvalidOperationException($"Bid package {command.BidPackageId} not found.");

        var chosen = command.RecipientIds is { Count: > 0 } ? command.RecipientIds : null;
        var recipients = await assembler.DefaultBccAsync(command.BidPackageId, cancellationToken, chosen);
        if (recipients.Count == 0)
        {
            throw new InvalidOperationException(chosen is null
                ? "No subcontractors still in the running with an email address in the directory — add "
                  + "recipients before drafting (Declined and Won rows are never invited)."
                : $"None of the {chosen.Count} recipientId(s) given is a recipient of this package still in "
                  + "the running with an email address in the directory — use tenderList[].recipientId from "
                  + "get_bid_package_context (not company names), and skip Declined and Won rows.");
        }

        var plan = await assembler.PlanAsync(package, command.HtmlBody, cancellationToken);

        var message = new MailboxDraftMessage(
            To: new[] { new MailboxDraftRecipient(options.Mailbox) },
            Subject: command.Subject,
            HtmlBody: plan.HtmlBody,
            Attachments: plan.Attach,
            Bcc: recipients,
            // Record tag + Subcontractor pathway: the invite thread is born filed on the
            // subcontractor side, and replies inherit both through the thread sweep.
            Categories: new[] { TriageCategories.Marker, TriageCategories.ForRecord(package.Reference), TriageCategories.Subcontractor });

        var draft = await mailbox.CreateDraftAsync(message, cancellationToken);
        if (draft is null)
            throw new InvalidOperationException(
                "The draft couldn't be created in the shared mailbox. Check the mailbox connection and try again.");

        return new BidPackageInviteDraft(
            package.ToModel(),
            command.Subject,
            recipients.Select(r => r.Email).ToList(),
            draft.WebLink,
            LinkedFiles: plan.LinkedFiles,
            DraftMessageId: draft.Id,
            AttachedFiles: plan.Attach.Select(file => file.FileName).ToList());
    }
}

using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>
/// Sends the tender-invite email from the shared projects mailbox — the in-app counterpart of
/// PrepareBidPackageInviteDraft, sharing its attachment plan through BidPackageInviteMailAssembler.
/// The composer's envelope is authoritative: whatever To/Cc/Bcc it shows is exactly what goes on
/// the wire (an empty To is addressed to the mailbox itself, the house convention for BCC
/// fan-out). Staged as a draft first, then sent through the system's single send chokepoint
/// (SendDraftAsync) — a failed send leaves the reviewed draft in the mailbox's Drafts folder and
/// says so, never losing the email. A successful send clears the package's persisted composer
/// draft: it has served its purpose.
/// </summary>
public sealed class SendBidPackageInviteHandler : ICommandHandler<SendBidPackageInvite, BidPackageInviteSendOutcome>
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient mailbox;
    private readonly MailboxIntakeOptions options;
    private readonly BidPackageInviteMailAssembler assembler;

    public SendBidPackageInviteHandler(
        JpmsContext context, IMailboxGraphClient mailbox, MailboxIntakeOptions options,
        BidPackageInviteMailAssembler assembler)
    {
        this.context = context; this.mailbox = mailbox; this.options = options;
        this.assembler = assembler;
    }

    public async Task<BidPackageInviteSendOutcome> HandleAsync(SendBidPackageInvite command, CancellationToken cancellationToken)
    {
        var package = await context.BidPackages.FindAsync(new object[] { command.BidPackageId }, cancellationToken);
        if (package is null) throw new InvalidOperationException($"Bid package {command.BidPackageId} not found.");

        var to = ParseRecipients(command.To);
        var cc = ParseRecipients(command.Cc);
        var bcc = ParseRecipients(command.Bcc);

        if (to.Count == 0 && cc.Count == 0 && bcc.Count == 0)
            throw new InvalidOperationException("The invite has no recipients — add at least one address before sending.");

        // The house convention for BCC fan-out: the mailbox itself takes the To when the composer
        // leaves it empty, so no subcontractor is ever the visible addressee of a mass invite.
        if (to.Count == 0)
            to.Add(new MailboxDraftRecipient(options.Mailbox));

        var plan = await assembler.PlanAsync(package, command.HtmlBody, cancellationToken);

        var message = new MailboxDraftMessage(
            To: to,
            Subject: command.Subject,
            HtmlBody: plan.HtmlBody,
            Attachments: plan.Attach,
            Bcc: bcc,
            // Record tag + Subcontractor pathway: the invite thread is born filed on the
            // subcontractor side, and replies inherit both through the thread sweep.
            Categories: new[] { TriageCategories.Marker, TriageCategories.ForRecord(package.Reference), TriageCategories.Subcontractor },
            Cc: cc);

        var draft = await mailbox.CreateDraftAsync(message, cancellationToken);
        if (draft is null)
            throw new InvalidOperationException(
                "The invite couldn't be staged in the shared mailbox. Check the mailbox connection and try again — nothing was sent.");

        var recipientCount = to.Count + cc.Count + bcc.Count;
        var attachedFiles = plan.Attach.Select(file => file.FileName).ToList();

        var sent = await mailbox.SendDraftAsync(draft.Id, cancellationToken);
        if (!sent)
        {
            // The reviewed email survives in Drafts — degraded, never lost.
            return new BidPackageInviteSendOutcome(
                package.ToModel(), Sent: false, draft.WebLink, recipientCount, plan.LinkedFiles,
                FailureNote: "The send didn't go through — the invite is saved as a draft in the projects mailbox. "
                    + "Open it there to send, or try again here.",
                AttachedFiles: attachedFiles);
        }

        // The composer draft has served its purpose; the sent copy (tagged to the package) is the
        // record now, readable under Tender responses & related emails.
        package.InviteDraftSubject = null;
        package.InviteDraftBody = null;
        package.InviteDraftTo = null;
        package.InviteDraftCc = null;
        package.InviteDraftBcc = null;
        package.InviteDraftSavedAt = null;
        await context.SaveChangesAsync(cancellationToken);

        var webLink = await mailbox.GetWebLinkAsync(draft.Id, cancellationToken) ?? draft.WebLink;
        return new BidPackageInviteSendOutcome(
            package.ToModel(), Sent: true, webLink, recipientCount, plan.LinkedFiles, AttachedFiles: attachedFiles);
    }

    private static List<MailboxDraftRecipient> ParseRecipients(string? raw) =>
        (raw ?? "")
            .Split(new[] { ';', ',' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(address => address.Contains('@', StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(address => new MailboxDraftRecipient(address))
            .ToList();
}

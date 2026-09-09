using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Procurement.Documents;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Drafts the reviewed work-order email in the shared mailbox — nothing is sent (same convention as
// PrepareBidPackageInviteDraft: the human sends from Outlook). Addressed To the supplier's directory
// email. When the order was awarded from a bid package the draft is tagged with the package's
// reference ("JPMS/BPI-0001"), so the sent copy and the supplier's replies group under the package
// alongside the tender correspondence. The purchase order PDF is attached (2026-09-09), so a
// re-send from Outlook needs no printing first.
public sealed class PrepareWorkOrderEmailDraftHandler : ICommandHandler<PrepareWorkOrderEmailDraft, WorkOrderEmailDraft>
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient mailbox;

    public PrepareWorkOrderEmailDraftHandler(JpmsContext context, IMailboxGraphClient mailbox)
    {
        this.context = context; this.mailbox = mailbox;
    }

    public async Task<WorkOrderEmailDraft> HandleAsync(PrepareWorkOrderEmailDraft command, CancellationToken cancellationToken)
    {
        var order = await context.WorkOrders.FindAsync(new object[] { command.WorkOrderId }, cancellationToken);
        if (order is null) throw new InvalidOperationException($"Work order {command.WorkOrderId} not found.");

        // A draft is invisible to the supplier by definition — approve it first — and a
        // rejected draft never becomes visible. The UI hides the button for both; this
        // keeps the promise against direct calls.
        if (order.Status == (int)WorkOrderStatus.Draft)
            throw new InvalidOperationException(
                "This work order is still a draft — approve it before emailing the supplier.");
        if (order.Status == (int)WorkOrderStatus.Rejected)
            throw new InvalidOperationException(
                "This work order was rejected — it is never sent to the supplier.");

        var supplier = await context.Subcontractors.FindAsync(new object[] { order.SubcontractorId }, cancellationToken);
        if (supplier is null || string.IsNullOrWhiteSpace(supplier.ContactEmail))
            throw new InvalidOperationException(
                "The supplier has no email address in the directory — add one before drafting the work order email.");

        var model = await WorkOrderPoDocumentBuilder.BuildAsync(context, command.WorkOrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Work order {command.WorkOrderId} not found.");
        var purchaseOrderPdf = new MailboxDraftAttachment(model.FileName, "application/pdf", WorkOrderPoRenderer.Render(model));

        // Tag with the source package's reference so the email (and replies) group under the package.
        // Work-order mail is subcontractor correspondence, so the thread is born on that pathway.
        var categories = new List<string> { TriageCategories.Marker, TriageCategories.Subcontractor };
        if (!string.IsNullOrWhiteSpace(order.BidPackageId))
        {
            var package = await context.BidPackages.FindAsync(new object[] { order.BidPackageId! }, cancellationToken);
            if (package is not null) categories.Add(TriageCategories.ForRecord(package.Reference));
        }

        var message = new MailboxDraftMessage(
            To: new[] { new MailboxDraftRecipient(supplier.ContactEmail!, supplier.CompanyName) },
            Subject: command.Subject,
            HtmlBody: command.HtmlBody,
            Attachments: new[] { purchaseOrderPdf },
            Categories: categories);

        var draft = await mailbox.CreateDraftAsync(message, cancellationToken);
        if (draft is null)
            throw new InvalidOperationException(
                "The draft couldn't be created in the shared mailbox. Check the mailbox connection and try again.");

        return new WorkOrderEmailDraft(
            order.ToModel(), command.Subject, supplier.ContactEmail!, draft.WebLink,
            DraftMessageId: draft.Id);
    }
}

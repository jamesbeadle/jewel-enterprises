using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Procurement.Documents;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// SENDS the purchase-order email to the supplier from the shared projects mailbox — the automatic
// counterpart of PrepareWorkOrderEmailDraftHandler, fired straight after an order is released
// (created without "save as draft", or a draft approved). Same guards as the Prepare handler: a
// draft is invisible to the supplier by definition and a rejected draft never becomes visible, so
// both are refused; a supplier without a directory email is refused with the fix in the message.
//
// Failure ordering mirrors triage compose (SendMailboxEmailHandler): stage the draft — categories
// already on it so the sent copy self-files under the order — then SEND last. A failed send
// therefore loses nothing: the reviewed draft stays in Drafts and the outcome says so
// (Sent=false + WebLink + FailureNote); the order is never affected. Every send and every failed
// send leaves an audit row, same as compose.
public sealed class SendWorkOrderPoEmailHandler : ICommandHandler<SendWorkOrderPoEmail, WorkOrderPoEmailOutcome>
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient mailbox;
    private readonly AuditTrail audit;

    public SendWorkOrderPoEmailHandler(JpmsContext context, IMailboxGraphClient mailbox, AuditTrail audit)
    {
        this.context = context; this.mailbox = mailbox; this.audit = audit;
    }

    public async Task<WorkOrderPoEmailOutcome> HandleAsync(SendWorkOrderPoEmail command, CancellationToken cancellationToken)
    {
        var order = await context.WorkOrders.FindAsync(new object[] { command.WorkOrderId }, cancellationToken);
        if (order is null) throw new InvalidOperationException($"Work order {command.WorkOrderId} not found.");

        // Same promise as PrepareWorkOrderEmailDraftHandler, kept against direct calls.
        if (order.Status == (int)WorkOrderStatus.Draft)
            throw new InvalidOperationException(
                "This work order is still a draft — approve it before emailing the supplier.");
        if (order.Status == (int)WorkOrderStatus.Rejected)
            throw new InvalidOperationException(
                "This work order was rejected — it is never sent to the supplier.");

        var supplier = await context.Subcontractors.FindAsync(new object[] { order.SubcontractorId }, cancellationToken);
        if (supplier is null || string.IsNullOrWhiteSpace(supplier.ContactEmail))
            throw new InvalidOperationException(
                "The supplier has no email address in the directory — add one, then email the purchase order from the PO page.");

        var purchaseOrderPdf = await RenderPurchaseOrderAsync(command.WorkOrderId, cancellationToken);

        // Categories on the draft = what the SENT copy should carry, so it self-files: the
        // subcontractor pathway, the order's own record tag (replies group under the order via
        // the shared record-link read-back), and — when the order came from awarding a tender —
        // the source package's tag, so the thread also reads alongside the tender correspondence.
        var categories = new List<string>
        {
            TriageCategories.Marker,
            TriageCategories.Subcontractor,
            TriageCategories.ForRecord(order.Reference)
        };
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
                "The purchase-order email couldn't be staged in the shared mailbox, so nothing was sent. "
                + "Check the mailbox connection, then email it from the PO page.");

        // ---- SEND — the irreversible step, last ----
        var sent = await mailbox.SendDraftAsync(draft.Id, cancellationToken);
        if (!sent)
        {
            await audit.WriteAsync(
                AuditEventType.EmailSendFailed,
                $"Send failed for \"{command.Subject}\" (to {supplier.ContactEmail}) — the purchase-order email is saved in the mailbox's Drafts folder.",
                pathway: "Subcontractor",
                projectId: order.ProjectId,
                recordType: RecordType.WorkOrder,
                recordId: order.WorkOrderId,
                recordReference: order.Reference,
                webLink: draft.WebLink,
                cancellationToken: cancellationToken);

            return new WorkOrderPoEmailOutcome(
                order.WorkOrderId, Sent: false, supplier.ContactEmail!, draft.WebLink,
                FailureNote: "The send didn't go through — the purchase-order email is saved as a draft in the "
                    + "projects mailbox. Open it in Outlook to send it from there, or re-send from the PO page.");
        }

        // Immutable ids: the draft's id stays valid on the sent message, so re-read its webLink
        // to point the audit row (and the outcome) at the sent copy.
        var sentWebLink = await mailbox.GetWebLinkAsync(draft.Id, cancellationToken) ?? draft.WebLink;
        await audit.WriteAsync(
            AuditEventType.EmailSent,
            $"Sent \"{command.Subject}\" to {supplier.ContactEmail}.",
            pathway: "Subcontractor",
            projectId: order.ProjectId,
            recordType: RecordType.WorkOrder,
            recordId: order.WorkOrderId,
            recordReference: order.Reference,
            webLink: sentWebLink,
            cancellationToken: cancellationToken);

        return new WorkOrderPoEmailOutcome(order.WorkOrderId, Sent: true, supplier.ContactEmail!, sentWebLink);
    }

    // The supplier receives the purchase order itself, not only its summary (2026-09-09, the
    // accountant's ask) — the same branded PDF the PO page prints and the reply draft attaches.
    private async Task<MailboxDraftAttachment> RenderPurchaseOrderAsync(string workOrderId, CancellationToken cancellationToken)
    {
        var model = await WorkOrderPoDocumentBuilder.BuildAsync(context, workOrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Work order {workOrderId} not found.");
        return new MailboxDraftAttachment(model.FileName, "application/pdf", WorkOrderPoRenderer.Render(model));
    }
}

using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.MailboxCompose;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

public sealed partial class SendMailboxEmailHandler
{
    private async Task<ComposeOutcome> LeaveAsDraftAsync(Compose compose, CancellationToken cancellationToken)
    {
        await AuditAsync(compose, AuditEventType.DraftCreated,
            $"Draft \"{compose.Subject}\" staged for {Recipients(compose.ToAddresses, compose.CcAddresses)} — review and send from Outlook.",
            compose.WebLink, withRecord: true, cancellationToken);
        return Unsent(compose, failureNote: null);
    }

    // The reviewed draft survives in Drafts; nothing else changes state here. A raised
    // request / record link (which tagged the thread already) is kept: the words are
    // written and waiting in Outlook, exactly like the old draft-only flow.
    private async Task<ComposeOutcome> ReportFailedSendAsync(Compose compose, CancellationToken cancellationToken)
    {
        await AuditAsync(compose, AuditEventType.EmailSendFailed,
            $"Send failed for \"{compose.Subject}\" ({Recipients(compose.ToAddresses, compose.CcAddresses)}) — the draft is saved in the mailbox's Drafts folder.",
            compose.WebLink, withRecord: false, cancellationToken);
        return Unsent(compose,
            failureNote: "The send didn't go through — your email is saved as a draft in the projects mailbox. "
                + "Open it in Outlook to send it from there, or try again.");
    }

    private static ComposeOutcome Unsent(Compose compose, string? failureNote) =>
        compose.Outcome(sent: false, compose.WebLink, threadHandled: compose.FiledToRecord, failureNote);

    /// <summary>The reply triages the thread: dealt with by answering, JPMS/Replied + pathway
    /// across the thread up to the anchor. Best-effort AFTER the send — a failure leaves the email
    /// visibly queued (with a Replied hint chip from the sent copy) and re-tagging is idempotent.
    /// Returns whether the thread now counts as handled.</summary>
    private async Task<bool> HandleThreadAsync(Compose compose, CancellationToken cancellationToken)
    {
        var handled = compose.FiledToRecord;
        if (!compose.WillHandleThread || compose.RecordTag is not null || compose.Snapshot is not { } snapshot)
            return handled;
        var anchor = compose.Command.ReplyToMessageId!;
        try
        {
            handled = await threadTagger.TagThreadAsync(
                anchor, snapshot.InternetMessageId, snapshot.ConversationId,
                TriageCategories.Replied, cancellationToken, anchorReceivedAt: snapshot.ReceivedAt);
            if (handled && compose.EffectiveBucket is not null && compose.ExistingBucket is null)
                await threadTagger.TagThreadAsync(
                    anchor, snapshot.InternetMessageId, snapshot.ConversationId,
                    compose.EffectiveBucket, cancellationToken, anchorReceivedAt: snapshot.ReceivedAt);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) { /* visible + retryable */ }
        return handled;
    }

    private async Task<ComposeOutcome> RecordSentAsync(Compose compose, bool threadHandled, CancellationToken cancellationToken)
    {
        // Immutable ids: the draft's id stays valid on the sent message, so re-read its webLink to
        // point the audit row at the sent copy.
        var sentWebLink = await graph.GetWebLinkAsync(compose.DraftId, cancellationToken) ?? compose.WebLink;
        await AuditAsync(compose, AuditEventType.EmailSent,
            $"Sent \"{compose.Subject}\" {Recipients(compose.ToAddresses, compose.CcAddresses)}.",
            sentWebLink, withRecord: true, cancellationToken);

        // Every to-do the sent copy is filed under gets an "Emailed …" timeline line and, if it was
        // still Open, becomes In progress — the assignee's proof of chasing, without closing it.
        await todoActivity.RecordSentAsync(
            compose.WorkflowStamp, compose.Subject,
            compose.ToAddresses, compose.SenderEmail, cancellationToken);
        // Every defect the sent copy is filed under, when the email went to its supplier, records
        // its first "sent to supplier" and moves Open → In progress (DefectSupplierSendRecorder).
        await defectSends.RecordSentAsync(
            compose.WorkflowStamp, compose.ToAddresses, compose.SenderEmail, cancellationToken);
        await OpenRaisedRequestAsync(compose, cancellationToken);

        return compose.Outcome(sent: true, sentWebLink, threadHandled, failureNote: null);
    }

    // A raised request whose reply has now been SENT moves Needs action → Open (the ball is
    // with the correspondent) — same rule as the draft flows, only now the send is real.
    private async Task OpenRaisedRequestAsync(Compose compose, CancellationToken cancellationToken)
    {
        if (compose.RaisedRequest is not { } raised) return;
        var raisedEntity = await context.Requests.FirstOrDefaultAsync(r => r.RequestId == raised.RequestId, cancellationToken);
        if (raisedEntity is null || (RequestStatus)raisedEntity.Status != RequestStatus.NeedsAction) return;
        raisedEntity.Status = (int)RequestStatus.Open;
        await context.SaveChangesAsync(cancellationToken);
        compose.RaisedRequest = raised with { Status = RequestStatus.Open };
    }

    private Task AuditAsync(Compose compose, AuditEventType eventType, string detail, string? webLink, bool withRecord, CancellationToken cancellationToken) =>
        audit.WriteAsync(
            eventType,
            detail,
            pathway: compose.PathwayLabel,
            projectId: compose.ProjectId,
            recordType: withRecord ? compose.AuditRecordType : null,
            recordId: withRecord ? compose.AuditRecordId : null,
            recordReference: withRecord ? compose.AuditRecordReference : "",
            conversationId: compose.Snapshot?.ConversationId,
            emailMessageId: compose.Command.ReplyToMessageId,
            internetMessageId: compose.Snapshot?.InternetMessageId,
            webLink: webLink,
            cancellationToken: cancellationToken);
}

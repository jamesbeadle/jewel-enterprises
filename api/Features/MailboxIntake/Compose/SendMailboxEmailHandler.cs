using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Closeout;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.MailboxIntake.Sharing;
using Jewel.JPMS.Api.Features.Progress.Storage;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Api.Features.Todos;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

/// <summary>
/// Triage compose: stage a draft in the projects mailbox and SEND it (docs: the 2026-08-04 send
/// decision, reversing ADR-006's draft-only rule — see the ADR amendment). One handler covers all
/// three shapes: a reply inside an existing conversation (Graph's createReplyAll supplies the
/// threading headers and quoted history; the composer's envelope then replaces the scaffolding
/// wholesale), a FORWARD of an existing email (Graph's createForward — same scaffolding shape, and
/// Graph carries the original attachments onto the draft itself), and a brand-new outbound email.
/// A forward is passing the email on, not answering it, so it never tags the thread JPMS/Replied;
/// its sent copy still inherits the anchor's record tags (same conversation), so it files itself
/// into a linked record's correspondence exactly like a reply.
///
/// Failure ordering puts the irreversible step last and keeps every failure recoverable:
///   validate → resolve attachments → (optional) raise request → stage draft (+envelope, categories,
///   attachments) → SEND → tag the inbound thread → audit.
/// A failed SEND therefore triages nothing and loses nothing — the reviewed draft stays in Drafts
/// (outcome Sent=false + webLink, so the user can finish in Outlook). A failed tag AFTER a send is
/// visible (the email is still queued, now with a Replied hint) and idempotently retryable. To-dos
/// raised alongside a compose are a separate command the UI runs first — see TriageQueue.
///
/// Replying IS a triage decision (MarkThreadHandled): after a successful send the inbound thread is
/// tagged JPMS/Replied + pathway — unless the compose also filed it to a record (opt-in
/// AlsoRaiseRequest or LinkRecord), whose tag already says more than "replied".
/// </summary>
public sealed partial class SendMailboxEmailHandler : ICommandHandler<SendMailboxEmail, ComposeOutcome>
{
    /// <summary>Cap on the combined size of a composed email's attachments — the usual Exchange
    /// message-size ceiling. One number for the whole system, owned by the planner; kept here as an
    /// alias for existing callers.</summary>
    public const long MaxTotalAttachmentBytes = EmailAttachmentPlanner.MaxTotalAttachmentBytes;

    private readonly JpmsContext context;
    private readonly IMailboxGraphClient graph;
    private readonly IIntakeMessageReader reader;
    private readonly RecordThreadTagger threadTagger;
    private readonly RecordProviderRegistry providers;
    private readonly IDrawingBlobStore drawingBlobs;
    private readonly IProgressPhotoStore photoBlobs;
    private readonly IEmailFileShareStore shareStore;
    private readonly ComposeHtmlPipeline pipeline;
    private readonly AuditTrail audit;
    private readonly TodoEmailActivityRecorder todoActivity;
    private readonly DefectSupplierSendRecorder defectSends;
    private readonly ICommandHandler<CreateRequestFromMessage, Request> createRequest;

    public SendMailboxEmailHandler(
        JpmsContext context,
        IMailboxGraphClient graph,
        IIntakeMessageReader reader,
        RecordThreadTagger threadTagger,
        RecordProviderRegistry providers,
        IDrawingBlobStore drawingBlobs,
        IProgressPhotoStore photoBlobs,
        IEmailFileShareStore shareStore,
        ComposeHtmlPipeline pipeline,
        AuditTrail audit,
        TodoEmailActivityRecorder todoActivity,
        DefectSupplierSendRecorder defectSends,
        ICommandHandler<CreateRequestFromMessage, Request> createRequest)
    {
        this.todoActivity = todoActivity;
        this.defectSends = defectSends;
        this.context = context;
        this.graph = graph;
        this.reader = reader;
        this.threadTagger = threadTagger;
        this.providers = providers;
        this.drawingBlobs = drawingBlobs;
        this.photoBlobs = photoBlobs;
        this.shareStore = shareStore;
        this.pipeline = pipeline;
        this.audit = audit;
        this.createRequest = createRequest;
    }

    /// <summary>An uploaded file's bytes, delivered by the endpoint from the multipart request and
    /// matched to a <see cref="ComposeAttachmentRef"/> (Source=Upload) by part name.</summary>
    public sealed record UploadedFile(string FileName, string ContentType, byte[] Content);

    // ICommandHandler shape: a plain JSON call carries no uploads.
    public Task<ComposeOutcome> HandleAsync(SendMailboxEmail command, CancellationToken cancellationToken) =>
        HandleAsync(command, uploads: null, cancellationToken);

    /// <summary>The pipeline in the order the class summary gives, each step a method below: the
    /// irreversible send comes after everything that can be checked or staged, and every step
    /// after it is visible and retryable.</summary>
    public async Task<ComposeOutcome> HandleAsync(
        SendMailboxEmail command, IReadOnlyDictionary<string, UploadedFile>? uploads, CancellationToken cancellationToken)
    {
        var compose = Compose.Validated(command);
        await ReadAnchorAsync(compose, cancellationToken);
        await ResolveBodyAndAttachmentsAsync(compose, uploads, cancellationToken);
        await FileToRecordAsync(compose, cancellationToken);
        await StageDraftAsync(compose, cancellationToken);

        if (command.SaveAsDraftOnly) return await LeaveAsDraftAsync(compose, cancellationToken);
        if (!await graph.SendDraftAsync(compose.DraftId, cancellationToken)) return await ReportFailedSendAsync(compose, cancellationToken);

        var threadHandled = await HandleThreadAsync(compose, cancellationToken);
        return await RecordSentAsync(compose, threadHandled, cancellationToken);
    }
}

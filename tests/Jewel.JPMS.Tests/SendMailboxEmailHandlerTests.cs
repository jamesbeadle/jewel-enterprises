using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Closeout;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.MailboxIntake.Sharing;
using Jewel.JPMS.Api.Features.Progress.Storage;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Api.Features.Todos;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// Characterisation of the compose handler — the one path that sends real email — pinned before
/// its ten-step HandleAsync is divided: which Graph calls a new email, a reply and a forward make
/// and in what order, what the staged draft carries (categories, envelope), what the thread is
/// tagged with afterwards, what the audit register records, and how each failure degrades. A
/// recording fake stands in for Graph; everything else is the real class over an in-memory
/// database.
/// </summary>
public sealed class SendMailboxEmailHandlerTests
{
    private const string Anchor = "msg-anchor";
    private static readonly DateTimeOffset ReceivedAt = new(2026, 9, 1, 9, 30, 0, TimeSpan.Zero);

    // ---- A brand-new email ------------------------------------------------------------------

    [Fact]
    public async Task ANewEmailIsStagedWithItsEnvelopeThenSentAndAudited()
    {
        var fixture = new Fixture();
        var command = NewEmail(subject: "Site access on Monday", body: "Gates open at seven.");

        var outcome = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(new[] { "CreateDraft", "SendDraft:draft-1", "GetWebLink:draft-1" }, fixture.Graph.Calls);
        var draft = fixture.Graph.CreatedDraft!;
        Assert.Equal("Site access on Monday", draft.Subject);
        Assert.Equal(new[] { "client@example.com" }, draft.To.Select(r => r.Email));
        Assert.Equal(new[] { "architect@example.com" }, draft.Cc!.Select(r => r.Email));
        Assert.Null(draft.Bcc);
        Assert.Null(draft.Categories);
        Assert.Contains("Gates open at seven.", draft.HtmlBody);

        Assert.True(outcome.Sent);
        Assert.Equal("draft-1", outcome.MessageId);
        Assert.Equal("https://web/sent", outcome.WebLink);
        Assert.False(outcome.ThreadHandled);
        Assert.Null(outcome.FailureNote);
        Assert.Equal(new[] { "client@example.com" }, outcome.To);
        Assert.Equal(new[] { "architect@example.com" }, outcome.Cc);

        var audit = Assert.Single(fixture.AuditEvents());
        Assert.Equal((int)AuditEventType.EmailSent, audit.EventType);
        Assert.Equal("", audit.Pathway);
        Assert.Equal("https://web/sent", audit.WebLink);
        Assert.StartsWith("Sent \"Site access on Monday\" to client@example.com (cc architect@example.com)", audit.Detail);
    }

    [Fact]
    public async Task RecipientsAreCleanedAndDeduplicated()
    {
        var fixture = new Fixture();
        var command = NewEmail(subject: "s", body: "b") with
        {
            To = new[] { new ComposeRecipient(" client@example.com ", " Client "), new ComposeRecipient("CLIENT@example.com"), new ComposeRecipient("not-an-address") },
            Cc = Array.Empty<ComposeRecipient>(),
        };

        await fixture.Handler.HandleAsync(command, CancellationToken.None);

        var to = Assert.Single(fixture.Graph.CreatedDraft!.To);
        Assert.Equal(("client@example.com", "Client"), (to.Email, to.Name));
        Assert.Null(fixture.Graph.CreatedDraft!.Cc);
    }

    // ---- A reply -----------------------------------------------------------------------------

    [Fact]
    public async Task AReplyIsStagedOnTheThreadWithTheComposersEnvelopeAndTriagesTheThreadAfterTheSend()
    {
        var fixture = new Fixture();
        fixture.Graph.Snapshot = Snapshot(categories: new[] { "JPMS", "JPMS/TODO-0011" });
        var command = Reply(pathway: "Client");

        var outcome = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(
            new[]
            {
                $"GetSnapshot:{Anchor}",
                "CreateReplyDraft",
                "UpdateDraftEnvelope:reply-1",
                "SendDraft:reply-1",
                $"Assign:{Anchor}:JPMS/Replied",
                "TagConversationMembers:conv-1:JPMS/Replied",
                $"Assign:{Anchor}:JPMS/Client",
                "TagConversationMembers:conv-1:JPMS/Client",
                "GetWebLink:reply-1",
            },
            fixture.Graph.Calls);

        var reply = fixture.Graph.CreatedReply!;
        Assert.Equal(Anchor, reply.MessageId);
        Assert.False(reply.Forward);
        // The sent copy self-files under the thread's record tag and the pathway; Replied is
        // stamped on the copy only when the thread carries no record tag at all (the inbound
        // thread itself is still tagged Replied below).
        Assert.Equal(new[] { "JPMS", "JPMS/TODO-0011", "JPMS/Client" }, reply.Categories);
        Assert.Equal(("Re: Boundary wall", "client@example.com", "architect@example.com", ""), fixture.Graph.Envelope);

        Assert.True(outcome.Sent);
        Assert.True(outcome.ThreadHandled);
        var audit = Assert.Single(fixture.AuditEvents());
        Assert.Equal("Client", audit.Pathway);
        Assert.Equal("conv-1", audit.ConversationId);
        Assert.Equal(Anchor, audit.EmailMessageId);
    }

    [Fact]
    public async Task AThreadAlreadyFiledUnderAPathwayKeepsItAndIsNotRetaggedWithOne()
    {
        var fixture = new Fixture();
        fixture.Graph.Snapshot = Snapshot(categories: new[] { "JPMS", "JPMS/Subcontractor" });

        var outcome = await fixture.Handler.HandleAsync(Reply(pathway: "Client"), CancellationToken.None);

        Assert.Equal(new[] { "JPMS", "JPMS/Replied", "JPMS/Subcontractor" }, fixture.Graph.CreatedReply!.Categories);
        Assert.DoesNotContain(fixture.Graph.Calls, call => call.Contains("JPMS/Client"));
        Assert.Contains($"Assign:{Anchor}:JPMS/Replied", fixture.Graph.Calls);
        Assert.Equal("Subcontractor", Assert.Single(fixture.AuditEvents()).Pathway);
        Assert.True(outcome.ThreadHandled);
    }

    [Fact]
    public async Task AReplyThatIsNotATriageDecisionCarriesNoTagsAndTagsNothing()
    {
        var fixture = new Fixture();
        fixture.Graph.Snapshot = Snapshot(categories: Array.Empty<string>());

        var outcome = await fixture.Handler.HandleAsync(Reply(pathway: null) with { MarkThreadHandled = false }, CancellationToken.None);

        Assert.Null(fixture.Graph.CreatedReply!.Categories);
        Assert.DoesNotContain(fixture.Graph.Calls, call => call.StartsWith("Assign:"));
        Assert.True(outcome.Sent);
        Assert.False(outcome.ThreadHandled);
    }

    // ---- A forward ---------------------------------------------------------------------------

    [Fact]
    public async Task AForwardInheritsTheThreadsRecordTagsButNeverHandlesTheThread()
    {
        var fixture = new Fixture();
        fixture.Graph.Snapshot = Snapshot(categories: new[] { "JPMS", "JPMS/TODO-0011", "JPMS/Client" });

        var outcome = await fixture.Handler.HandleAsync(Reply(pathway: "Client") with { Forward = true }, CancellationToken.None);

        var forward = fixture.Graph.CreatedReply!;
        Assert.True(forward.Forward);
        Assert.Equal(new[] { "JPMS", "JPMS/TODO-0011", "JPMS/Client" }, forward.Categories);
        Assert.DoesNotContain(fixture.Graph.Calls, call => call.StartsWith("Assign:"));
        Assert.True(outcome.Sent);
        Assert.False(outcome.ThreadHandled);
    }

    // ---- Stopping short of the send -----------------------------------------------------------

    [Fact]
    public async Task SaveAsDraftOnlyStagesAuditsTheDraftAndSendsNothing()
    {
        var fixture = new Fixture();

        var outcome = await fixture.Handler.HandleAsync(NewEmail("s", "b") with { SaveAsDraftOnly = true }, CancellationToken.None);

        Assert.Equal(new[] { "CreateDraft" }, fixture.Graph.Calls);
        Assert.False(outcome.Sent);
        Assert.Null(outcome.FailureNote);
        Assert.Equal("https://web/draft-1", outcome.WebLink);
        var audit = Assert.Single(fixture.AuditEvents());
        Assert.Equal((int)AuditEventType.DraftCreated, audit.EventType);
        Assert.StartsWith("Draft \"s\" staged for to client@example.com (cc architect@example.com)", audit.Detail);
    }

    [Fact]
    public async Task AFailedSendLeavesTheDraftSaysSoAndTriagesNothing()
    {
        var fixture = new Fixture();
        fixture.Graph.Snapshot = Snapshot(categories: Array.Empty<string>());
        fixture.Graph.SendSucceeds = false;

        var outcome = await fixture.Handler.HandleAsync(Reply(pathway: "Client"), CancellationToken.None);

        Assert.Equal(
            new[] { $"GetSnapshot:{Anchor}", "CreateReplyDraft", "UpdateDraftEnvelope:reply-1", "SendDraft:reply-1" },
            fixture.Graph.Calls);
        Assert.False(outcome.Sent);
        Assert.False(outcome.ThreadHandled);
        Assert.Equal("reply-1", outcome.MessageId);
        Assert.Equal("https://web/reply-1", outcome.WebLink);
        Assert.StartsWith("The send didn't go through", outcome.FailureNote);
        var audit = Assert.Single(fixture.AuditEvents());
        Assert.Equal((int)AuditEventType.EmailSendFailed, audit.EventType);
        Assert.Equal("Client", audit.Pathway);
    }

    // ---- Refusals before anything is created -------------------------------------------------

    [Theory]
    [InlineData("no recipients", "Add at least one recipient before sending.")]
    [InlineData("cc only", "Add a To recipient (Cc/Bcc-only emails are refused by most mail servers).")]
    [InlineData("no subject", "Write a subject before sending.")]
    [InlineData("no body", "Write the email before sending.")]
    [InlineData("raise on new", "A request can only be raised from a reply to an email.")]
    [InlineData("raise on forward", "A request is raised from a reply, not a forward.")]
    [InlineData("raise without project", "Choose the project the request is raised on.")]
    [InlineData("raise and link", "Raise a request or link an existing record — not both in one send.")]
    public async Task AnInvalidComposeIsRefusedBeforeGraphIsTouched(string shape, string message)
    {
        var fixture = new Fixture();
        var command = shape switch
        {
            "no recipients" => NewEmail("s", "b") with { To = Array.Empty<ComposeRecipient>(), Cc = Array.Empty<ComposeRecipient>() },
            "cc only" => NewEmail("s", "b") with { To = Array.Empty<ComposeRecipient>() },
            "no subject" => NewEmail("  ", "b"),
            "no body" => NewEmail("s", " "),
            "raise on new" => NewEmail("s", "b") with { AlsoRaiseRequest = true, ProjectId = "p1" },
            "raise on forward" => Reply("Client") with { Forward = true, AlsoRaiseRequest = true, ProjectId = "p1" },
            "raise without project" => Reply("Client") with { AlsoRaiseRequest = true },
            _ => Reply("Client") with { AlsoRaiseRequest = true, ProjectId = "p1", LinkRecordType = RecordType.WorkOrder, LinkRecordId = "wo1" },
        };

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Handler.HandleAsync(command, CancellationToken.None));

        Assert.Equal(message, refusal.Message);
        Assert.Empty(fixture.Graph.Calls);
        Assert.Empty(fixture.AuditEvents());
    }

    [Fact]
    public async Task AReplyWhoseEmailHasGoneIsRefused()
    {
        var fixture = new Fixture();
        fixture.Graph.Snapshot = null;

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Handler.HandleAsync(Reply("Client"), CancellationToken.None));

        Assert.Equal("The email you're replying to could not be read from the mailbox.", refusal.Message);
        Assert.Equal(new[] { $"GetSnapshot:{Anchor}" }, fixture.Graph.Calls);
    }

    // ---- Filing to a record while replying ---------------------------------------------------

    [Fact]
    public async Task LinkingARecordTagsTheThreadBeforeTheSendAndFilesTheSentCopyUnderIt()
    {
        var fixture = new Fixture();
        fixture.Graph.Snapshot = Snapshot(categories: new[] { "JPMS" });
        fixture.Provider.Record = new LinkableRecord(RecordType.WorkOrder, "wo1", "p1", "WO-0042", "WO-0042", "Groundworks");

        var outcome = await fixture.Handler.HandleAsync(
            Reply(pathway: null) with { LinkRecordType = RecordType.WorkOrder, LinkRecordId = "wo1" }, CancellationToken.None);

        Assert.Equal(
            new[]
            {
                $"GetSnapshot:{Anchor}",
                $"Assign:{Anchor}:JPMS/WO-0042",
                "TagConversationMembers:conv-1:JPMS/WO-0042",
                "CreateReplyDraft",
                "UpdateDraftEnvelope:reply-1",
                "SendDraft:reply-1",
                "GetWebLink:reply-1",
            },
            fixture.Graph.Calls);
        // The record tag says more than "replied", so Replied is not stamped; the work order's
        // own pathway (Subcontractor) files the copy.
        Assert.Equal(new[] { "JPMS", "JPMS/WO-0042", "JPMS/Subcontractor" }, fixture.Graph.CreatedReply!.Categories);
        Assert.True(outcome.ThreadHandled);
        var audit = Assert.Single(fixture.AuditEvents());
        Assert.Equal((int)RecordType.WorkOrder, audit.RecordType);
        Assert.Equal("wo1", audit.RecordId);
        Assert.Equal("WO-0042", audit.RecordReference);
        Assert.Equal("Subcontractor", audit.Pathway);
    }

    [Fact]
    public async Task ALinkThatWouldCrossTheClientWallIsRefused()
    {
        var fixture = new Fixture();
        fixture.Graph.Snapshot = Snapshot(categories: new[] { "JPMS", "JPMS/Client" });
        fixture.Provider.Record = new LinkableRecord(RecordType.WorkOrder, "wo1", "p1", "WO-0042", "WO-0042", "Groundworks");

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Handler.HandleAsync(
            Reply(pathway: null) with { LinkRecordType = RecordType.WorkOrder, LinkRecordId = "wo1" }, CancellationToken.None));

        Assert.StartsWith("This thread is filed under Client; WO-0042 would file it under Subcontractor.", refusal.Message);
        Assert.Equal(new[] { $"GetSnapshot:{Anchor}" }, fixture.Graph.Calls);
    }

    // ---- Raising a request while replying ----------------------------------------------------

    [Fact]
    public async Task RaisingARequestCreatesItFirstAndRollsItBackWhenTheDraftCannotBeStaged()
    {
        var fixture = new Fixture();
        fixture.Graph.Snapshot = Snapshot(categories: new[] { "JPMS" });
        fixture.Graph.ReplyDraftSucceeds = false;
        fixture.Context.Projects.Add(new ProjectEntity { ProjectId = "p1", Reference = "JBB-2026-001", Name = "Elm Grove" });
        await fixture.Context.SaveChangesAsync();

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Handler.HandleAsync(
            Reply(pathway: "Client") with { AlsoRaiseRequest = true, ProjectId = "p1" }, CancellationToken.None));

        Assert.StartsWith("The reply couldn't be staged in the projects mailbox", refusal.Message);
        Assert.Equal("Replied to email in thread with:\n\nThanks — the wall is on the boundary.", fixture.CreateRequest.Received!.Description);
        Assert.Equal(
            new[] { $"GetSnapshot:{Anchor}", "CreateReplyDraft", "ClearRequestTags:JPMS/JBB-2026-001-REQ-0007" },
            fixture.Graph.Calls);
        Assert.Empty(fixture.Context.Requests.ToList());
    }

    [Fact]
    public async Task ARaisedRequestMovesToOpenOnceTheReplyIsSent()
    {
        var fixture = new Fixture();
        fixture.Graph.Snapshot = Snapshot(categories: new[] { "JPMS" });
        fixture.Context.Projects.Add(new ProjectEntity { ProjectId = "p1", Reference = "JBB-2026-001", Name = "Elm Grove" });
        await fixture.Context.SaveChangesAsync();

        var outcome = await fixture.Handler.HandleAsync(
            Reply(pathway: "Client") with { AlsoRaiseRequest = true, ProjectId = "p1" }, CancellationToken.None);

        Assert.Equal(new[] { "JPMS", "JPMS/JBB-2026-001-REQ-0007", "JPMS/Client" }, fixture.Graph.CreatedReply!.Categories);
        Assert.DoesNotContain(fixture.Graph.Calls, call => call.Contains("JPMS/Replied"));
        Assert.True(outcome.Sent);
        Assert.True(outcome.ThreadHandled);
        Assert.Equal(RequestStatus.Open, outcome.RaisedRequest!.Status);
        Assert.Equal((int)RequestStatus.Open, Assert.Single(fixture.Context.Requests.ToList()).Status);
        var audit = Assert.Single(fixture.AuditEvents());
        Assert.Equal((int)RecordType.Request, audit.RecordType);
        Assert.Equal("REQ-0007", audit.RecordReference);
    }

    // ---- Shapes ------------------------------------------------------------------------------

    private static SendMailboxEmail NewEmail(string subject, string body) => new(
        ReplyToMessageId: null,
        ReplyToInternetMessageId: null,
        To: new[] { new ComposeRecipient("client@example.com") },
        Cc: new[] { new ComposeRecipient("architect@example.com") },
        Bcc: Array.Empty<ComposeRecipient>(),
        Subject: subject,
        Body: body,
        BodyIsHtml: false,
        Attachments: Array.Empty<ComposeAttachmentRef>(),
        SenderEmail: "pm@jewelbb.co.uk");

    private static SendMailboxEmail Reply(string? pathway) => NewEmail("Re: Boundary wall", "Thanks — the wall is on the boundary.") with
    {
        ReplyToMessageId = Anchor,
        ReplyToInternetMessageId = "<anchor@example.com>",
        Pathway = pathway,
    };

    private static MailboxSnapshot Snapshot(IReadOnlyList<string> categories) => new(
        "<anchor@example.com>", "conv-1", null, "client@example.com", "A Client", "Boundary wall", "…", ReceivedAt, categories);

    // ---- The fixture -------------------------------------------------------------------------

    private sealed class Fixture
    {
        public JpmsContext Context { get; }
        public RecordingGraph Graph { get; } = new();
        public FakeProvider Provider { get; } = new();
        public FakeCreateRequest CreateRequest { get; }
        public SendMailboxEmailHandler Handler { get; }

        public Fixture()
        {
            Context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
                .UseInMemoryDatabase($"compose-{Guid.NewGuid():N}")
                .Options);
            var actor = new AuditActor { Email = "pm@jewelbb.co.uk" };
            CreateRequest = new FakeCreateRequest(Context);
            Handler = new SendMailboxEmailHandler(
                Context,
                Graph,
                new NullIntakeMessageReader(),
                new RecordThreadTagger(Graph),
                new RecordProviderRegistry(new[] { Provider }),
                new NoBlobs(),
                new NoPhotos(),
                new NoShareStore(),
                new ComposeHtmlPipeline(),
                new AuditTrail(Context, actor, NullLogger<AuditTrail>.Instance),
                new TodoEmailActivityRecorder(Context, new TodoActivityRecorder(Context, actor), NullLogger<TodoEmailActivityRecorder>.Instance),
                new DefectSupplierSendRecorder(Context, NullLogger<DefectSupplierSendRecorder>.Instance),
                CreateRequest);
        }

        public List<AuditEventEntity> AuditEvents() => Context.AuditEvents.OrderBy(e => e.OccurredAt).ToList();
    }

    /// <summary>Records every Graph call in order; answers are configurable per test.</summary>
    private sealed class RecordingGraph : IMailboxGraphClient
    {
        public List<string> Calls { get; } = new();
        public MailboxSnapshot? Snapshot { get; set; }
        public bool SendSucceeds { get; set; } = true;
        public bool ReplyDraftSucceeds { get; set; } = true;
        public MailboxDraftMessage? CreatedDraft { get; private set; }
        public MailboxReplyDraftMessage? CreatedReply { get; private set; }
        public (string Subject, string To, string Cc, string Bcc)? Envelope { get; private set; }

        public Task<MailboxSnapshot?> GetSnapshotAsync(string messageId, string? internetMessageId, CancellationToken ct)
        {
            Calls.Add($"GetSnapshot:{messageId}");
            return Task.FromResult(Snapshot);
        }

        public Task<MailboxDraft?> CreateDraftAsync(MailboxDraftMessage draft, CancellationToken ct)
        {
            Calls.Add("CreateDraft");
            CreatedDraft = draft;
            return Task.FromResult<MailboxDraft?>(new MailboxDraft("draft-1", "https://web/draft-1"));
        }

        public Task<MailboxReplyDraft?> CreateReplyDraftAsync(MailboxReplyDraftMessage reply, CancellationToken ct)
        {
            Calls.Add("CreateReplyDraft");
            CreatedReply = reply;
            return Task.FromResult(ReplyDraftSucceeds
                ? new MailboxReplyDraft("reply-1", "https://web/reply-1", "Re: Boundary wall", new[] { "client@example.com" }, Array.Empty<string>())
                : null);
        }

        public Task<bool> UpdateDraftEnvelopeAsync(string draftMessageId, IReadOnlyList<MailboxDraftRecipient> to, IReadOnlyList<MailboxDraftRecipient> cc, IReadOnlyList<MailboxDraftRecipient> bcc, string subject, CancellationToken ct)
        {
            Calls.Add($"UpdateDraftEnvelope:{draftMessageId}");
            Envelope = (subject, Join(to), Join(cc), Join(bcc));
            return Task.FromResult(true);
        }

        public Task<bool> SendDraftAsync(string draftMessageId, CancellationToken ct)
        {
            Calls.Add($"SendDraft:{draftMessageId}");
            return Task.FromResult(SendSucceeds);
        }

        public Task<string?> GetWebLinkAsync(string messageId, CancellationToken ct)
        {
            Calls.Add($"GetWebLink:{messageId}");
            return Task.FromResult<string?>("https://web/sent");
        }

        public Task<bool> AssignAsync(string messageId, string? internetMessageId, string requestCategory, CancellationToken ct)
        {
            Calls.Add($"Assign:{messageId}:{requestCategory}");
            return Task.FromResult(true);
        }

        public Task<int> TagConversationMembersAsync(string conversationId, string category, CancellationToken ct, DateTimeOffset? receivedOnOrBefore = null)
        {
            Calls.Add($"TagConversationMembers:{conversationId}:{category}");
            return Task.FromResult(0);
        }

        public Task<int> ClearRequestTagsAsync(string requestCategory, CancellationToken ct)
        {
            Calls.Add($"ClearRequestTags:{requestCategory}");
            return Task.FromResult(0);
        }

        private static string Join(IReadOnlyList<MailboxDraftRecipient> recipients) => string.Join(";", recipients.Select(r => r.Email));

        public Task<MailboxPage> ListInboxAsync(string? cursor, int take, bool newestFirst, CancellationToken ct) => throw new NotSupportedException();
        public Task<MailboxPage> ListDiscardedAsync(string? cursor, int take, bool newestFirst, CancellationToken ct) => throw new NotSupportedException();
        public Task<MailboxPage> ListByTagAsync(string tag, string? cursor, int take, CancellationToken ct) => throw new NotSupportedException();
        public Task<MailboxPage> ListTaggedAsync(string? cursor, int take, bool newestFirst, CancellationToken ct) => throw new NotSupportedException();
        public Task<MailboxPage> SearchAsync(string query, int take, CancellationToken ct) => throw new NotSupportedException();
        public Task<MailboxPage> ListConversationAsync(string conversationId, CancellationToken ct) => throw new NotSupportedException();
        public Task<MailboxPage> ListByTagsAsync(IReadOnlyList<string> tags, string? cursor, int take, bool newestFirst, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> RemoveTagAsync(string messageId, string? internetMessageId, string tag, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> DiscardAsync(string messageId, string? internetMessageId, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> RestoreAsync(string messageId, string? internetMessageId, CancellationToken ct) => throw new NotSupportedException();
        public Task<int> RetagAsync(string oldCategory, string newCategory, CancellationToken ct) => throw new NotSupportedException();
        public Task<int> AddAliasTagAsync(string existingCategory, string aliasCategory, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> ListUntaggedIdsInConversationAsync(string conversationId, string category, CancellationToken ct, DateTimeOffset? receivedOnOrBefore = null) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> ListTaggedIdsInConversationAsync(string conversationId, string category, CancellationToken ct) => throw new NotSupportedException();
        public Task<int> UntagConversationMembersAsync(string conversationId, string category, CancellationToken ct) => throw new NotSupportedException();
        public Task<MailboxDraftDeletion> DeleteDraftAsync(string draftMessageId, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class FakeProvider : ILinkableRecordProvider
    {
        public LinkableRecord? Record { get; set; }
        public RecordType Type => RecordType.WorkOrder;
        public IReadOnlyCollection<string> ReferencePrefixes => new[] { "WO" };
        public Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct) => throw new NotSupportedException();
        public Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct) =>
            Task.FromResult(Record is { } record && record.RecordId == recordId ? record : null);
    }

    /// <summary>Stands in for the create-from-message handler: records what it was asked and
    /// writes the request row the rollback and the status move act on.</summary>
    private sealed class FakeCreateRequest : ICommandHandler<CreateRequestFromMessage, Request>
    {
        private readonly JpmsContext context;
        public CreateRequestFromMessage? Received { get; private set; }

        public FakeCreateRequest(JpmsContext context) { this.context = context; }

        public async Task<Request> HandleAsync(CreateRequestFromMessage command, CancellationToken cancellationToken)
        {
            Received = command;
            context.Requests.Add(new RequestEntity
            {
                RequestId = "r7", ProjectId = command.ProjectId, Reference = "REQ-0007", Title = command.Title,
                Status = (int)RequestStatus.NeedsAction, RaisedByEmail = command.RaisedByEmail, RaisedAt = ReceivedAt,
            });
            await context.SaveChangesAsync(cancellationToken);
            return new Request("r7", command.ProjectId, RequestType.General, "REQ-0007", command.Title, command.Description,
                RequestStatus.NeedsAction, null, command.RaisedByEmail, ReceivedAt, null, Number: 7);
        }
    }

    private sealed class NoBlobs : IDrawingBlobStore
    {
        public Task<string> UploadAsync(string projectId, string drawingId, string revisionId, string fileName, string contentType, Stream content, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<DrawingBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken) => Task.FromResult<DrawingBlob?>(null);
        public Task DeleteAsync(string blobRef, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NoPhotos : IProgressPhotoStore
    {
        public Task<string> UploadAsync(string projectId, string progressUpdateId, string photoId, string fileName, string contentType, Stream content, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ProgressPhotoBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken) => Task.FromResult<ProgressPhotoBlob?>(null);
        public Task DeleteAsync(string blobRef, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NoShareStore : IEmailFileShareStore
    {
        public bool IsConfigured => false;
        public Task<EmailFileShareLink?> ShareAsync(string scope, string fileName, string contentType, byte[] content, CancellationToken cancellationToken) => Task.FromResult<EmailFileShareLink?>(null);
    }
}

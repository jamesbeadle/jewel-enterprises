using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Ai.Tools;
using Jewel.JPMS.Api.Features.Ai.Tools.Actions;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.MailboxIntake.Sharing;
using Jewel.JPMS.Api.Features.Procurement.Attachments;
using Jewel.JPMS.Api.Features.Procurement.Commands;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The invite path after the 2026-09-10 connector audit (docs/ai/12-connector-weakness-audit.md
/// §2): eleven firms got the invite twice because the BCC was every tender-list row whatever its
/// status, the result said nothing about attachments, and the context read named unnamed drawings
/// as blanks. Pins: Declined/Won never in the default BCC; recipientIds honoured; attachedFiles
/// populated by both paths; the linked-documents read falls back to the file name; the action is
/// confirm-first and its text tells the model to read the record first.
/// </summary>
public sealed class BidPackageInviteTests
{
    private const string PackageId = "BP-1";
    private static readonly DateTimeOffset AddedAt = new(2026, 9, 1, 9, 0, 0, TimeSpan.Zero);

    // ---- DefaultBccAsync ---------------------------------------------------------------------

    [Fact]
    public async Task DefaultBcc_skipsDeclinedAndWon_andRowsWithoutAnEmail()
    {
        var fixture = await Fixture.CreateAsync();

        var bcc = await fixture.Assembler.DefaultBccAsync(PackageId, CancellationToken.None);

        Assert.Equal(
            new[] { "acme@example.com", "birch@example.com" },
            bcc.Select(r => r.Email).OrderBy(e => e).ToArray());
        Assert.DoesNotContain(bcc, r => r.Email == "declined@example.com");
        Assert.DoesNotContain(bcc, r => r.Email == "won@example.com");
    }

    [Fact]
    public async Task DefaultBcc_honoursRecipientIds_stillRequiringAnEmail_andStillSkippingDeclined()
    {
        var fixture = await Fixture.CreateAsync();

        var only = await fixture.Assembler.DefaultBccAsync(
            PackageId, CancellationToken.None, new[] { "R-BIRCH", "R-NOEMAIL", "R-DECLINED", "not-a-recipient" });

        var recipient = Assert.Single(only);
        Assert.Equal(("birch@example.com", "Birch Ltd"), (recipient.Email, recipient.Name));
    }

    [Fact]
    public async Task DefaultBcc_withAnEmptyRecipientIds_isTheDefaultSet()
    {
        var fixture = await Fixture.CreateAsync();

        var all = await fixture.Assembler.DefaultBccAsync(PackageId, CancellationToken.None, Array.Empty<string>());

        Assert.Equal(2, all.Count);
    }

    // ---- PrepareBidPackageInviteDraftHandler ------------------------------------------------------

    [Fact]
    public async Task PrepareDraft_bccsTheChosenRecipients_andReportsTheAttachedFiles()
    {
        var fixture = await Fixture.CreateAsync();

        var draft = await fixture.PrepareHandler.HandleAsync(
            new PrepareBidPackageInviteDraft(PackageId, "Invitation to tender", "<p>Please price.</p>", new[] { "R-ACME" }),
            CancellationToken.None);

        Assert.Equal(new[] { "acme@example.com" }, draft.Bcc);
        Assert.Equal(new[] { "acme@example.com" }, fixture.Graph.CreatedDraft!.Bcc!.Select(r => r.Email));
        Assert.Equal(new[] { "projects@jewelbb.co.uk" }, fixture.Graph.CreatedDraft!.To.Select(r => r.Email));

        // attachedFiles is the truth about attachments: schedule first, then the T&Cs, then the
        // package's own document, then the linked drawing — and linkedFiles stays the overflow only.
        Assert.Equal(
            new[] { "BPI-0007 - Pricing Schedule.xlsx", "Jewel T&Cs.pdf", "Spec.pdf", "site-plan.pdf" },
            draft.AttachedFiles);
        Assert.Empty(draft.LinkedFiles!);
        Assert.Equal("draft-1", draft.DraftMessageId);
    }

    [Fact]
    public async Task PrepareDraft_withoutRecipientIds_bccsEveryoneStillInTheRunning()
    {
        var fixture = await Fixture.CreateAsync();

        var draft = await fixture.PrepareHandler.HandleAsync(
            new PrepareBidPackageInviteDraft(PackageId, "Invitation to tender", "<p>Please price.</p>"),
            CancellationToken.None);

        Assert.Equal(new[] { "acme@example.com", "birch@example.com" }, draft.Bcc.OrderBy(e => e));
    }

    [Fact]
    public async Task PrepareDraft_refusesWhenNoneOfTheRecipientIdsResolve()
    {
        var fixture = await Fixture.CreateAsync();

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.PrepareHandler.HandleAsync(
            new PrepareBidPackageInviteDraft(PackageId, "s", "<p>b</p>", new[] { "R-DECLINED", "Acme Ltd" }),
            CancellationToken.None));

        Assert.Contains("recipientId", refusal.Message);
        Assert.Contains("tenderList[].recipientId", refusal.Message);
        Assert.Null(fixture.Graph.CreatedDraft);
    }

    // ---- SendBidPackageInviteHandler -------------------------------------------------------------

    [Fact]
    public async Task Send_reportsTheAttachedFiles()
    {
        var fixture = await Fixture.CreateAsync();

        var outcome = await fixture.SendHandler.HandleAsync(
            new SendBidPackageInvite(PackageId, "Invitation to tender", "<p>Please price.</p>", Bcc: "acme@example.com"),
            CancellationToken.None);

        Assert.True(outcome.Sent);
        Assert.Equal(
            new[] { "BPI-0007 - Pricing Schedule.xlsx", "Jewel T&Cs.pdf", "Spec.pdf", "site-plan.pdf" },
            outcome.AttachedFiles);
        Assert.Empty(outcome.LinkedFiles);
    }

    // ---- get_bid_package_context reads ---------------------------------------------------------------

    [Fact]
    public async Task LinkedDocumentsOf_fallsBackToTheCurrentRevisionsFileName()
    {
        var fixture = await Fixture.CreateAsync();

        var rows = await BidPackageContextReads.LinkedDocumentsOf(fixture.Context, PackageId, CancellationToken.None);

        var unnamed = Assert.Single(rows);
        var json = System.Text.Json.JsonSerializer.SerializeToElement(unnamed);
        Assert.Equal("", json.GetProperty("drawingCode").GetString());
        Assert.Equal("site-plan.pdf", json.GetProperty("title").GetString());
        Assert.Equal("site-plan.pdf", json.GetProperty("fileName").GetString());
        Assert.Equal("D-1", json.GetProperty("drawingId").GetString());
    }

    [Fact]
    public async Task LinkedDocumentsOf_keepsCodeAndTitleWhenTheDrawingHasThem()
    {
        var fixture = await Fixture.CreateAsync();
        fixture.Context.Drawings.Add(new DrawingEntity { DrawingId = "D-2", ProjectId = "P-1", DrawingCode = "A-101", Title = "Ground floor" });
        fixture.Context.DrawingRevisions.Add(new DrawingRevisionEntity { DrawingRevisionId = "REV-2", DrawingId = "D-2", FileName = "A-101-P2.pdf", ReceivedAt = AddedAt });
        fixture.Context.BidPackageDrawings.Add(new BidPackageDrawingEntity { BidPackageDrawingId = "L-2", BidPackageId = PackageId, DrawingId = "D-2", LinkedAt = AddedAt.AddDays(1) });
        await fixture.Context.SaveChangesAsync();

        var rows = await BidPackageContextReads.LinkedDocumentsOf(fixture.Context, PackageId, CancellationToken.None);

        var named = System.Text.Json.JsonSerializer.SerializeToElement(rows[0]);
        Assert.Equal("A-101", named.GetProperty("drawingCode").GetString());
        Assert.Equal("Ground floor", named.GetProperty("title").GetString());
        Assert.Equal("A-101-P2.pdf", named.GetProperty("fileName").GetString());
    }

    [Fact]
    public async Task TenderListOf_namesTheAddedToListStamp_notAnInviteSent()
    {
        var fixture = await Fixture.CreateAsync();

        var rows = await BidPackageContextReads.TenderListOf(fixture.Context, PackageId, CancellationToken.None);

        var row = System.Text.Json.JsonSerializer.SerializeToElement(rows[0]);
        Assert.True(row.TryGetProperty("addedToListAt", out _));
        Assert.False(row.TryGetProperty("invitedAt", out _));
        Assert.True(row.TryGetProperty("recipientId", out _));
    }

    // ---- The connector action ----------------------------------------------------------------------

    [Fact]
    public void PrepareInviteDraftAction_isConfirmFirst_andTellsTheModelToReadTheRecord()
    {
        var action = AiActionRegistry.Find("prepare_bid_package_invite_draft")!;

        Assert.True(action.RequiresConfirmation);
        var text = action.Description + " " + action.Notes;
        Assert.Contains("Declined", text);
        Assert.Contains("read_record_emails", action.Notes);
        Assert.Contains("tenderList[].recipientId", action.Notes);
        Assert.Contains("attachedFiles", text);
        Assert.Contains("recipientIds", System.Text.Json.JsonSerializer.Serialize(AiActionSchema.InputSchema(action)));
    }

    // ---- Fixture -----------------------------------------------------------------------------------

    /// <summary>One package with four rows on its tender list — Acme (on list), Birch (Responded),
    /// a Declined firm, the Won firm and a row with no directory email — one tender document and
    /// one linked, uncoded drawing. Storage fakes return small byte arrays; Graph records the draft.</summary>
    private sealed class Fixture
    {
        public JpmsContext Context { get; }
        public RecordingGraph Graph { get; } = new();
        public BidPackageInviteMailAssembler Assembler { get; }
        public PrepareBidPackageInviteDraftHandler PrepareHandler { get; }
        public SendBidPackageInviteHandler SendHandler { get; }

        private Fixture()
        {
            Context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
                .UseInMemoryDatabase($"invite-{Guid.NewGuid():N}")
                .Options);
            Assembler = new BidPackageInviteMailAssembler(Context, new Blobs(), new NoShareStore(), new Attachments(), new Terms());
            var options = new MailboxIntakeOptions { Mailbox = "projects@jewelbb.co.uk" };
            PrepareHandler = new PrepareBidPackageInviteDraftHandler(Context, Graph, options, Assembler);
            SendHandler = new SendBidPackageInviteHandler(Context, Graph, options, Assembler);
        }

        public static async Task<Fixture> CreateAsync()
        {
            var fixture = new Fixture();
            var db = fixture.Context;
            db.BidPackages.Add(new BidPackageEntity { BidPackageId = PackageId, ProjectId = "P-1", Title = "Roofing", Trade = "Roofing", Number = 7, CreatedAt = AddedAt });

            db.Subcontractors.Add(new SubcontractorEntity { SubcontractorId = "S-ACME", CompanyName = "Acme Ltd", ContactEmail = "acme@example.com" });
            db.Subcontractors.Add(new SubcontractorEntity { SubcontractorId = "S-BIRCH", CompanyName = "Birch Ltd", ContactEmail = "birch@example.com" });
            db.Subcontractors.Add(new SubcontractorEntity { SubcontractorId = "S-DECLINED", CompanyName = "Declined Ltd", ContactEmail = "declined@example.com" });
            db.Subcontractors.Add(new SubcontractorEntity { SubcontractorId = "S-WON", CompanyName = "Won Ltd", ContactEmail = "won@example.com" });
            db.Subcontractors.Add(new SubcontractorEntity { SubcontractorId = "S-NOEMAIL", CompanyName = "Silent Ltd", ContactEmail = "" });

            db.BidPackageRecipients.AddRange(
                Row("R-ACME", "S-ACME", BidPackageRecipientStatus.Invited),
                Row("R-BIRCH", "S-BIRCH", BidPackageRecipientStatus.Responded),
                Row("R-DECLINED", "S-DECLINED", BidPackageRecipientStatus.Declined),
                Row("R-WON", "S-WON", BidPackageRecipientStatus.Won),
                Row("R-NOEMAIL", "S-NOEMAIL", BidPackageRecipientStatus.Invited));

            db.BidPackageAttachments.Add(new BidPackageAttachmentEntity
            {
                BidPackageAttachmentId = "A-1", BidPackageId = PackageId, ProjectId = "P-1",
                FileName = "Spec.pdf", ContentType = "application/pdf", BlobRef = "blob:spec", AddedAt = AddedAt
            });

            // An uncoded, untitled drawing — a file dropped on the register and linked as-is.
            db.Drawings.Add(new DrawingEntity { DrawingId = "D-1", ProjectId = "P-1", DrawingCode = "", Title = "" });
            db.DrawingRevisions.Add(new DrawingRevisionEntity
            {
                DrawingRevisionId = "REV-1", DrawingId = "D-1", FileName = "site-plan.pdf",
                ReceivedAt = AddedAt, BlobRef = "blob:site-plan", ContentType = "application/pdf"
            });
            db.BidPackageDrawings.Add(new BidPackageDrawingEntity { BidPackageDrawingId = "L-1", BidPackageId = PackageId, DrawingId = "D-1", LinkedAt = AddedAt });

            await db.SaveChangesAsync();
            return fixture;
        }

        private static BidPackageRecipientEntity Row(string id, string subcontractorId, BidPackageRecipientStatus status) =>
            new() { RecipientId = id, BidPackageId = PackageId, SubcontractorId = subcontractorId, Status = (int)status, InvitedAt = AddedAt };
    }

    private sealed class RecordingGraph : IMailboxGraphClient
    {
        public MailboxDraftMessage? CreatedDraft { get; private set; }

        public Task<MailboxDraft?> CreateDraftAsync(MailboxDraftMessage draft, CancellationToken ct)
        {
            CreatedDraft = draft;
            return Task.FromResult<MailboxDraft?>(new MailboxDraft("draft-1", "https://web/draft-1"));
        }

        public Task<bool> SendDraftAsync(string draftMessageId, CancellationToken ct) => Task.FromResult(true);
        public Task<string?> GetWebLinkAsync(string messageId, CancellationToken ct) => Task.FromResult<string?>("https://web/sent");

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
        public Task<bool> AssignAsync(string messageId, string? internetMessageId, string requestCategory, CancellationToken ct) => throw new NotSupportedException();
        public Task<int> ClearRequestTagsAsync(string requestCategory, CancellationToken ct) => throw new NotSupportedException();
        public Task<int> RetagAsync(string oldCategory, string newCategory, CancellationToken ct) => throw new NotSupportedException();
        public Task<int> AddAliasTagAsync(string existingCategory, string aliasCategory, CancellationToken ct) => throw new NotSupportedException();
        public Task<MailboxSnapshot?> GetSnapshotAsync(string messageId, string? internetMessageId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> ListUntaggedIdsInConversationAsync(string conversationId, string category, CancellationToken ct, DateTimeOffset? receivedOnOrBefore = null) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> ListTaggedIdsInConversationAsync(string conversationId, string category, CancellationToken ct) => throw new NotSupportedException();
        public Task<int> TagConversationMembersAsync(string conversationId, string category, CancellationToken ct, DateTimeOffset? receivedOnOrBefore = null) => throw new NotSupportedException();
        public Task<int> UntagConversationMembersAsync(string conversationId, string category, CancellationToken ct) => throw new NotSupportedException();
        public Task<MailboxReplyDraft?> CreateReplyDraftAsync(MailboxReplyDraftMessage reply, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> UpdateDraftEnvelopeAsync(string draftMessageId, IReadOnlyList<MailboxDraftRecipient> to, IReadOnlyList<MailboxDraftRecipient> cc, IReadOnlyList<MailboxDraftRecipient> bcc, string subject, CancellationToken ct) => throw new NotSupportedException();
        public Task<MailboxDraftDeletion> DeleteDraftAsync(string draftMessageId, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class Blobs : IDrawingBlobStore
    {
        public Task<string> UploadAsync(string projectId, string drawingId, string revisionId, string fileName, string contentType, Stream content, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<DrawingBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken) =>
            Task.FromResult<DrawingBlob?>(new DrawingBlob(new MemoryStream(new byte[] { 1, 2, 3 }), "application/pdf", 3));
        public Task DeleteAsync(string blobRef, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Attachments : IBidPackageAttachmentStore
    {
        public Task<string> UploadAsync(string projectId, string bidPackageId, string attachmentId, string fileName, string contentType, Stream content, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<BidPackageAttachmentBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken) =>
            Task.FromResult<BidPackageAttachmentBlob?>(new BidPackageAttachmentBlob(new MemoryStream(new byte[] { 4, 5 }), "application/pdf", 2));
        public Task DeleteAsync(string blobRef, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Terms : ICompanyTenderTermsStore
    {
        public bool IsConfigured => true;
        public Task<CompanyTenderTermsInfo?> GetInfoAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<CompanyTenderTermsFile?> OpenAsync(CancellationToken cancellationToken) =>
            Task.FromResult<CompanyTenderTermsFile?>(new CompanyTenderTermsFile(new byte[] { 9 }, "Jewel T&Cs.pdf"));
        public Task<CompanyTenderTermsInfo> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class NoShareStore : IEmailFileShareStore
    {
        public bool IsConfigured => false;
        public Task<EmailFileShareLink?> ShareAsync(string scope, string fileName, string contentType, byte[] content, CancellationToken cancellationToken) => Task.FromResult<EmailFileShareLink?>(null);
    }
}

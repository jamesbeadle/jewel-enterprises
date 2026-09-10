using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.MailboxIntake.Sharing;
using Jewel.JPMS.Api.Features.Procurement.Attachments;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>
/// Everything that travels with a tender invite, planned once and shared by BOTH invite paths —
/// the stage-in-Drafts flow (PrepareBidPackageInviteDraft) and the in-app send
/// (SendBidPackageInvite) — so the two can never disagree about what a tenderer receives.
///
/// The order is deliberate: the generated pricing schedule leads (the one file the tender can't
/// run without must never overflow into a link), then the company's standard Terms &amp;
/// Conditions (Admin → System — skipped silently when none is uploaded), then the package's own
/// tender documents, then the linked drawings. Anything that would push the email past the ~25 MB
/// Exchange ceiling is copied to the email-shares container and travels as a 7-day download link
/// appended to the body instead.
/// </summary>
// Public because the (public) handler constructors take it — CS0051 otherwise.
public sealed class BidPackageInviteMailAssembler
{
    private readonly JpmsContext context;
    private readonly IDrawingBlobStore blobStore;
    private readonly IEmailFileShareStore shareStore;
    private readonly IBidPackageAttachmentStore attachmentStore;
    private readonly ICompanyTenderTermsStore termsStore;

    public BidPackageInviteMailAssembler(
        JpmsContext context, IDrawingBlobStore blobStore, IEmailFileShareStore shareStore,
        IBidPackageAttachmentStore attachmentStore, ICompanyTenderTermsStore termsStore)
    {
        this.context = context; this.blobStore = blobStore; this.shareStore = shareStore;
        this.attachmentStore = attachmentStore; this.termsStore = termsStore;
    }

    public sealed record InvitePlan(
        IReadOnlyList<MailboxDraftAttachment> Attach,
        string HtmlBody,
        IReadOnlyList<string> LinkedFiles);

    public async Task<InvitePlan> PlanAsync(BidPackageEntity package, string htmlBody, CancellationToken cancellationToken)
    {
        var files = new List<MailboxDraftAttachment>
        {
            await BuildPricingScheduleAsync(package, cancellationToken)
        };

        // The company's standard terms — one document, company-wide (Admin → System). An invite
        // without them still goes out: their absence is an admin gap, not a reason to block a
        // tender, and the System panel shows it plainly.
        var terms = await termsStore.OpenAsync(cancellationToken);
        if (terms is not null)
            files.Add(new MailboxDraftAttachment(terms.FileName, "application/pdf", terms.Content));

        files.AddRange(await LoadPackageAttachmentsAsync(package.BidPackageId, cancellationToken));
        files.AddRange(await LoadDrawingAttachmentsAsync(package.BidPackageId, cancellationToken));

        var plan = EmailAttachmentPlanner.Split(files);
        var linkedFiles = new List<string>();

        if (plan.ToLink.Count > 0)
        {
            if (!shareStore.IsConfigured)
                throw new InvalidOperationException(
                    $"The tender documents total {EmailAttachmentPlanner.FormatSize(files.Sum(a => a.Content.LongLength))} — " +
                    "more than fits one email — and the file-share store isn't configured, so download links can't be " +
                    "created. Unlink some drawings or remove attachments, or configure DrawingsStorage:ConnectionString.");

            var links = new List<EmailFileShareLink>();
            foreach (var file in plan.ToLink)
            {
                var link = await shareStore.ShareAsync(
                        package.Reference, file.FileName, file.ContentType, file.Content, cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"A download link couldn't be created for {file.FileName}. Nothing was drafted — try again.");
                links.Add(link);
                linkedFiles.Add(file.FileName);
            }
            htmlBody += EmailAttachmentPlanner.LinksHtmlBlock(links, "Tender documents — download links");
        }

        return new InvitePlan(plan.Attach, htmlBody, linkedFiles);
    }

    /// <summary>
    /// The BCC list both paths default to: the tender-list recipients still in the running — on the
    /// list (Invited) or Responded; Declined and Won are skipped — that have a directory email.
    /// With <paramref name="recipientIds"/> (non-empty) only those BidPackageRecipient ids are
    /// included, still requiring a directory email (2026-09-10 — the connector re-invited eleven
    /// firms because the list was every row regardless of status). Ids that resolve to nothing are
    /// ignored; the caller decides what an empty result means.
    /// </summary>
    public async Task<IReadOnlyList<MailboxDraftRecipient>> DefaultBccAsync(
        string bidPackageId, CancellationToken cancellationToken, IReadOnlyCollection<string>? recipientIds = null)
    {
        var declined = (int)BidPackageRecipientStatus.Declined;
        var won = (int)BidPackageRecipientStatus.Won;
        var wanted = recipientIds is { Count: > 0 }
            ? recipientIds.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()).Distinct().ToList()
            : null;

        var query =
            from recipient in context.BidPackageRecipients
            where recipient.BidPackageId == bidPackageId
                && recipient.Status != declined && recipient.Status != won
            join sub in context.Subcontractors on recipient.SubcontractorId equals sub.SubcontractorId
            where sub.ContactEmail != null && sub.ContactEmail != ""
            select new { recipient.RecipientId, sub.ContactEmail, sub.CompanyName };

        if (wanted is not null)
            query = query.Where(row => wanted.Contains(row.RecipientId));

        var bcc = await query.ToListAsync(cancellationToken);

        return bcc
            .GroupBy(r => r.ContactEmail, StringComparer.OrdinalIgnoreCase)
            .Select(g => new MailboxDraftRecipient(g.Key, g.First().CompanyName))
            .ToList();
    }

    // The pricing schedule the tenderer completes and returns: the package's line items grouped by
    // trade, each carrying the cost-code / VO reference of its commercial home (the same column the
    // firm's hand-built tender sheets lead with). Generated fresh on every draft so it always
    // matches the current scope.
    private async Task<MailboxDraftAttachment> BuildPricingScheduleAsync(
        BidPackageEntity package, CancellationToken cancellationToken)
    {
        var lines = await context.BidPackageLineItems
            .Where(line => line.BidPackageId == package.BidPackageId)
            .OrderBy(line => line.SortOrder)
            .ToListAsync(cancellationToken);

        // Variation-covered lines show the variation's reference (V18) rather than a cost code —
        // exactly how the hand-built sheets mix "0012" and "V05" in one column.
        var variationIds = lines
            .Where(line => !string.IsNullOrWhiteSpace(line.VariationOrderId))
            .Select(line => line.VariationOrderId!)
            .Distinct()
            .ToList();
        var variationRefs = variationIds.Count == 0
            ? new Dictionary<string, string>()
            : await context.VariationOrders
                .Where(order => variationIds.Contains(order.VariationOrderId))
                .ToDictionaryAsync(
                    order => order.VariationOrderId,
                    order => !string.IsNullOrWhiteSpace(order.VariationRef)
                        ? order.VariationRef!
                        : order.Number > 0 ? $"V{order.Number}" : "",
                    cancellationToken);

        var sections = lines
            .GroupBy(line => string.IsNullOrWhiteSpace(line.Trade) ? package.Trade : line.Trade.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .Select(group => new PricingScheduleWorkbook.ScheduleSection(
                group.Key,
                group.Select(line => new PricingScheduleWorkbook.ScheduleLine(
                        line.VariationOrderId is not null && variationRefs.TryGetValue(line.VariationOrderId, out var vRef)
                            ? vRef
                            : line.CostCode,
                        line.Description, line.Quantity, line.Unit))
                    .ToList()))
            .ToList();

        var project = await context.Projects.FindAsync(new object[] { package.ProjectId }, cancellationToken);
        var bytes = PricingScheduleWorkbook.Write(
            package.Reference, package.Title, project?.Name ?? "",
            package.SpecificationSummary, package.MaterialsApplicable, sections);

        return new MailboxDraftAttachment(
            $"{package.Reference} - Pricing Schedule.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            bytes);
    }

    // The package's own tender documents — the supplier-facing attachment register. A file whose
    // blob can't be opened is skipped rather than blocking the draft, same as drawings.
    private async Task<IReadOnlyList<MailboxDraftAttachment>> LoadPackageAttachmentsAsync(
        string bidPackageId, CancellationToken cancellationToken)
    {
        var rows = await context.BidPackageAttachments
            .AsNoTracking()
            .Where(row => row.BidPackageId == bidPackageId)
            .OrderBy(row => row.AddedAt)
            .ToListAsync(cancellationToken);

        var attachments = new List<MailboxDraftAttachment>();
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.BlobRef)) continue;
            var blob = await attachmentStore.OpenAsync(row.BlobRef, cancellationToken);
            if (blob is null) continue;

            await using var stream = blob.Content;
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            attachments.Add(new MailboxDraftAttachment(
                row.FileName,
                string.IsNullOrWhiteSpace(row.ContentType) ? "application/octet-stream" : row.ContentType,
                buffer.ToArray()));
        }
        return attachments;
    }

    // One attachment per linked drawing: its latest approved revision, or — when nothing is approved
    // yet — the newest uploaded revision. Drawings whose file can't be opened are skipped rather than
    // blocking the draft; the linked list on the package remains the source of truth.
    private async Task<IReadOnlyList<MailboxDraftAttachment>> LoadDrawingAttachmentsAsync(
        string bidPackageId, CancellationToken cancellationToken)
    {
        var revisions = await (
            from link in context.BidPackageDrawings
            where link.BidPackageId == bidPackageId
            join revision in context.DrawingRevisions on link.DrawingId equals revision.DrawingId
            where revision.BlobRef != null
            select revision)
            .ToListAsync(cancellationToken);

        var attachments = new List<MailboxDraftAttachment>();
        foreach (var chosen in revisions
            .GroupBy(r => r.DrawingId)
            .Select(g => g
                .OrderByDescending(r => r.ApprovalStatus == (int)DrawingApprovalStatus.Approved)
                .ThenByDescending(r => r.ReceivedAt)
                .First()))
        {
            var blob = await blobStore.OpenAsync(chosen.BlobRef!, cancellationToken);
            if (blob is null) continue;

            await using var stream = blob.Content;
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            attachments.Add(new MailboxDraftAttachment(
                chosen.FileName,
                string.IsNullOrWhiteSpace(chosen.ContentType) ? "application/octet-stream" : chosen.ContentType!,
                buffer.ToArray()));
        }
        return attachments;
    }
}

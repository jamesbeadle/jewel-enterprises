namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>The lists get_bid_package_context returns, each row carrying the id the matching
/// connector action takes — a row without its id is a row the assistant can read but never act on.</summary>
internal static class BidPackageContextReads
{
    public static async Task<IReadOnlyList<object>> LineItemsOf(JpmsContext db, string bidPackageId, CancellationToken ct) =>
        await db.BidPackageLineItems.AsNoTracking()
            .Where(row => row.BidPackageId == bidPackageId)
            .OrderBy(row => row.SortOrder)
            .Select(row => new
            {
                lineItemId = row.LineItemId,
                row.Trade,
                row.Description,
                row.Unit,
                row.Quantity,
                row.CostCode,
                coverage = ((BidPackageLineCoverage)row.Coverage).ToString(),
                row.BoqLineItemId,
                row.VariationOrderId
            })
            .ToListAsync(ct);

    public static async Task<IReadOnlyList<object>> TenderListOf(JpmsContext db, string bidPackageId, CancellationToken ct) =>
        await (
            from recipient in db.BidPackageRecipients.AsNoTracking()
            where recipient.BidPackageId == bidPackageId
            join sub in db.Subcontractors.AsNoTracking()
                on recipient.SubcontractorId equals sub.SubcontractorId into subs
            from sub in subs.DefaultIfEmpty()
            orderby recipient.InvitedAt
            select new
            {
                recipientId = recipient.RecipientId,
                subcontractorId = recipient.SubcontractorId,
                company = sub != null ? sub.CompanyName : recipient.SubcontractorId,
                status = ((BidPackageRecipientStatus)recipient.Status).ToString(),
                // The stamp of the row being ADDED to the list (the entity's InvitedAt) — not
                // evidence an invite email went; read_record_emails has the sent copy.
                addedToListAt = recipient.InvitedAt,
                respondedAt = recipient.RespondedAt
            })
            .ToListAsync(ct);

    public static async Task<IReadOnlyList<object>> QuotesOf(JpmsContext db, string bidPackageId, CancellationToken ct) =>
        await (
            from quote in db.Quotes.AsNoTracking()
            where quote.BidPackageId == bidPackageId
            join sub in db.Subcontractors.AsNoTracking()
                on quote.SubcontractorId equals sub.SubcontractorId into subs
            from sub in subs.DefaultIfEmpty()
            orderby quote.ReceivedAt descending
            select new
            {
                quoteId = quote.QuoteId,
                subcontractorId = quote.SubcontractorId,
                company = sub != null ? sub.CompanyName : quote.SubcontractorId,
                value = quote.Value,
                notes = quote.Notes,
                receivedAt = quote.ReceivedAt,
                isDeclined = quote.IsDeclined
            })
            .ToListAsync(ct);

    public static async Task<IReadOnlyList<object>> TenderDocumentsOf(JpmsContext db, string bidPackageId, CancellationToken ct) =>
        await db.BidPackageAttachments.AsNoTracking()
            .Where(row => row.BidPackageId == bidPackageId)
            .Select(row => new { bidPackageAttachmentId = row.BidPackageAttachmentId, row.FileName, row.ContentType })
            .ToListAsync(ct);

    // A linked document is named by its code and title; a drawing that has neither yet (a file
    // dropped on the register, never coded) is named by its current revision's file name — the
    // same rule the register and the invite's attachment list follow — so the model never lists
    // "" "" for a document that will travel with the invite. The current revision is the latest
    // approved one, else the newest received, exactly as BidPackageInviteMailAssembler attaches.
    public static async Task<IReadOnlyList<object>> LinkedDocumentsOf(JpmsContext db, string bidPackageId, CancellationToken ct)
    {
        var drawings = await (
            from link in db.BidPackageDrawings.AsNoTracking()
            where link.BidPackageId == bidPackageId
            join drawing in db.Drawings.AsNoTracking() on link.DrawingId equals drawing.DrawingId
            orderby link.LinkedAt descending
            select new
            {
                drawing.DrawingId,
                drawing.DrawingCode,
                drawing.Title,
                drawing.CurrentApprovedRevisionLabel
            })
            .ToListAsync(ct);
        if (drawings.Count == 0) return Array.Empty<object>();

        var drawingIds = drawings.Select(d => d.DrawingId).ToList();
        var revisions = await db.DrawingRevisions.AsNoTracking()
            .Where(revision => drawingIds.Contains(revision.DrawingId))
            .Select(revision => new { revision.DrawingId, revision.FileName, revision.ApprovalStatus, revision.ReceivedAt })
            .ToListAsync(ct);
        var currentFileNames = revisions
            .GroupBy(revision => revision.DrawingId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(revision => revision.ApprovalStatus == (int)DrawingApprovalStatus.Approved)
                    .ThenByDescending(revision => revision.ReceivedAt)
                    .First().FileName);

        return drawings
            .Select(drawing =>
            {
                currentFileNames.TryGetValue(drawing.DrawingId, out var fileName);
                var unnamed = string.IsNullOrWhiteSpace(drawing.DrawingCode) && string.IsNullOrWhiteSpace(drawing.Title);
                return (object)new
                {
                    drawingId = drawing.DrawingId,
                    drawingCode = drawing.DrawingCode,
                    title = unnamed ? fileName ?? "" : drawing.Title,
                    currentRevision = drawing.CurrentApprovedRevisionLabel,
                    fileName
                };
            })
            .ToList();
    }
}

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
                invitedAt = recipient.InvitedAt,
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

    public static async Task<IReadOnlyList<object>> LinkedDocumentsOf(JpmsContext db, string bidPackageId, CancellationToken ct) =>
        await (
            from link in db.BidPackageDrawings.AsNoTracking()
            where link.BidPackageId == bidPackageId
            join drawing in db.Drawings.AsNoTracking() on link.DrawingId equals drawing.DrawingId
            orderby link.LinkedAt descending
            select new
            {
                drawingId = drawing.DrawingId,
                drawing.DrawingCode,
                drawing.Title,
                currentRevision = drawing.CurrentApprovedRevisionLabel
            })
            .ToListAsync(ct);
}

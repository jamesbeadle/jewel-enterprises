using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement;

// The one place a package's email verdicts are found and written, shared by the Discard/Restore
// command and by SaveExtractedQuote (which stamps Extracted). Matching is by Graph message id
// first, then by the stable internet id — Graph ids change when a message moves folder or the
// mailbox re-syncs, and a verdict must survive that.
internal static class BidPackageEmailDispositionStore
{
    public static async Task<BidPackageEmailDispositionEntity?> FindAsync(
        JpmsContext context, string bidPackageId, string messageId, string? internetMessageId, CancellationToken cancellationToken)
    {
        var byGraphId = await context.BidPackageEmailDispositions
            .FirstOrDefaultAsync(row => row.BidPackageId == bidPackageId && row.MessageId == messageId, cancellationToken);
        if (byGraphId is not null || string.IsNullOrWhiteSpace(internetMessageId))
            return byGraphId;
        return await context.BidPackageEmailDispositions
            .FirstOrDefaultAsync(row => row.BidPackageId == bidPackageId && row.InternetMessageId == internetMessageId, cancellationToken);
    }

    // Records the verdict on one email, replacing any earlier one; the row's ids are refreshed to
    // the ones just seen so the next match is by the cheap Graph id again.
    public static async Task UpsertAsync(
        JpmsContext context, string bidPackageId, string messageId, string? internetMessageId,
        BidPackageEmailOutcome outcome, string? quoteId, string note, string setByEmail, CancellationToken cancellationToken)
    {
        var row = await FindAsync(context, bidPackageId, messageId, internetMessageId, cancellationToken);
        if (row is null)
        {
            row = new BidPackageEmailDispositionEntity
            {
                BidPackageEmailDispositionId = Guid.NewGuid().ToString("N"),
                BidPackageId = bidPackageId
            };
            context.BidPackageEmailDispositions.Add(row);
        }
        row.MessageId = messageId;
        row.InternetMessageId = string.IsNullOrWhiteSpace(internetMessageId) ? row.InternetMessageId : internetMessageId;
        row.Outcome = (int)outcome;
        row.QuoteId = quoteId;
        row.Note = note.Length > 1024 ? note[..1024] : note;
        row.SetByEmail = setByEmail;
        row.SetAt = DateTimeOffset.UtcNow;
    }

    public static async Task<IReadOnlyList<BidPackageEmailDisposition>> ListAsync(
        JpmsContext context, string bidPackageId, CancellationToken cancellationToken)
    {
        var rows = await context.BidPackageEmailDispositions
            .AsNoTracking()
            .Where(row => row.BidPackageId == bidPackageId)
            .OrderBy(row => row.SetAt)
            .ToListAsync(cancellationToken);
        return rows.Select(row => row.ToModel()).ToList();
    }
}

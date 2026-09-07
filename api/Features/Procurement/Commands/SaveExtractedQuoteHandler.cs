using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Commits a reviewed tender submission. A subcontractor has ONE live submission per package: any
// previous quote of theirs (and its lines) is replaced, so re-extracting a corrected email simply
// supersedes the earlier read. Quote.Value is the sum of the accepted line totals. Marks the
// recipient Responded and moves an Inviting package to QuotesReceived.
public sealed class SaveExtractedQuoteHandler : ICommandHandler<SaveExtractedQuote, Quote>
{
    private readonly JpmsContext context;

    public SaveExtractedQuoteHandler(JpmsContext context) { this.context = context; }

    public async Task<Quote> HandleAsync(SaveExtractedQuote command, CancellationToken cancellationToken)
    {
        var package = await context.BidPackages.FindAsync(new object[] { command.BidPackageId }, cancellationToken);
        if (package is null) throw new InvalidOperationException($"Bid package {command.BidPackageId} not found.");

        // Replace any earlier submission from this subcontractor.
        var previous = await context.Quotes
            .Where(q => q.BidPackageId == command.BidPackageId && q.SubcontractorId == command.SubcontractorId)
            .ToListAsync(cancellationToken);
        if (previous.Count > 0)
        {
            var previousIds = previous.Select(q => q.QuoteId).ToList();
            var previousLines = await context.QuoteLineItems
                .Where(line => previousIds.Contains(line.QuoteId))
                .ToListAsync(cancellationToken);
            context.QuoteLineItems.RemoveRange(previousLines);
            context.Quotes.RemoveRange(previous);
        }

        var quote = new QuoteEntity
        {
            QuoteId = ProcurementIdentifierFactory.NextQuoteId(),
            BidPackageId = command.BidPackageId,
            SubcontractorId = command.SubcontractorId,
            Value = command.Lines.Sum(line => line.Total),
            Notes = command.Notes.Length > 1024 ? command.Notes[..1024] : command.Notes,
            ReceivedAt = DateTimeOffset.UtcNow,
            IsDeclined = false
        };
        context.Quotes.Add(quote);

        foreach (var line in command.Lines)
        {
            context.QuoteLineItems.Add(new QuoteLineItemEntity
            {
                QuoteLineItemId = Guid.NewGuid().ToString("N"),
                QuoteId = quote.QuoteId,
                BidPackageLineItemId = string.IsNullOrWhiteSpace(line.BidPackageLineItemId) ? null : line.BidPackageLineItemId,
                Description = line.Description.Length > 512 ? line.Description[..512] : line.Description,
                Unit = line.Unit.Length > 32 ? line.Unit[..32] : line.Unit,
                Quantity = line.Quantity,
                Rate = line.Rate,
                Total = line.Total
            });
        }

        var recipient = await context.BidPackageRecipients
            .FirstOrDefaultAsync(r => r.BidPackageId == command.BidPackageId && r.SubcontractorId == command.SubcontractorId, cancellationToken);
        if (recipient is not null && recipient.Status == (int)BidPackageRecipientStatus.Invited)
        {
            recipient.Status = (int)BidPackageRecipientStatus.Responded;
            recipient.RespondedAt = DateTimeOffset.UtcNow;
        }

        if (package.Status is (int)BidPackageStatus.Draft or (int)BidPackageStatus.Inviting)
            package.Status = (int)BidPackageStatus.QuotesReceived;

        // Extracted from a tagged email: record that email's verdict as Extracted → this quote, so
        // the Submissions tab shows it as done rather than offering to extract it again. Any earlier
        // verdict on the email (a Discard someone reversed by extracting after all) is replaced.
        if (!string.IsNullOrWhiteSpace(command.SourceMessageId))
        {
            await BidPackageEmailDispositionStore.UpsertAsync(
                context, command.BidPackageId, command.SourceMessageId, command.SourceInternetMessageId,
                BidPackageEmailOutcome.Extracted, quote.QuoteId, note: "", setByEmail: command.SavedByEmail, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        return quote.ToModel();
    }
}

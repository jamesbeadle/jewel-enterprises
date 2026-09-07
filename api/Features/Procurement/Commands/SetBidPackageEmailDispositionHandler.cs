using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Records "not a tender" on one of the package's tagged emails (Discarded), or clears the verdict
// (Pending = Restore). Touches nothing in the mailbox — the tag, and so the Emails tab, are
// unchanged; only the Submissions tab's reading of the email moves. Returns the package's full
// list of verdicts so the caller can re-render without a second read.
public sealed class SetBidPackageEmailDispositionHandler
    : ICommandHandler<SetBidPackageEmailDisposition, IReadOnlyList<BidPackageEmailDisposition>>
{
    private readonly JpmsContext context;

    public SetBidPackageEmailDispositionHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<BidPackageEmailDisposition>> HandleAsync(
        SetBidPackageEmailDisposition command, CancellationToken cancellationToken)
    {
        var package = await context.BidPackages.FindAsync(new object[] { command.BidPackageId }, cancellationToken);
        if (package is null) throw new InvalidOperationException($"Bid package {command.BidPackageId} not found.");

        if (command.Outcome == BidPackageEmailOutcome.Pending)
        {
            var existing = await BidPackageEmailDispositionStore.FindAsync(
                context, command.BidPackageId, command.MessageId, command.InternetMessageId, cancellationToken);
            if (existing is not null) context.BidPackageEmailDispositions.Remove(existing);
        }
        else
        {
            await BidPackageEmailDispositionStore.UpsertAsync(
                context, command.BidPackageId, command.MessageId, command.InternetMessageId,
                command.Outcome, quoteId: null, command.Note ?? "", command.SetByEmail, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        return await BidPackageEmailDispositionStore.ListAsync(context, command.BidPackageId, cancellationToken);
    }
}

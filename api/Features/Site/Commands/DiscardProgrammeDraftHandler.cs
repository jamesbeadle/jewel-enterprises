using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

// Closes the draft without touching the programme. Kept, not deleted: it is the record of what
// the claim proposed and that somebody chose not to take it.
public sealed class DiscardProgrammeDraftHandler : ICommandHandler<DiscardProgrammeDraft, ProgrammeDraft>
{
    private readonly JpmsContext context;
    public DiscardProgrammeDraftHandler(JpmsContext context) { this.context = context; }

    public async Task<ProgrammeDraft> HandleAsync(DiscardProgrammeDraft command, CancellationToken cancellationToken)
    {
        var draft = await context.ProgrammeDrafts
            .FirstOrDefaultAsync(row => row.ProgrammeDraftId == command.ProgrammeDraftId, cancellationToken)
            ?? throw new InvalidOperationException("That draft programme update no longer exists.");
        if (draft.Status != (int)ProgrammeDraftStatus.Open)
            throw new InvalidOperationException("That draft has already been closed.");

        draft.Status = (int)ProgrammeDraftStatus.Discarded;
        draft.ResolvedAt = DateTimeOffset.UtcNow;
        draft.ResolvedByEmail = command.DiscardedByEmail;

        await context.SaveChangesAsync(cancellationToken);
        return draft.ToModel();
    }
}

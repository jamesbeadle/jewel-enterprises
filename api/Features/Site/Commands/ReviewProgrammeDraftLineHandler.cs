using Jewel.JPMS.Api.Features.Site.Drafts;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

// The reviewer's decision on one line. A changed mapping recomputes the proposal from the
// claim and marks the line as set by a person (that is the mapping applying will save on the
// task); an unchanged mapping keeps its source and its proposal, and only the tick and the
// reviewer's own figure move.
public sealed class ReviewProgrammeDraftLineHandler : ICommandHandler<ReviewProgrammeDraftLine, ProgrammeDraftLine>
{
    private readonly JpmsContext context;
    public ReviewProgrammeDraftLineHandler(JpmsContext context) { this.context = context; }

    public async Task<ProgrammeDraftLine> HandleAsync(ReviewProgrammeDraftLine command, CancellationToken cancellationToken)
    {
        var line = await context.ProgrammeDraftLines
            .FirstOrDefaultAsync(row => row.ProgrammeDraftLineId == command.ProgrammeDraftLineId, cancellationToken)
            ?? throw new InvalidOperationException("That draft line no longer exists.");
        var draft = await context.ProgrammeDrafts.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ProgrammeDraftId == line.ProgrammeDraftId, cancellationToken)
            ?? throw new InvalidOperationException("That draft programme update no longer exists.");
        if (draft.Status != (int)ProgrammeDraftStatus.Open)
            throw new InvalidOperationException("That draft has already been closed.");

        var codes = ProgrammeDraftCostCodes.Clean(command.CostCodes);
        var mappingChanged = !codes.SequenceEqual(ProgrammeDraftCostCodes.Split(line.CostCodes), StringComparer.OrdinalIgnoreCase);
        if (mappingChanged)
        {
            var centres = await ProgrammeDraftClaimCentres.LoadAsync(context, draft.ProjectId, draft.ValuationClaimId, cancellationToken);
            ProgrammeDraftLineProposal.Apply(line, centres, codes, ProgrammeMappingSource.Person);
        }

        // A line takes part only when there is a figure to write — the reviewer's or the proposal.
        line.ReviewedPercent = command.ReviewedPercent;
        var hasFigureToApply = (line.ReviewedPercent ?? line.ProposedPercent) is not null;
        line.IsIncluded = command.IsIncluded && hasFigureToApply;

        await context.SaveChangesAsync(cancellationToken);
        return line.ToModel();
    }
}

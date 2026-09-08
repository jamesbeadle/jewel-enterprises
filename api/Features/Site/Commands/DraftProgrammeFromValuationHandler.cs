using Jewel.JPMS.Api.Features.Site.Drafts;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

// The Programme tab's hand-run of the draft capture: the same ProgrammeDraftCapture the approval
// hook runs, against the claim the user picked. Refusals are phrased for the reader — the
// endpoint turns each into a 400 the form prints — because here somebody is waiting for the
// answer, where the approval hook quietly does nothing.
public sealed class DraftProgrammeFromValuationHandler : ICommandHandler<DraftProgrammeFromValuation, ProgrammeDraftDetail>
{
    private readonly JpmsContext context;
    public DraftProgrammeFromValuationHandler(JpmsContext context) { this.context = context; }

    public async Task<ProgrammeDraftDetail> HandleAsync(DraftProgrammeFromValuation command, CancellationToken cancellationToken)
    {
        var claim = await context.ValuationClaims.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ValuationClaimId == command.ValuationClaimId && row.ProjectId == command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException("That valuation claim isn't on this project.");
        if (claim.Status == (int)ValuationClaimStatus.Draft)
            throw new InvalidOperationException("That claim is still a draft — its percentages are still moving. Lock it (\"We're claiming this\") on the Valuation tab first.");

        var hasTasks = await context.ProgrammeTasks.AnyAsync(task => task.ProjectId == command.ProjectId, cancellationToken);
        if (!hasTasks)
            throw new InvalidOperationException("There are no programme tasks to update yet — add the programme first.");

        var draft = await ProgrammeDraftCapture.OpenAsync(context, command.ProjectId, command.ValuationClaimId, command.RequestedByEmail, cancellationToken)
            ?? throw new InvalidOperationException("The valuation report has no priced lines to take percentages from.");

        await context.SaveChangesAsync(cancellationToken);
        return await ProgrammeDraftReader.DetailAsync(context, draft, cancellationToken);
    }
}

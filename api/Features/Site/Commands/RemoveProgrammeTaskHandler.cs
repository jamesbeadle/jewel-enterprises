using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

// Removes a live programme task together with any dependency links that touch it (a link without
// both ends is meaningless), its saved cost-centre mappings (a mapping of nothing) and the variation
// pushes recorded on it (a push of nothing). Baseline snapshots are deliberately kept: they are the
// contemporaneous record movement is measured against, and ProgrammeMovementCalculator simply
// skips snapshot rows whose live task no longer exists.
public sealed class RemoveProgrammeTaskHandler : ICommandHandler<RemoveProgrammeTask, Acknowledgement>
{
    private readonly JpmsContext context;
    public RemoveProgrammeTaskHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(RemoveProgrammeTask command, CancellationToken cancellationToken)
    {
        var entity = await context.ProgrammeTasks
            .FirstOrDefaultAsync(t => t.ProgrammeTaskId == command.ProgrammeTaskId, cancellationToken);
        if (entity is not null)
        {
            var links = await context.ProgrammeTaskLinks
                .Where(l => l.PredecessorTaskId == command.ProgrammeTaskId || l.SuccessorTaskId == command.ProgrammeTaskId)
                .ToListAsync(cancellationToken);
            context.ProgrammeTaskLinks.RemoveRange(links);
            var costCentres = await context.ProgrammeTaskCostCentres
                .Where(mapping => mapping.ProgrammeTaskId == command.ProgrammeTaskId)
                .ToListAsync(cancellationToken);
            context.ProgrammeTaskCostCentres.RemoveRange(costCentres);
            var variationEffects = await context.ProgrammeVariationEffects
                .Where(effect => effect.ProgrammeTaskId == command.ProgrammeTaskId)
                .ToListAsync(cancellationToken);
            context.ProgrammeVariationEffects.RemoveRange(variationEffects);
            context.ProgrammeTasks.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }
        return new Acknowledgement(command.ProgrammeTaskId);
    }
}

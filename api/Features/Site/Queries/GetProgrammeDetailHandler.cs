using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Queries;

// Everything the Programme tab's programme view needs in one round trip: live tasks, dependency
// links, the latest baseline with its task snapshots (movement is computed from the pair via
// ProgrammeMovementCalculator, client- or agent-side), and each task's confirmed cost-centre
// mappings from applied draft programme updates, and every push a variation has been recorded
// as making on a task (the variations themselves come from their own store).
public sealed class GetProgrammeDetailHandler : IQueryHandler<GetProgrammeDetail, ProgrammeDetail>
{
    private readonly JpmsContext context;
    public GetProgrammeDetailHandler(JpmsContext context) { this.context = context; }

    public async Task<ProgrammeDetail> HandleAsync(GetProgrammeDetail query, CancellationToken cancellationToken)
    {
        var tasks = await context.ProgrammeTasks.AsNoTracking()
            .Where(task => task.ProjectId == query.ProjectId)
            .OrderBy(task => task.PlannedStart)
            .ToListAsync(cancellationToken);

        var links = await context.ProgrammeTaskLinks.AsNoTracking()
            .Where(link => link.ProjectId == query.ProjectId)
            .ToListAsync(cancellationToken);

        var baselines = await context.ProgrammeBaselines.AsNoTracking()
            .Where(b => b.ProjectId == query.ProjectId)
            .OrderByDescending(b => b.TakenAt)
            .ToListAsync(cancellationToken);

        var baseline = baselines.FirstOrDefault();

        var baselineTasks = baseline is null
            ? new List<Data.Entities.ProgrammeBaselineTaskEntity>()
            : await context.ProgrammeBaselineTasks.AsNoTracking()
                .Where(t => t.ProgrammeBaselineId == baseline.ProgrammeBaselineId)
                .ToListAsync(cancellationToken);

        var costCentres = await context.ProgrammeTaskCostCentres.AsNoTracking()
            .Where(mapping => mapping.ProjectId == query.ProjectId)
            .ToListAsync(cancellationToken);

        var variationEffects = await context.ProgrammeVariationEffects.AsNoTracking()
            .Where(effect => effect.ProjectId == query.ProjectId)
            .ToListAsync(cancellationToken);

        return new ProgrammeDetail(
            tasks.Select(t => t.ToModel()).ToList().AsReadOnly(),
            links.Select(l => l.ToModel()).ToList().AsReadOnly(),
            baseline?.ToModel(),
            baselineTasks.Select(t => t.ToModel()).ToList().AsReadOnly(),
            baselines.Select(b => b.ToModel()).ToList().AsReadOnly(),
            costCentres.Select(mapping => mapping.ToModel()).ToList().AsReadOnly(),
            variationEffects.Select(effect => effect.ToModel()).ToList().AsReadOnly());
    }
}

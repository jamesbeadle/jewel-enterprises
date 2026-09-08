using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

// The draft becomes the programme: every included line's figure is written onto its task as
// the task's progress, every mapped line's cost centres are saved on the task (replacing what
// was there — the reviewer has just confirmed them, and the next valuation's draft starts from
// exactly these), and the draft closes as Applied. Tasks removed since the draft opened are
// skipped; dates are never touched (decision 2026-09-08: progress only).
public sealed class ApplyProgrammeDraftHandler : ICommandHandler<ApplyProgrammeDraft, ProgrammeDraft>
{
    private readonly JpmsContext context;
    public ApplyProgrammeDraftHandler(JpmsContext context) { this.context = context; }

    public async Task<ProgrammeDraft> HandleAsync(ApplyProgrammeDraft command, CancellationToken cancellationToken)
    {
        var draft = await context.ProgrammeDrafts
            .FirstOrDefaultAsync(row => row.ProgrammeDraftId == command.ProgrammeDraftId, cancellationToken)
            ?? throw new InvalidOperationException("That draft programme update no longer exists.");
        if (draft.Status != (int)ProgrammeDraftStatus.Open)
            throw new InvalidOperationException("That draft has already been closed.");

        var lines = await context.ProgrammeDraftLines.AsNoTracking()
            .Where(line => line.ProgrammeDraftId == draft.ProgrammeDraftId)
            .ToListAsync(cancellationToken);
        var tasksById = await context.ProgrammeTasks
            .Where(task => task.ProjectId == draft.ProjectId)
            .ToDictionaryAsync(task => task.ProgrammeTaskId, cancellationToken);

        foreach (var line in lines)
        {
            if (!tasksById.TryGetValue(line.ProgrammeTaskId, out var task)) continue;

            var percent = line.ReviewedPercent ?? line.ProposedPercent;
            if (line.IsIncluded && percent is { } figure)
                task.ProgressPercent = Math.Clamp(figure, 0m, 100m);

            var codes = ProgrammeDraftCostCodes.Split(line.CostCodes);
            if (codes.Count > 0)
                await SaveMappingAsync(draft.ProjectId, task.ProgrammeTaskId, codes, cancellationToken);
        }

        draft.Status = (int)ProgrammeDraftStatus.Applied;
        draft.ResolvedAt = DateTimeOffset.UtcNow;
        draft.ResolvedByEmail = command.AppliedByEmail;

        await context.SaveChangesAsync(cancellationToken);
        return draft.ToModel();
    }

    private async Task SaveMappingAsync(string projectId, string programmeTaskId, IReadOnlyList<string> codes, CancellationToken cancellationToken)
    {
        var existing = await context.ProgrammeTaskCostCentres
            .Where(mapping => mapping.ProgrammeTaskId == programmeTaskId)
            .ToListAsync(cancellationToken);
        context.ProgrammeTaskCostCentres.RemoveRange(existing);

        foreach (var code in codes)
        {
            context.ProgrammeTaskCostCentres.Add(new ProgrammeTaskCostCentreEntity
            {
                ProgrammeTaskCostCentreId = SiteIdentifierFactory.NextProgrammeTaskCostCentreId(),
                ProjectId = projectId,
                ProgrammeTaskId = programmeTaskId,
                CostCode = code
            });
        }
    }
}

using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

// Records the days a variation pushes one programme task — the programme's own fact about the
// variation, written here and nowhere near the variation document. One effect per variation per
// task: recording again re-states the days and the note. The task must be the project's; the
// variation must be the project's and still on the programme (not rejected).
public sealed class RecordProgrammeVariationEffectHandler : ICommandHandler<RecordProgrammeVariationEffect, ProgrammeVariationEffect>
{
    private readonly JpmsContext context;
    public RecordProgrammeVariationEffectHandler(JpmsContext context) { this.context = context; }

    public async Task<ProgrammeVariationEffect> HandleAsync(RecordProgrammeVariationEffect command, CancellationToken cancellationToken)
    {
        await EnsureTaskIsOnProjectAsync(command, cancellationToken);
        await EnsureVariationIsOnProgrammeAsync(command, cancellationToken);

        var entity = await context.ProgrammeVariationEffects
            .FirstOrDefaultAsync(effect => effect.VariationOrderId == command.VariationOrderId
                                           && effect.ProgrammeTaskId == command.ProgrammeTaskId, cancellationToken)
            ?? NewEffect(command);

        entity.DelayDays = command.DelayDays;
        entity.Note = command.Note.Trim();
        entity.RecordedByEmail = command.RecordedByEmail;
        entity.RecordedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    private ProgrammeVariationEffectEntity NewEffect(RecordProgrammeVariationEffect command)
    {
        var entity = new ProgrammeVariationEffectEntity
        {
            ProgrammeVariationEffectId = SiteIdentifierFactory.NextProgrammeVariationEffectId(),
            ProjectId = command.ProjectId,
            VariationOrderId = command.VariationOrderId,
            ProgrammeTaskId = command.ProgrammeTaskId
        };
        context.ProgrammeVariationEffects.Add(entity);
        return entity;
    }

    private async Task EnsureTaskIsOnProjectAsync(RecordProgrammeVariationEffect command, CancellationToken cancellationToken)
    {
        var isOnProject = await context.ProgrammeTasks
            .AnyAsync(task => task.ProjectId == command.ProjectId && task.ProgrammeTaskId == command.ProgrammeTaskId, cancellationToken);
        if (!isOnProject) throw new InvalidOperationException("That programme task is not on this project.");
    }

    private async Task EnsureVariationIsOnProgrammeAsync(RecordProgrammeVariationEffect command, CancellationToken cancellationToken)
    {
        var variation = await context.VariationOrders.AsNoTracking()
            .FirstOrDefaultAsync(order => order.VariationOrderId == command.VariationOrderId, cancellationToken);
        if (variation is null || variation.ProjectId != command.ProjectId)
            throw new InvalidOperationException("That variation is not on this project.");
        if ((VariationOrderStatus)variation.Status == VariationOrderStatus.Rejected)
            throw new InvalidOperationException("A rejected variation has left the programme — it cannot push a task.");
    }
}

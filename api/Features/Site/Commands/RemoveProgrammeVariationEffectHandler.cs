using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class RemoveProgrammeVariationEffectHandler : ICommandHandler<RemoveProgrammeVariationEffect, Acknowledgement>
{
    private readonly JpmsContext context;
    public RemoveProgrammeVariationEffectHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(RemoveProgrammeVariationEffect command, CancellationToken cancellationToken)
    {
        var entity = await context.ProgrammeVariationEffects
            .FirstOrDefaultAsync(effect => effect.ProgrammeVariationEffectId == command.ProgrammeVariationEffectId, cancellationToken);
        if (entity is not null)
        {
            context.ProgrammeVariationEffects.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }
        return new Acknowledgement(command.ProgrammeVariationEffectId);
    }
}

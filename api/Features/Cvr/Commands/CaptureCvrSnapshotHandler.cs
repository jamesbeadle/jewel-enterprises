using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class CaptureCvrSnapshotHandler : ICommandHandler<CaptureCvrSnapshot, CvrSnapshot>
{
    private readonly JpmsContext context;

    public CaptureCvrSnapshotHandler(JpmsContext context) { this.context = context; }

    public async Task<CvrSnapshot> HandleAsync(CaptureCvrSnapshot command, CancellationToken cancellationToken)
    {
        var marginPounds = command.ForecastFinalValue - command.ForecastFinalCost;
        var entity = new CvrSnapshotEntity
        {
            CvrSnapshotId = CvrIdentifierFactory.NextSnapshotId(),
            ProjectId = command.ProjectId,
            SnapshotAt = DateTimeOffset.UtcNow,
            TenderValue = command.TenderValue,
            ForecastFinalCost = command.ForecastFinalCost,
            ForecastFinalValue = command.ForecastFinalValue,
            MarginPounds = marginPounds,
            MarginPercent = CvrCalculations.ProfitOnCostPercent(marginPounds, command.ForecastFinalCost),
            WeeksAheadOrBehind = command.WeeksAheadOrBehind
        };
        context.CvrSnapshots.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

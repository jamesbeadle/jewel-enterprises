using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class RecordCvrPackageRowHandler : ICommandHandler<RecordCvrPackageRow, CvrPackageRow>
{
    private readonly JpmsContext context;

    public RecordCvrPackageRowHandler(JpmsContext context) { this.context = context; }

    public async Task<CvrPackageRow> HandleAsync(RecordCvrPackageRow command, CancellationToken cancellationToken)
    {
        var entity = await context.CvrPackageRows.FirstOrDefaultAsync(
            row => row.ProjectId == command.ProjectId && row.PackageName == command.PackageName, cancellationToken);
        var newCombinedValue = command.OrderValue + command.VariationValue;
        if (entity is null)
        {
            entity = new CvrPackageRowEntity
            {
                CvrPackageRowId = CvrIdentifierFactory.NextPackageRowId(),
                ProjectId = command.ProjectId,
                PackageName = command.PackageName
            };
            context.CvrPackageRows.Add(entity);
        }
        else
        {
            entity.MovementSinceLastSnapshot = newCombinedValue - (entity.OrderValue + entity.VariationValue);
        }
        entity.OrderCost = command.OrderCost;
        entity.OrderValue = command.OrderValue;
        entity.VariationCost = command.VariationCost;
        entity.VariationValue = command.VariationValue;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

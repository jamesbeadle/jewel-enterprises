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
        var priorCombinedValue = entity is null
            ? command.OrderValue + command.VariationValue
            : entity.OrderValue + entity.VariationValue;

        entity ??= AddNewPackageRow(command);
        entity.MovementSinceLastSnapshot = command.OrderValue + command.VariationValue - priorCombinedValue;
        entity.OrderCost = command.OrderCost;
        entity.OrderValue = command.OrderValue;
        entity.VariationCost = command.VariationCost;
        entity.VariationValue = command.VariationValue;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    private CvrPackageRowEntity AddNewPackageRow(RecordCvrPackageRow command)
    {
        var entity = new CvrPackageRowEntity
        {
            CvrPackageRowId = CvrIdentifierFactory.NextPackageRowId(),
            ProjectId = command.ProjectId,
            PackageName = command.PackageName
        };
        context.CvrPackageRows.Add(entity);
        return entity;
    }
}

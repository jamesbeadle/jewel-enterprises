using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Cashflow;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Cashflow.Commands;

public sealed class CaptureCashflowSnapshotHandler : ICommandHandler<CaptureCashflowSnapshot, CashflowSnapshot>
{
    private readonly JpmsContext context;

    public CaptureCashflowSnapshotHandler(JpmsContext context) { this.context = context; }

    public async Task<CashflowSnapshot> HandleAsync(CaptureCashflowSnapshot command, CancellationToken cancellationToken)
    {
        var entity = new CashflowSnapshotEntity
        {
            CashflowSnapshotId = CashflowIdentifierFactory.NextSnapshotId(),
            GeneratedAt = DateTimeOffset.UtcNow,
            ExpectedIncome13Week = command.ExpectedIncome13Week,
            CommittedSpend13Week = command.CommittedSpend13Week,
            NetPosition13Week = command.ExpectedIncome13Week - command.CommittedSpend13Week
        };
        context.CashflowSnapshots.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cashflow;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Cashflow.Queries;

public sealed class GetLatestCashflowSnapshotHandler
    : IQueryHandler<GetLatestCashflowSnapshot, CashflowSnapshot?>
{
    private readonly JpmsContext context;

    public GetLatestCashflowSnapshotHandler(JpmsContext context) { this.context = context; }

    public async Task<CashflowSnapshot?> HandleAsync(
        GetLatestCashflowSnapshot query, CancellationToken cancellationToken)
    {
        var entity = await context.CashflowSnapshots
            .OrderByDescending(snapshot => snapshot.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return entity?.ToModel();
    }
}

using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class GetBidPackageByIdHandler : IQueryHandler<GetBidPackageById, BidPackage?>
{
    private readonly JpmsContext context;

    public GetBidPackageByIdHandler(JpmsContext context) { this.context = context; }

    public async Task<BidPackage?> HandleAsync(GetBidPackageById query, CancellationToken cancellationToken)
    {
        var entity = await context.BidPackages
            .FirstOrDefaultAsync(package => package.BidPackageId == query.BidPackageId, cancellationToken);
        return entity?.ToModel();
    }
}

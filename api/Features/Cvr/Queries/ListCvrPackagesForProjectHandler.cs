using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Cvr.Queries;

public sealed class ListCvrPackagesForProjectHandler
    : IQueryHandler<ListCvrPackagesForProject, IReadOnlyList<CvrPackageRow>>
{
    private readonly JpmsContext context;

    public ListCvrPackagesForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<CvrPackageRow>> HandleAsync(
        ListCvrPackagesForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.CvrPackageRows
            .Where(row => row.ProjectId == query.ProjectId)
            .OrderBy(row => row.PackageName)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}

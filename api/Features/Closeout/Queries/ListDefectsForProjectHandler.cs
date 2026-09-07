using Jewel.JPMS.Contracts.Closeout;

namespace Jewel.JPMS.Api.Features.Closeout.Queries;

public sealed class ListDefectsForProjectHandler : IQueryHandler<ListDefectsForProject, IReadOnlyList<Defect>>
{
    private readonly JpmsContext context;
    public ListDefectsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Defect>> HandleAsync(ListDefectsForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.Defects.AsNoTracking().Where(d => d.ProjectId == query.ProjectId).OrderByDescending(d => d.RaisedAt).ToListAsync(cancellationToken);
        var suppliers = await DefectSupplierLookup.ForAsync(context, entities, cancellationToken);
        return entities
            .Select(entity => entity.ToModel(
                entity.SubcontractorId is { } id && suppliers.TryGetValue(id, out var supplier) ? supplier : null))
            .ToList()
            .AsReadOnly();
    }
}

using Jewel.JPMS.Contracts.Closeout;

namespace Jewel.JPMS.Api.Features.Closeout.Queries;

// One defect for its own page. Null when there is no such row — the page shows "gone", the
// endpoint answers 200 null so a deleted-behind-the-link case reads the same as any other miss.
public sealed class GetDefectByIdHandler : IQueryHandler<GetDefectById, Defect?>
{
    private readonly JpmsContext context;
    public GetDefectByIdHandler(JpmsContext context) { this.context = context; }

    public async Task<Defect?> HandleAsync(GetDefectById query, CancellationToken cancellationToken)
    {
        var entity = await context.Defects.AsNoTracking().FirstOrDefaultAsync(d => d.DefectId == query.DefectId, cancellationToken);
        if (entity is null) return null;
        return entity.ToModel(await DefectSupplierLookup.OneAsync(context, entity.SubcontractorId, cancellationToken));
    }
}

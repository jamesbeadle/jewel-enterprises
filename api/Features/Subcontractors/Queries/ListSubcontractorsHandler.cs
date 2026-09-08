using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

public sealed class ListSubcontractorsHandler
    : IQueryHandler<ListSubcontractors, IReadOnlyList<Subcontractor>>
{
    private readonly JpmsContext context;

    public ListSubcontractorsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Subcontractor>> HandleAsync(ListSubcontractors query, CancellationToken cancellationToken)
    {
        var entities = await context.Subcontractors.AsNoTracking().OrderBy(sub => sub.CompanyName).ToListAsync(cancellationToken);
        var tradesBySubcontractor = await context.TradesBySubcontractorAsync(cancellationToken);

        // The Xero link mark: a record holding at least one Xero link (imported from Xero, linked
        // to a Xero contact, or a Xero-imported record was consolidated into it) shows as linked,
        // and carries the links so its page can name the Xero contact and offer to unlink.
        var xeroLinksByRecord = await DirectoryXeroLinks.ByRecordAsync(context, cancellationToken);

        return entities
            .Select(entity => entity.ToModel(
                tradesBySubcontractor.TryGetValue(entity.SubcontractorId, out var trades) ? trades : Array.Empty<Trade>(),
                xeroLinked: xeroLinksByRecord.ContainsKey(entity.SubcontractorId),
                xeroLinks: xeroLinksByRecord.TryGetValue(entity.SubcontractorId, out var links) ? links : null))
            .ToList()
            .AsReadOnly();
    }
}

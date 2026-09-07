using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

// The package's recorded verdicts on its tagged emails (Discarded / Extracted). The live tagged
// list (ListBidPackageEmails) stays the source of WHICH emails exist; the client joins the two by
// message id, so a verdict on an email that has since been untagged simply matches nothing.
public sealed class ListBidPackageEmailDispositionsHandler
    : IQueryHandler<ListBidPackageEmailDispositions, IReadOnlyList<BidPackageEmailDisposition>>
{
    private readonly JpmsContext context;

    public ListBidPackageEmailDispositionsHandler(JpmsContext context) { this.context = context; }

    public Task<IReadOnlyList<BidPackageEmailDisposition>> HandleAsync(
        ListBidPackageEmailDispositions query, CancellationToken cancellationToken)
        => BidPackageEmailDispositionStore.ListAsync(context, query.BidPackageId, cancellationToken);
}

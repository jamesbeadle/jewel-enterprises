using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Subcontractors;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Queries;

/// <summary>
/// The Xero supplier list for the directory's "Import from Xero" modal. The Xero read is the
/// client's cached snapshot; each supplier is then stamped with whether a directory record already
/// links to it (and which), so the modal can show "Imported" instead of offering a second import —
/// and, for an unlinked contact, with the one unlinked directory record its name matches, so the
/// modal can offer "link to that record" instead of minting a duplicate (2026-09-08).
/// </summary>
public sealed class ListXeroSuppliersHandler : IQueryHandler<ListXeroSuppliers, XeroSuppliersSnapshot>
{
    private readonly IXeroClient xero;
    private readonly JpmsContext context;

    public ListXeroSuppliersHandler(IXeroClient xero, JpmsContext context)
    {
        this.xero = xero;
        this.context = context;
    }

    public async Task<XeroSuppliersSnapshot> HandleAsync(ListXeroSuppliers query, CancellationToken cancellationToken)
    {
        var snapshot = await xero.GetSuppliersAsync(query.Force, cancellationToken);
        if (snapshot.Suppliers.Count == 0) return snapshot;

        var linkedByContactId = await context.SubcontractorXeroLinks.AsNoTracking()
            .ToDictionaryAsync(link => link.XeroContactId, link => link.SubcontractorId, cancellationToken);
        var unlinkedRecords = await DirectoryXeroMatcher.UnlinkedRecordsAsync(context, cancellationToken);

        return snapshot with
        {
            Suppliers = snapshot.Suppliers
                .Select(supplier => linkedByContactId.TryGetValue(supplier.ContactId, out var subcontractorId)
                    ? supplier with { AlreadyImported = true, LinkedSubcontractorId = subcontractorId }
                    : WithMatchingRecord(supplier, unlinkedRecords))
                .ToList()
        };
    }

    // Unambiguous only — two records that both match are a human's call in the directory, not a
    // suggestion; the connector's list_unlinked_directory_records shows every candidate.
    private static XeroSupplier WithMatchingRecord(XeroSupplier supplier, IReadOnlyList<SubcontractorEntity> unlinkedRecords)
    {
        var matches = DirectoryXeroMatcher.RecordsMatching(supplier.Name, unlinkedRecords);
        if (matches.Count != 1) return supplier;
        return supplier with { MatchingSubcontractorId = matches[0].SubcontractorId, MatchingSubcontractorName = matches[0].CompanyName };
    }
}

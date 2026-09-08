using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Subcontractors;

/// <summary>
/// Pairs unlinked directory records with unlinked Xero contacts by name — the SAME rule the
/// worker ↔ directory linking uses (WorkerDirectoryMatcher: normalised equality, or containment
/// either way when the shorter name still has two words), so "JP Air conditioning" finds
/// "JP Air Conditioning Services Ltd" here exactly as it would on the allocation page. Serves the
/// import modal's "link to existing record" offer and the connector's
/// list_unlinked_directory_records; a human confirms every pairing — nothing here writes.
/// </summary>
public static class DirectoryXeroMatcher
{
    public static bool Matches(string companyName, string xeroContactName) =>
        WorkerDirectoryMatcher.Matches(companyName, xeroContactName);

    public static IReadOnlyList<XeroSupplier> SuppliersMatching(string companyName, IEnumerable<XeroSupplier> suppliers) =>
        suppliers.Where(supplier => Matches(companyName, supplier.Name)).ToList();

    public static IReadOnlyList<SubcontractorEntity> RecordsMatching(string xeroContactName, IEnumerable<SubcontractorEntity> records) =>
        records.Where(record => Matches(record.CompanyName, xeroContactName)).ToList();

    /// <summary>The directory records a Xero contact could be linked to: in the directory proper
    /// (never a tender-only prospect) and holding no Xero link yet.</summary>
    public static async Task<IReadOnlyList<SubcontractorEntity>> UnlinkedRecordsAsync(JpmsContext context, CancellationToken cancellationToken)
    {
        var linkedIds = await context.SubcontractorXeroLinks.AsNoTracking()
            .Select(link => link.SubcontractorId)
            .Distinct()
            .ToListAsync(cancellationToken);
        return await context.Subcontractors.AsNoTracking()
            .Where(record => !record.IsProspect && !linkedIds.Contains(record.SubcontractorId))
            .OrderBy(record => record.CompanyName)
            .ToListAsync(cancellationToken);
    }
}

namespace Jewel.JPMS.Features.Directory;

/// <summary>One line of the compliance register: a company and one of its current documents —
/// or the company alone, standing Missing, when it holds none. Rows read worst first (Expired,
/// Expiring soon, Missing, Current), soonest expiry first within a standing, then by company.</summary>
public sealed record ComplianceRegisterRow(Subcontractor Company, ComplianceDocument? Document, ComplianceStatus Status)
{
    public string DocumentLabel => Document?.Kind ?? "No documents on file";

    public DateTimeOffset? ExpiresAt => Document?.ExpiresAt;

    public static IReadOnlyList<ComplianceRegisterRow> Build(
        IReadOnlyList<Subcontractor> companies, IReadOnlyList<ComplianceDocument> currentDocuments)
    {
        var documentsByCompany = currentDocuments
            .Where(document => document.IsCurrentVersion)
            .ToLookup(document => document.SubcontractorId, StringComparer.OrdinalIgnoreCase);
        return companies
            .SelectMany(company => RowsFor(company, documentsByCompany[company.SubcontractorId].ToList()))
            .OrderBy(row => DirectoryComplianceFilter.RankOf(row.Status))
            .ThenBy(row => row.ExpiresAt ?? DateTimeOffset.MaxValue)
            .ThenBy(row => row.Company.CompanyName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<ComplianceRegisterRow> RowsFor(Subcontractor company, IReadOnlyList<ComplianceDocument> documents)
    {
        if (documents.Count == 0) return new[] { new ComplianceRegisterRow(company, null, ComplianceStatus.Missing) };
        return documents.Select(document => new ComplianceRegisterRow(company, document, document.Status()));
    }
}

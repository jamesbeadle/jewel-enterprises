using Jewel.JPMS.Contracts.Subcontractors;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The compliance register (`/directory/compliance`, 2026-09-09, the accountant's ask) as a
/// connector read: every directory company with its standing — the worst status among its
/// current documents, Missing when it holds none — and each current document with its expiry,
/// in ComplianceStatusExtensions.ReadingOrder (expired, expiring, current, then the companies
/// with nothing on file). Same read as the page (ListCurrentComplianceDocuments), the same
/// standing rule (ComplianceDocumentExtensions.Standing) and the same order, so the connector
/// and the register never disagree about who is lapsing or what comes first.
/// </summary>
internal static partial class AiRecordTools
{
    // Mirror of ListCurrentComplianceDocumentsEndpoint.InternalRolesThatMayReadCompliance, plus
    // Role.Admin named explicitly per the authorisation convention.
    private static readonly RoleSet ComplianceReaders = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager,
        JpmsRoles.Estimator, JpmsRoles.SiteManager, JpmsRoles.HealthAndSafetyLead,
        JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing);

    private static IEnumerable<AiTool> ComplianceTools() => new AiTool[]
    {
        new(
            "list_compliance_register",
            "The compliance register: every company in the directory (tender-only prospects "
            + "excluded) with its standing — Expired (a current document has passed its expiry), "
            + "ExpiringSoon (expires within 30 days), Missing (no compliance documents on file) or "
            + "Current — and its current documents (kind, file, expiry, status), expired and expiring "
            + "first, then current, then the companies with nothing on file. Pass "
            + "a status to read only the companies standing there; a search narrows to a company. "
            + "This is the data behind the Directory's compliance chips and /directory/compliance — "
            + "call it for anything about who can be paid, whose insurance has lapsed, or what "
            + "needs chasing.",
            AiToolSchema.Object(
                ("status", "string", "Expired, ExpiringSoon, Missing or Current — the company standing to list; omit for all.", false),
                ("search", "string", "Optional text matched against the company name.", false)),
            AiToolKind.Read,
            ComplianceReaders,
            async (context, input, ct) =>
            {
                var statusText = AiToolSchema.Text(input, "status")?.Trim();
                ComplianceStatus? wanted = null;
                if (!string.IsNullOrWhiteSpace(statusText))
                {
                    if (!Enum.TryParse<ComplianceStatus>(statusText, ignoreCase: true, out var parsed))
                        return Fail("status must be Expired, ExpiringSoon, Missing or Current.");
                    wanted = parsed;
                }
                var search = AiToolSchema.Text(input, "search");

                var documents = await context.Services
                    .GetRequiredService<IQueryHandler<ListCurrentComplianceDocuments, IReadOnlyList<ComplianceDocument>>>()
                    .HandleAsync(new ListCurrentComplianceDocuments(), ct);
                var byCompany = documents.ToLookup(document => document.SubcontractorId, StringComparer.OrdinalIgnoreCase);

                var companiesQuery = context.Db.Subcontractors.AsNoTracking().Where(row => !row.IsProspect);
                if (!string.IsNullOrWhiteSpace(search))
                    companiesQuery = companiesQuery.Where(row => row.CompanyName.Contains(search));
                var companies = await companiesQuery.OrderBy(row => row.CompanyName).ToListAsync(ct);

                var rows = companies
                    .Select(company => new
                    {
                        subcontractorId = company.SubcontractorId,
                        companyName = company.CompanyName,
                        category = ((DirectoryCategory)company.Category).ToString(),
                        standing = byCompany[company.SubcontractorId].Standing(),
                        documents = byCompany[company.SubcontractorId]
                            .OrderBy(document => document.ExpiresAt ?? DateTimeOffset.MaxValue)
                            .Select(document => new
                            {
                                document.ComplianceDocumentId,
                                document.Kind,
                                document.FileName,
                                document.ExpiresAt,
                                status = document.Status().ToString(),
                                document.UploadedAt
                            }).ToList()
                    })
                    .Where(row => wanted is null || row.standing == wanted)
                    .OrderBy(row => row.standing.ReadingRank())
                    .ThenBy(row => row.documents.Select(document => document.ExpiresAt).Min() ?? DateTimeOffset.MaxValue)
                    .ThenBy(row => row.companyName)
                    .Select(row => new { row.subcontractorId, row.companyName, row.category, standing = row.standing.ToString(), row.documents })
                    .ToList();

                return Serialise(new
                {
                    ok = true,
                    count = rows.Count,
                    counts = ComplianceStatusExtensions.ReadingOrder.ToDictionary(status => status.ToString(),
                        status => companies.Count(company => byCompany[company.SubcontractorId].Standing() == status)),
                    companies = rows,
                    note = "A company's standing is the worst of its current documents; superseded "
                           + "versions never count. list_sources with record_type subcontractor lists "
                           + "the files themselves; read_source opens one."
                });
            })
    };
}

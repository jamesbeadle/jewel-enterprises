using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Features.Subcontractors;

/// <summary>Every subcontractor's current-version compliance documents in one read — what the
/// directory list's compliance column and the dashboard's expiring-documents panel are built
/// from. Null Current is the honest "not fetched yet"; an empty list means "no documents".</summary>
public sealed class ComplianceOverviewReadModel : IReadModelStore<IReadOnlyList<ComplianceDocument>>
{
    private readonly IQueryClient queries;
    public IReadOnlyList<ComplianceDocument>? Current { get; private set; }
    public event Action? OnChanged;

    public ComplianceOverviewReadModel(IQueryClient queries) { this.queries = queries; }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListCurrentComplianceDocuments(), cancellationToken);
        OnChanged?.Invoke();
    }

    /// <summary>The listed subcontractor's overall standing: the worst status among its current
    /// documents, or Missing when it has none on record.</summary>
    public ComplianceStatus WorstStatusFor(string subcontractorId) =>
        (Current ?? Array.Empty<ComplianceDocument>())
            .Where(document => string.Equals(document.SubcontractorId, subcontractorId, StringComparison.OrdinalIgnoreCase))
            .Standing();
}

using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

/// <summary>
/// The rendered snapshot PDF plus the identity the callers need around it: the filename it should
/// travel under and the project the snapshot belongs to (for authorisation and addressing).
/// </summary>
public sealed record ValuationReportSnapshotPdf(
    byte[] Content,
    string FileName,
    string ProjectId,
    string ProjectName,
    ValuationReportSnapshot Snapshot);

/// <summary>
/// One valuation statement, loaded and ready to render — a frozen snapshot, or the working copy
/// the capture maths would freeze right now — with everything around it that both documents
/// share: the project identity for the header, the cost-centre names for the bill's area
/// sub-headings, and the ONE file-name stem the PDF and the spreadsheet travel under
/// (<see cref="ValuationReportFileNames"/>: extension aside, the pair share a name exactly).
/// Load it once and render both files from it, so a PDF and a workbook exported together can
/// never disagree about what the report said (the connector's export_valuation_report does this).
/// </summary>
public sealed record ValuationReportStatement(
    string ProjectId,
    string ProjectReference,
    string ProjectName,
    string ClientName,
    ValuationReportSnapshotDetail Detail,
    bool IsDraft,
    string FileNameStem,
    IReadOnlyDictionary<string, string> CostCentreNames);

/// <summary>
/// Assembles and renders the snapshot PDF in one place — the download endpoint streams exactly the
/// bytes the email command attaches, so what's downloaded and what's sent never diverge. Loads the
/// frozen detail through the existing query handler and the project header for the document's
/// identity, then hands both to <see cref="ValuationReportSnapshotRenderer"/>.
/// <see cref="BuildDraftAsync"/> renders the same statement from the LIVE report instead: the
/// capture maths compute what a snapshot would freeze right now (nothing is saved), and the
/// renderer stamps it as a working copy — so the preview and any later snapshot always agree.
/// The loading half is public on its own (<see cref="LoadAsync"/> / <see cref="LoadDraftAsync"/>)
/// so the spreadsheet builder can render the same loaded statement.
/// </summary>
public sealed class ValuationReportSnapshotPdfBuilder
{
    private readonly IQueryHandler<GetValuationReportSnapshot, ValuationReportSnapshotDetail> snapshots;
    private readonly JpmsContext context;

    public ValuationReportSnapshotPdfBuilder(
        IQueryHandler<GetValuationReportSnapshot, ValuationReportSnapshotDetail> snapshots,
        JpmsContext context)
    {
        this.snapshots = snapshots; this.context = context;
    }

    public async Task<ValuationReportSnapshotPdf> BuildAsync(string snapshotId, CancellationToken cancellationToken) =>
        Render(await LoadAsync(snapshotId, cancellationToken));

    /// <summary>
    /// The live valuation report as a working-copy PDF: computes the snapshot a capture would
    /// freeze right now (same maths, same line order — via ComputeAsync, which never touches the
    /// change tracker, so nothing persists) and renders it with the draft stamps. Labelled with
    /// the latest claim's period name so the accountant knows which claim they are reading.
    /// </summary>
    public async Task<ValuationReportSnapshotPdf> BuildDraftAsync(string projectId, CancellationToken cancellationToken) =>
        Render(await LoadDraftAsync(projectId, cancellationToken));

    /// <summary>A frozen snapshot with its project, ready to render.</summary>
    public async Task<ValuationReportStatement> LoadAsync(string snapshotId, CancellationToken cancellationToken)
    {
        var detail = await snapshots.HandleAsync(new GetValuationReportSnapshot(snapshotId), cancellationToken);
        var snapshot = detail.Snapshot;

        var project = await context.Projects.FindAsync(new object[] { snapshot.ProjectId }, cancellationToken)
            ?? throw new InvalidOperationException($"Project {snapshot.ProjectId} for snapshot {snapshotId} not found.");

        // Named exactly as the snapshot viewer names its spreadsheet, so the pair match.
        return new ValuationReportStatement(
            project.ProjectId, project.Reference, project.Name, project.ClientName, detail, IsDraft: false,
            ValuationReportFileNames.For(project.Reference, project.Name, snapshot.Label, snapshot.TakenAt),
            await CostCentreNamesAsync(cancellationToken));
    }

    /// <summary>The live report as the working-copy statement a capture would freeze right now.</summary>
    public async Task<ValuationReportStatement> LoadDraftAsync(string projectId, CancellationToken cancellationToken)
    {
        var project = await context.Projects.FindAsync(new object[] { projectId }, cancellationToken)
            ?? throw new InvalidOperationException($"Project {projectId} not found.");

        // "June 2026 — working copy" when the latest claim is named, "Claim 3 — working copy"
        // otherwise, "Working copy" when the project has never been valued.
        var claim = await context.ValuationClaims
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.ClaimNumber)
            .FirstOrDefaultAsync(cancellationToken);
        var claimName = claim is null
            ? null
            : string.IsNullOrWhiteSpace(claim.Name) ? $"Claim {claim.ClaimNumber}" : claim.Name;
        var label = claimName is null ? "Working copy" : $"{claimName} — working copy";

        var (snapshotEntity, lineEntities) = await ValuationReportSnapshotCapture.ComputeAsync(
            context, projectId, label, valuationInvoiceId: null, cancellationToken);

        var detail = new ValuationReportSnapshotDetail(
            snapshotEntity.ToModel(),
            lineEntities.Select(line => line.ToModel()).ToList());

        // The file is named by the claim alone — the working-copy wording stays inside the
        // document, so this PDF and the page's spreadsheet of the same claim share a name.
        return new ValuationReportStatement(
            projectId, project.Reference, project.Name, project.ClientName, detail, IsDraft: true,
            ValuationReportFileNames.For(project.Reference, project.Name, claimName, DateTimeOffset.UtcNow),
            await CostCentreNamesAsync(cancellationToken));
    }

    /// <summary>Renders a loaded statement — frozen or working copy — to its PDF.</summary>
    public static ValuationReportSnapshotPdf Render(ValuationReportStatement statement)
    {
        var pdf = ValuationReportSnapshotRenderer.Render(new ValuationReportSnapshotDocument(
            statement.ProjectReference,
            statement.ProjectName,
            statement.ClientName,
            statement.Detail,
            IsDraft: statement.IsDraft,
            CostCentreNames: statement.CostCentreNames));

        return new ValuationReportSnapshotPdf(
            pdf, SanitiseFileName($"{statement.FileNameStem}.pdf"),
            statement.ProjectId, statement.ProjectName, statement.Detail.Snapshot);
    }

    // Cost code → master name, for the bill's area sub-headings when a line carries no
    // estimate section (the ValuationReportAreas rule the renderer applies). Grouped rather
    // than ToDictionary so a duplicated code can never turn a PDF download into a 500.
    private async Task<IReadOnlyDictionary<string, string>> CostCentreNamesAsync(CancellationToken cancellationToken)
    {
        var centres = await context.CostCenters.AsNoTracking()
            .Select(centre => new { centre.Code, centre.Name })
            .ToListAsync(cancellationToken);
        return centres
            .GroupBy(centre => centre.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Name, StringComparer.OrdinalIgnoreCase);
    }

    internal static string SanitiseFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(fileName.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }
}

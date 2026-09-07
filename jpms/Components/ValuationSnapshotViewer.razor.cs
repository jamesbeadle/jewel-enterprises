using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Features.Triage.Panels;

namespace Jewel.JPMS.Components;

public partial class ValuationSnapshotViewer
{
    [Parameter, EditorRequired] public string SnapshotId { get; set; } = "";

    /// <summary>Wired by pages that host the email-draft modal: opens the draft flow for this
    /// snapshot. Left unwired, the button simply doesn't render.</summary>
    [Parameter] public EventCallback<string> OnEmailRequested { get; set; }

    private static readonly System.Globalization.CultureInfo Gb = System.Globalization.CultureInfo.GetCultureInfo("en-GB");

    private ValuationReportSnapshotDetail? detail;
    private bool loadFailed;
    private string? loadedSnapshotId;

    private string ExportFileName(ValuationReportSnapshot snapshot)
    {
        var project = Projects.Find(snapshot.ProjectId);
        return ValuationReportFileNames.Stem(project?.Reference ?? "", project?.Name, snapshot.Label);
    }

    // Null until the mailbox has ANSWERED — an empty list is a real "none", never a placeholder
    // (the loading convention: nullable backing field, gate on the null).
    private IReadOnlyList<MailboxMessage>? emails;
    private string? emailsError;

    // ---- Pending variations for the export's Pending tab --------------------
    // A snapshot freezes the report, not the register, so the workbook's Pending tab reads the
    // LIVE register at export time (the tab says so). Nullable until answered (the loading
    // convention); on failure the export still runs and the tab reports the register unread.
    private IReadOnlyList<VariationOrder>? variationOrders;
    private bool variationOrdersFailed;
    private string? variationOrdersProjectId;

    private bool PendingVariationsUnanswered => variationOrders is null && !variationOrdersFailed;

    private async Task LoadVariationOrdersAsync(string projectId)
    {
        if (projectId == variationOrdersProjectId) return;
        variationOrdersProjectId = projectId;
        variationOrders = null;
        variationOrdersFailed = false;
        try { variationOrders = await Variations.ListForProjectAsync(projectId); }
        catch { variationOrdersFailed = true; }
        await InvokeAsync(StateHasChanged);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (SnapshotId == loadedSnapshotId) return;
        loadedSnapshotId = SnapshotId;
        detail = null;
        loadFailed = false;
        emails = null;
        emailsError = null;
        try { detail = await Store.GetSnapshotAsync(SnapshotId); }
        catch { loadFailed = true; }
        if (detail is not null) _ = LoadVariationOrdersAsync(detail.Snapshot.ProjectId);
        await LoadEmailsAsync();
    }

    // The emails triaged to this snapshot's tag PLUS those tagged to the claim it was frozen from
    // (the period's correspondence travels with its statement), read live and merged — an email
    // tagged to both appears once. A failure opens the gate with its own message rather than
    // blocking the report above (the frozen lines are the main event).
    private async Task LoadEmailsAsync()
    {
        try
        {
            var tagged = new List<MailboxMessage>(await Queries.AskAsync(
                new ListRecordEmails(RecordType.ValuationReportSnapshot, SnapshotId), CancellationToken.None));
            if (detail?.Snapshot.ValuationClaimId is { Length: > 0 } claimId)
            {
                tagged.AddRange(await Queries.AskAsync(
                    new ListRecordEmails(RecordType.ValuationClaim, claimId), CancellationToken.None));
            }
            emails = tagged
                .GroupBy(email => string.IsNullOrEmpty(email.InternetMessageId) ? email.Id : email.InternetMessageId)
                .Select(group => group.First())
                .ToList();
        }
        catch
        {
            emailsError = "Couldn't load this snapshot's correspondence. Please try again.";
        }
    }

    private record Section(ValuationElementType Type, string Title, List<ValuationReportSnapshotLine> Lines)
    {
        public decimal Amount => Lines.Where(l => l.CountsTowardTotals).Sum(l => l.LineAmount);
        public decimal Claimed => Lines.Where(l => l.CountsTowardTotals).Sum(l => l.CumulativeClaimed);
    }

    private IEnumerable<Section> Sections
    {
        get
        {
            if (detail is null) yield break;
            Section Make(string title, ValuationElementType type) =>
                new(type, title, detail.Lines.Where(l => l.ElementType == type)
                                             .OrderBy(l => l.DisplayOrder)
                                             .ToList());
            yield return Make("Contract Works", ValuationElementType.ContractWorks);
            yield return Make("Provisional Sums", ValuationElementType.PcSum);
            yield return Make("Contingency Sums", ValuationElementType.Contingency);
            yield return Make("Variations", ValuationElementType.Variation);
        }
    }

    // ---- Area titles --------------------------------------------------------
    // Same shared rule as the live report table: the estimate section frozen on the line,
    // else the cost-centre name from the master. The master loads in the background —
    // re-render when it lands so fallback titles fill in (a snapshot is read-only, so a
    // late name never changes any figure).
    private string AreaTitleFor(ValuationReportSnapshotLine line) =>
        ValuationReportAreas.TitleFor(line.SectionName, line.CostCode, CostCentreNameFor);

    private string? CostCentreNameFor(string code) =>
        CostCenters.All().FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase))?.Name;

    protected override void OnInitialized()
    {
        CostCenters.OnChange += OnCostCentresChanged;
        // The export file name carries the project reference — re-render when the list lands.
        Projects.OnChanged += OnCostCentresChanged;
        // Revalidate the master (stale-while-revalidate; fetch-once underneath) — the host
        // page usually warmed it already, but the snapshots register opens this viewer too.
        _ = CostCenters.ListAllAsync();
    }

    private void OnCostCentresChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        CostCenters.OnChange -= OnCostCentresChanged;
        Projects.OnChanged -= OnCostCentresChanged;
    }

    // The code and title fallbacks are the shared export mapping's (ValuationSnapshotExport) —
    // the same rule the workbook, the PDF and the live report table read a line by.
    private static string CodeFor(ValuationReportSnapshotLine line) => ValuationSnapshotExport.CodeFor(line);
    private static string TitleFor(ValuationReportSnapshotLine line) => ValuationSnapshotExport.TitleFor(line);

    private static string Num(decimal v) => v.ToString("0.##", Gb);
    private static string Pct(decimal v) => v.ToString("0.##", Gb) + "%";

    // Every frozen line plus the summary footer, mapped by the shared ValuationSnapshotExport so
    // the workbook this button downloads and the one the connector's export_valuation_report
    // hands out are the same file — and both agree with the screen and the PDF, rendered from
    // the same lines. The workbook does its own consolidating (Summary with variations as one
    // row per order, one tab per variation order, and the Pending variations tab read from the
    // live register).
    private ExcelWorkbook? BuildExportWorkbook(bool _)
    {
        if (detail is null) return null;
        var snapshot = detail.Snapshot;

        return ValuationReportExportWorkbook.Build(
            ValuationSnapshotExport.Meta(snapshot, isDraft: false),
            ValuationSnapshotExport.Lines(detail.Lines, CostCentreNameFor),
            ValuationSnapshotExport.Summary(snapshot, detail.Lines),
            variationOrders is null ? null : ValuationExportPendingVariations.From(variationOrders));
    }
}

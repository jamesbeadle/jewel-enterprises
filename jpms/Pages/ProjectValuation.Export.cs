using Jewel.JPMS.Commercial;
using Jewel.JPMS.Features.Commercial;

namespace Jewel.JPMS.Pages;

public partial class ProjectValuation
{

    // ---- Working-copy Excel export ------------------------------------------
    // Same stem as the working-copy PDF's file name (ValuationReportFileNames), so the two
    // downloads of one claim sit together; the date is stamped on by the export button.
    private string LiveExportFileName
    {
        get
        {
            var project = Projects.Find(ProjectId);
            return ValuationReportFileNames.Stem(project?.Reference ?? "", project?.Name, Selected?.DisplayName);
        }
    }

    // The live report in the same workbook shape as a snapshot export (Summary with variations
    // as one row per order, one tab per variation order carrying its lines, and the Pending
    // variations tab from the register), stamped as a working copy: figures are the selected
    // claim's as the table currently shows them, with each line's previous/this-period movement
    // read against the claim before it. The client-facing export remains the frozen snapshot's —
    // this one is for checking the statement before it goes anywhere.
    private ExcelWorkbook? BuildLiveExportWorkbook(bool _)
    {
        if (!ReportReady) return null;
        var lines = Store.LinesFor(ProjectId);
        if (lines.Count == 0) return null;
        var entries = Selected is null
            ? Array.Empty<ClaimLine>() : Store.EntriesFor(Selected.ValuationClaimId);
        var previousEntries = PreviousClaim is null
            ? Array.Empty<ClaimLine>() : Store.EntriesFor(PreviousClaim.ValuationClaimId);

        static decimal PercentIn(IReadOnlyList<ClaimLine> set, ValuationLineItem line) =>
            set.FirstOrDefault(e => e.ValuationLineItemId == line.ValuationLineItemId)?.PercentComplete ?? 0m;

        // Same section order as the report table (and the snapshot capture): element type,
        // variations grouped by V-ref, then display order.
        var exportLines = new List<ValuationExportLine>();
        foreach (var (title, type) in new[]
        {
            ("Contract Works", ValuationElementType.ContractWorks),
            ("Provisional Sums", ValuationElementType.PcSum),
            ("Contingency Sums", ValuationElementType.Contingency),
            ("Variations", ValuationElementType.Variation)
        })
        {
            foreach (var line in lines.Where(l => l.ElementType == type)
                         .OrderBy(l => type == ValuationElementType.Variation ? VariationRefOrder(l.VariationRef) : 0)
                         .ThenBy(l => l.DisplayOrder))
            {
                // Both cumulatives are computed against the CURRENT line amount, so a re-priced
                // line doesn't produce a phantom movement — mirrors the report table.
                var claimed = ValuationCalculations.CumulativeClaimed(PercentIn(entries, line), line.LineAmount);
                var previous = ValuationCalculations.CumulativeClaimed(PercentIn(previousEntries, line), line.LineAmount);
                exportLines.Add(new ValuationExportLine(
                    title, line.ElementType, ExportAreaFor(line), ExportCodeFor(line), ExportTitleFor(line), LineTypeLabel(line.LineType),
                    line.CountsTowardTotals, line.Unit, line.Quantity, line.Rate, line.LineAmount,
                    PercentIn(entries, line), previous, claimed - previous, claimed, line.Comments,
                    line.VariationRef, line.VariationTitle, line.CostCode, line.DisplayOrder,
                    ClientReferenceFor(line)));
            }
        }

        var figures = ValuationSummaryFigures.For(lines, entries, Selected, CertifiedToDateGross, DepositCreditedToDate);
        var summary = new List<ValuationExportSummaryRow>
        {
            new("Original contract sum", figures.ContractSum),
            new("Net variations", figures.NetVariations),
            new("Revised contract sum", figures.RevisedContractSum, Strong: true),
            new("Total works complete", figures.TotalWorksComplete),
        };
        if (Selected is not null)
        {
            summary.Add(new("Works claimed this period",
                exportLines.Where(l => l.CountsTowardTotals).Sum(l => l.ThisPeriod)));
        }
        summary.Add(new($"Retention held ({PctText(figures.RetentionPercent)})", figures.RetentionHeld));
        summary.Add(new($"Retention released ({PctText(figures.RetentionReleasePercent)})", figures.RetentionReleased));
        if (figures.DepositPercent > 0m || figures.DepositReleased != 0m)
        {
            summary.Add(new($"Deposit received ({PctText(figures.DepositPercent)})", figures.DepositReceived));
        }
        summary.Add(new("Certified to date", figures.CertifiedToDate));
        if (figures.DepositPercent > 0m || figures.DepositReleased != 0m)
        {
            summary.Add(new("Payment due before deposit (ex VAT)", figures.PaymentDueBeforeDepositExVat));
            summary.Add(new($"Less deposit released ({PctText(figures.DepositPercent)})", figures.DepositReleased));
        }
        summary.Add(new("Payment due (ex VAT)", figures.PaymentDueExVat, Strong: true));

        return ValuationReportExportWorkbook.Build(
            new ValuationExportMeta(
                $"{Selected?.DisplayName ?? "Valuation report"} — working copy",
                $"Prepared {DateTime.Now.ToString("dd MMM yyyy HH:mm", Gb)} · working copy of the live report",
                IsDraft: true),
            exportLines,
            summary,
            variationOrders is null ? null : ValuationExportPendingVariations.From(variationOrders));
    }

    // ---- Pending variations for the export's Pending tab --------------------
    // Nullable until the register has ANSWERED (the loading convention): an unanswered fetch and
    // "no pending variations" must never look alike. On failure the export still runs — the
    // Pending tab says the register couldn't be read (Build receives null).
    private IReadOnlyList<VariationOrder>? variationOrders;
    private bool variationOrdersFailed;

    private bool PendingVariationsUnanswered => variationOrders is null && !variationOrdersFailed;

    private async Task LoadVariationOrdersAsync()
    {
        try { variationOrders = await Variations.ListForProjectAsync(ProjectId); }
        catch { variationOrdersFailed = true; }
        await InvokeAsync(StateHasChanged);
    }

    // ---- Client references for the export's Client ref column ---------------
    // Same nullable-until-answered rule. On failure the export still runs with the lines' own
    // references — the map is an enrichment, not a gate the whole workbook should fail on.
    private IReadOnlyList<ClientCostReference>? clientReferences;
    private bool clientReferencesFailed;

    private bool ClientReferencesUnanswered => clientReferences is null && !clientReferencesFailed;

    private async Task LoadClientReferencesAsync()
    {
        try { clientReferences = await ClientReferences.ListAsync(ProjectId); }
        catch { clientReferencesFailed = true; }
        await InvokeAsync(StateHasChanged);
    }

    // The line's own reference beats the per-cost-centre map — the same rule snapshot capture
    // freezes by, so the working-copy workbook and the PDF always print the same reference.
    private string ClientReferenceFor(ValuationLineItem line)
    {
        if (!string.IsNullOrWhiteSpace(line.ClientReference)) return line.ClientReference;
        var mapped = clientReferences?.FirstOrDefault(reference =>
            string.Equals(reference.CostCode, line.CostCode, StringComparison.OrdinalIgnoreCase));
        return mapped?.ClientReference ?? "";
    }

    // The area sub-heading this line falls under in the workbook — same shared rule as the
    // report table's title rows (estimate section, else cost-centre name); variations never group.
    private string ExportAreaFor(ValuationLineItem line) =>
        ValuationReportAreas.GroupsByArea(line.ElementType)
            ? ValuationReportAreas.TitleFor(line.SectionName, line.CostCode,
                code => CostCenters.All().FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase))?.Name)
            : "";

    // Same fallbacks as the report table and the snapshot renderer, so every surface agrees.
    private static string ExportCodeFor(ValuationLineItem line) =>
        line.ElementType == ValuationElementType.Variation
            ? (string.IsNullOrWhiteSpace(line.VariationRef) ? line.CostCode : VariationRefs.Padded(line.VariationRef))
            : (string.IsNullOrWhiteSpace(line.CostCode) ? line.SectionCode : line.CostCode);

    private static string ExportTitleFor(ValuationLineItem line)
    {
        if (line.ElementType == ValuationElementType.Variation)
            return string.IsNullOrWhiteSpace(line.Description) ? line.VariationTitle : line.Description;
        if (!string.IsNullOrWhiteSpace(line.Description)) return line.Description;
        return line.SectionName;
    }

    private static string LineTypeLabel(ValuationLineType type) => type switch
    {
        ValuationLineType.Priced => "Priced",
        ValuationLineType.ProvisionalSum => "Provisional sum",
        ValuationLineType.Omit => "Omit",
        ValuationLineType.Declined => "Declined",
        ValuationLineType.Tbc => "TBC",
        _ => type.ToString()
    };

    private static int VariationRefOrder(string variationRef)
    {
        var digits = new string(variationRef.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var number) ? number : int.MaxValue;
    }

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        Store.OnChange += OnStoreChanged;
        // The new-claim form shows the retention terms read-only — re-render when the
        // background fetch lands so it doesn't sit on the "no terms" warning.
        Retention.OnChange += OnStoreChanged;
        // The export file name carries the project reference — re-render when the list lands.
        Projects.OnChanged += OnStoreChanged;
        // Refresh on entry: cached lines/claims render immediately, then update when the
        // background reload lands — so navigating back to this tab never shows stale data.
        Store.Refresh(ProjectId);
        // Warm the retention terms for the new-claim form's read-only summary.
        Retention.Refresh(ProjectId);
        // The Excel export's Pending tab reads the variations register — load it alongside.
        _ = LoadVariationOrdersAsync();
        // …and its Client ref column reads the project's client-reference map.
        _ = LoadClientReferencesAsync();
        // Revalidate the cost-centre master (stale-while-revalidate) — the report table's
        // Variations section and the line form both offer it as a dropdown.
        _ = CostCenters.ListAllAsync();
        // The claim card's stage needs the invoice list from first render (the invoices
        // section reports changes later via OnInvoicedToDateChanged).
        await RefreshInvoicesAsync();
    }

    private void OnStoreChanged()
    {
        if (string.IsNullOrEmpty(selectedClaimId) && Claims.Count > 0)
            selectedClaimId = Claims[0].ValuationClaimId;
        StateHasChanged();
    }

    private void ToggleStartClaim() => showStartClaim = !showStartClaim;

    private Task StartClaimAsync() => GuardAsync(async () =>
    {
        // SpecifyKind: the bound date can arrive as Kind.Local (DateTime.Today default), and the
        // DateTimeOffset ctor rejects a Local value with a zero offset whenever the UK is on BST.
        var date = new DateTimeOffset(DateTime.SpecifyKind(newClaimDate.Date, DateTimeKind.Unspecified), TimeSpan.Zero);
        // Retention percents are omitted deliberately: the server stamps them from the
        // project's retention terms, with the completion date gating the release %.
        // Always seeded from the latest claim: valuations stack month to month, so a new
        // period never starts from zero once a claim exists (null for Claim 1).
        var claim = await Store.StartClaimAsync(new StartValuationClaim(
            ProjectId, NextClaimNumber, date,
            Name: newClaimName.Trim(),
            SeedFromClaimId: LatestClaim?.ValuationClaimId));
        selectedClaimId = claim.ValuationClaimId;
        showStartClaim = false;
        newClaimName = "";
    }, "Couldn't start the claim — the server may be restarting. Please try again.");
}

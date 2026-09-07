using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiCommercialTools
{
    /// <summary>Lines returned per call before the result says it clipped — a whole report is a
    /// few hundred lines at most; the cap is a safety net, not a page size.</summary>
    private const int MaxLines = 600;

    private static string SectionName(ValuationElementType elementType) => elementType switch
    {
        ValuationElementType.ContractWorks => "Contract works",
        ValuationElementType.PcSum => "PC sums",
        ValuationElementType.Contingency => "Contingency",
        ValuationElementType.Variation => "Variations",
        _ => elementType.ToString()
    };

    private static IEnumerable<AiTool> ValuationContextTool()
    {
        return new AiTool[]
        {
            new(
                GetValuationContext,
                "The project's LIVE Valuation Report, line by line: every line's valuationLineItemId, "
                + "section (contract works, PC sums, contingency, variations — with the V-number and "
                + "title on variation lines), cost centre, description, quantity, rate and amount, its "
                + "cumulative % complete and £ claimed on the SELECTED claim, the previous claim's %, "
                + "and this period's increment — plus the claims list (which is selected, its status) "
                + "and the report totals. The selected claim is the newest unless claimId says "
                + "otherwise. Filter with variationRef (\"V01\") to see one variation's lines only. "
                + "Each claim's ValuationClaimId is also its correspondence record id: read_record_emails "
                + "(recordType valuation_claim) reads the mail tagged to the period, and "
                + "file_email_to_record (type ValuationClaim) files an email to it. "
                + "Call this before reviewing or correcting % complete, and before claim_progress.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view.", false),
                    ("claimId", "string", "A claim's id from an earlier call. Defaults to the newest claim.", false),
                    ("variationRef", "string", "Only the lines of this variation — V01, V72.", false),
                    ("section", "string",
                        "Only one section: contract_works, pc_sums, contingency or variations.", false)),
                AiToolKind.Read,
                JpmsRoleSets.AllInternal,
                async (context, input, ct) =>
                {
                    var projectId = AiToolSchema.Text(input, "projectId") ?? context.Scope?.ProjectId;
                    if (string.IsNullOrWhiteSpace(projectId))
                        return Fail("Say which project: pass projectId (list_projects returns ids) or have the user open one of its pages.");

                    var project = await context.Db.Projects.AsNoTracking()
                        .Where(row => row.ProjectId == projectId)
                        .Select(row => new { row.ProjectId, row.Reference, row.Name })
                        .FirstOrDefaultAsync(ct);
                    if (project is null) return Fail($"No project exists with id \"{projectId}\".");

                    var claims = await context.Db.ValuationClaims.AsNoTracking()
                        .Where(row => row.ProjectId == projectId)
                        .OrderByDescending(row => row.ClaimNumber)
                        .ToListAsync(ct);

                    var requestedClaimId = AiToolSchema.Text(input, "claimId");
                    var selected = string.IsNullOrWhiteSpace(requestedClaimId)
                        ? claims.FirstOrDefault()
                        : claims.FirstOrDefault(row => row.ValuationClaimId == requestedClaimId);
                    if (!string.IsNullOrWhiteSpace(requestedClaimId) && selected is null)
                        return Fail($"No claim with id \"{requestedClaimId}\" exists on this project — the claims are listed in a call without claimId.");

                    // "Previous" as the report table shows it: the claim immediately before the
                    // selected one by number, whatever its status.
                    var previous = selected is null
                        ? null
                        : claims.Where(row => row.ClaimNumber < selected.ClaimNumber)
                            .OrderByDescending(row => row.ClaimNumber)
                            .FirstOrDefault();

                    var allLines = await context.Db.ValuationLineItems.AsNoTracking()
                        .Where(row => row.ProjectId == projectId)
                        .OrderBy(row => row.ElementType).ThenBy(row => row.DisplayOrder)
                        .ToListAsync(ct);

                    var claimIds = new List<string>();
                    if (selected is not null) claimIds.Add(selected.ValuationClaimId);
                    if (previous is not null) claimIds.Add(previous.ValuationClaimId);
                    var entries = claimIds.Count == 0
                        ? new List<ClaimLineEntity>()
                        : await context.Db.ClaimLines.AsNoTracking()
                            .Where(row => claimIds.Contains(row.ValuationClaimId))
                            .ToListAsync(ct);
                    var selectedEntries = selected is null
                        ? new Dictionary<string, ClaimLineEntity>()
                        : entries.Where(row => row.ValuationClaimId == selected.ValuationClaimId)
                            .ToDictionary(row => row.ValuationLineItemId);
                    var previousEntries = previous is null
                        ? new Dictionary<string, ClaimLineEntity>()
                        : entries.Where(row => row.ValuationClaimId == previous.ValuationClaimId)
                            .ToDictionary(row => row.ValuationLineItemId);

                    // Filters: one variation, or one section.
                    var variationRef = NormaliseVariationRef(AiToolSchema.Text(input, "variationRef"));
                    var sectionFilter = AiToolSchema.Text(input, "section")?.Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
                    ValuationElementType? elementFilter = sectionFilter switch
                    {
                        "contract_works" or "contract" or "works" => ValuationElementType.ContractWorks,
                        "pc_sums" or "pc" or "pcsum" or "pc_sum" => ValuationElementType.PcSum,
                        "contingency" => ValuationElementType.Contingency,
                        "variations" or "variation" => ValuationElementType.Variation,
                        null or "" => null,
                        _ => null
                    };
                    if (!string.IsNullOrWhiteSpace(sectionFilter) && elementFilter is null)
                        return Fail($"\"{sectionFilter}\" is not a section — use contract_works, pc_sums, contingency or variations.");

                    var filtered = allLines
                        .Where(row => elementFilter is null || row.ElementType == (int)elementFilter)
                        .Where(row => variationRef is null
                                      || (row.ElementType == (int)ValuationElementType.Variation
                                          && string.Equals(NormaliseVariationRef(row.VariationRef), variationRef, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    var shown = filtered.Take(MaxLines).ToList();
                    var lineRows = shown.Select(row =>
                    {
                        selectedEntries.TryGetValue(row.ValuationLineItemId, out var now);
                        previousEntries.TryGetValue(row.ValuationLineItemId, out var before);
                        var counts = (ValuationLineType)row.LineType is not (ValuationLineType.Declined or ValuationLineType.Tbc);
                        return new
                        {
                            row.ValuationLineItemId,
                            section = SectionName((ValuationElementType)row.ElementType),
                            area = row.ElementType == (int)ValuationElementType.Variation ? null : row.SectionName,
                            variationRef = row.ElementType == (int)ValuationElementType.Variation ? row.VariationRef : null,
                            variationTitle = row.ElementType == (int)ValuationElementType.Variation ? row.VariationTitle : null,
                            row.CostCode,
                            row.Description,
                            lineType = ((ValuationLineType)row.LineType).ToString(),
                            countsTowardTotals = counts,
                            row.Unit,
                            row.Quantity,
                            row.Rate,
                            row.LineAmount,
                            percentComplete = counts ? (decimal?)(now?.PercentComplete ?? 0m) : null,
                            claimedToDate = counts ? (decimal?)(now?.CumulativeClaimed ?? 0m) : null,
                            previousPercent = counts && previous is not null ? (decimal?)(before?.PercentComplete ?? 0m) : null,
                            // Derived from the same previous claim as previousPercent (the one
                            // rule, ClaimPeriodBaseline) — never the entry's stored increment.
                            periodIncrement = counts
                                ? (decimal?)((now?.CumulativeClaimed ?? 0m) - (before?.CumulativeClaimed ?? 0m))
                                : null
                        };
                    }).ToList();

                    // Totals over the WHOLE report (not the filter), so a filtered read still says
                    // where the report stands.
                    var counting = allLines.Where(row => (ValuationLineType)row.LineType is not (ValuationLineType.Declined or ValuationLineType.Tbc)).ToList();
                    var contractSum = counting.Where(row => row.ElementType != (int)ValuationElementType.Variation).Sum(row => row.LineAmount);
                    var netVariations = counting.Where(row => row.ElementType == (int)ValuationElementType.Variation).Sum(row => row.LineAmount);
                    var worksComplete = counting.Sum(row => selectedEntries.TryGetValue(row.ValuationLineItemId, out var entry) ? entry.CumulativeClaimed : 0m);

                    return Serialise(new
                    {
                        ok = true,
                        project = $"{project.Reference} — {project.Name}",
                        project.ProjectId,
                        claims = claims.Select(row => new
                        {
                            row.ValuationClaimId,
                            number = row.ClaimNumber,
                            name = string.IsNullOrWhiteSpace(row.Name) ? $"Claim {row.ClaimNumber}" : row.Name,
                            date = row.ClaimDate,
                            status = ((ValuationClaimStatus)row.Status).ToString(),
                            selected = selected is not null && row.ValuationClaimId == selected.ValuationClaimId
                        }).ToList(),
                        selectedClaim = selected is null
                            ? null
                            : new
                            {
                                selected.ValuationClaimId,
                                name = string.IsNullOrWhiteSpace(selected.Name) ? $"Claim {selected.ClaimNumber}" : selected.Name,
                                status = ((ValuationClaimStatus)selected.Status).ToString(),
                                editable = selected.Status == (int)ValuationClaimStatus.Draft,
                                previousClaim = previous is null ? null : (string.IsNullOrWhiteSpace(previous.Name) ? $"Claim {previous.ClaimNumber}" : previous.Name)
                            },
                        totals = new
                        {
                            contractSum,
                            netVariations,
                            revisedContractSum = contractSum + netVariations,
                            totalWorksCompleteOnSelectedClaim = worksComplete
                        },
                        filter = new { variationRef, section = elementFilter?.ToString() },
                        lineCount = filtered.Count,
                        lines = lineRows,
                        clipped = filtered.Count > shown.Count,
                        route = $"/projects/{projectId}/valuation",
                        note = (selected is null
                                   ? "No claim exists yet — % complete can only be recorded on a Draft claim; the user starts one on the Valuation Report tab. "
                                   : selected.Status == (int)ValuationClaimStatus.Draft
                                       ? "The selected claim is Draft, so % complete can be changed: open_modal \"claim_progress\" with the lines to change (valuationLineItemId + cumulative percentComplete). "
                                       : $"The selected claim is {((ValuationClaimStatus)selected.Status)}, so its % complete is locked — say so; a new Draft claim is started on the Valuation Report tab. ")
                               + "Percentages are CUMULATIVE to date. Every figure here is read from the report; quote it, never estimate."
                               + (filtered.Count > shown.Count ? $" Only the first {MaxLines} of {filtered.Count} lines are shown — filter by section or variationRef." : "")
                    });
                })
        };
    }
}

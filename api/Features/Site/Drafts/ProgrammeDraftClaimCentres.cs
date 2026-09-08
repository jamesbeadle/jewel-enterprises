using Jewel.JPMS.Api.Features.Commercial;

namespace Jewel.JPMS.Api.Features.Site.Drafts;

/// <summary>
/// The claim as a draft programme update sees it: every cost centre with priced lines on the
/// project's bill, summed (£ amount, £ claimed on THIS claim) through the pure
/// <see cref="ProgrammeProgressProposal"/>. Read the same way by the capture, the review pane's
/// query and the reviewer's remap, so the three can never disagree about a centre's figure. A
/// line the claim carries no entry for is 0 claimed — the report's own rule.
/// </summary>
internal static class ProgrammeDraftClaimCentres
{
    public static async Task<IReadOnlyList<ProgrammeDraftCostCentre>> LoadAsync(
        JpmsContext context, string projectId, string valuationClaimId, CancellationToken cancellationToken)
    {
        var lines = await context.ValuationLineItems.AsNoTracking()
            .Where(line => line.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        var claimedByLineId = await context.ClaimLines.AsNoTracking()
            .Where(entry => entry.ValuationClaimId == valuationClaimId)
            .ToDictionaryAsync(entry => entry.ValuationLineItemId, entry => entry.CumulativeClaimed, cancellationToken);

        var master = await context.CostCenters.AsNoTracking()
            .Select(centre => new { centre.Code, centre.Name })
            .ToListAsync(cancellationToken);
        var namesByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var centre in master)
            namesByCode.TryAdd(centre.Code.Trim(), centre.Name);

        return ProgrammeProgressProposal.CostCentres(
            lines.Select(line => line.ToModel()),
            claimedByLineId,
            code => namesByCode.GetValueOrDefault(code, ""));
    }
}

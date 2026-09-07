using Jewel.JPMS.Api.Features.Commercial;

namespace Jewel.JPMS.Api.Features.ValuationInvoices;

/// <summary>
/// Re-freezes the summary totals of a project's Preapproved claims after the certified
/// (valuation-invoiced) total changes — issuing or deleting a historic invoice would otherwise
/// leave a preapproved claim showing the certified figure frozen at preapproval time. A claim's
/// certified figure counts only the invoices that came before it (CertifiedBeforeClaim), so
/// issuing the claim's OWN invoice leaves its totals exactly as the statement said them.
/// Draft claims compute live in the UI and Confirmed claims are final, so neither is
/// touched. Call AFTER SaveChanges: the recompute reads the invoice table from the
/// database, not the change tracker.
/// </summary>
internal static class PreapprovedClaimTotals
{
    public static async Task RefreshAsync(JpmsContext context, string projectId, CancellationToken cancellationToken)
    {
        var preapprovedClaims = await context.ValuationClaims
            .Where(claim => claim.ProjectId == projectId && claim.Status == (int)ValuationClaimStatus.Preapproved)
            .ToListAsync(cancellationToken);
        if (preapprovedClaims.Count == 0) return;

        foreach (var claim in preapprovedClaims)
            await ValuationClaimSummary.ApplyTotalsAsync(context, claim, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}

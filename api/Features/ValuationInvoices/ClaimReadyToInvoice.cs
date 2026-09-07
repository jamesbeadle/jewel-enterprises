namespace Jewel.JPMS.Api.Features.ValuationInvoices;

/// <summary>
/// The one check on raising a valuation invoice: it is drawn against the project's LATEST claim,
/// that claim is locked ("We're claiming this" — Preapproved), and no live invoice is drawn
/// against it already. The claim card only OFFERS "Raise invoice" at that moment; the Valuation
/// Invoices add form and the connector's create_valuation_invoice reach the handler directly, and
/// until 2026-09-07 nothing refused a raise over a Draft claim (a statement frozen from figures
/// still being edited), over a claim the report had already moved past (the snapshot capture
/// freezes the latest claim, so the statement would not be that claim's), or a second live
/// invoice over a claim that already had one. A Confirmed latest claim with no live invoice may
/// still be invoiced — the early-confirm nudge lets a claim be confirmed before its invoice
/// exists. Historic (manual) entries record the past and never pass through here.
/// </summary>
internal static class ClaimReadyToInvoice
{
    private sealed record ClaimIdentity(int ClaimNumber, string Name, int Status)
    {
        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? $"Claim {ClaimNumber}" : Name.Trim();
    }

    public static async Task EnsureAsync(
        JpmsContext context, string projectId, string? valuationClaimId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(valuationClaimId))
            throw new InvalidOperationException(
                "An invoice is raised against a locked claim — raise it from the claim card on the Valuation Report tab.");

        var claim = await LockedClaimAsync(context, projectId, valuationClaimId, cancellationToken);
        await EnsureLatestAsync(context, projectId, claim, cancellationToken);
        await EnsureNoLiveInvoiceAsync(context, valuationClaimId, claim, cancellationToken);
    }

    private static async Task<ClaimIdentity> LockedClaimAsync(
        JpmsContext context, string projectId, string valuationClaimId, CancellationToken cancellationToken)
    {
        var claim = await context.ValuationClaims.AsNoTracking()
            .Where(candidate => candidate.ValuationClaimId == valuationClaimId && candidate.ProjectId == projectId)
            .Select(candidate => new ClaimIdentity(candidate.ClaimNumber, candidate.Name, candidate.Status))
            .FirstOrDefaultAsync(cancellationToken);
        if (claim is null)
            throw new InvalidOperationException("That claim does not exist on this project.");

        if (claim.Status == (int)ValuationClaimStatus.Draft)
            throw new InvalidOperationException(
                $"{claim.DisplayName} is still a draft — lock it first with \"We're claiming this\" on the claim card, so the statement frozen behind the invoice is the claim as valued.");
        return claim;
    }

    private static async Task EnsureLatestAsync(
        JpmsContext context, string projectId, ClaimIdentity claim, CancellationToken cancellationToken)
    {
        var latestClaimNumber = await context.ValuationClaims.AsNoTracking()
            .Where(candidate => candidate.ProjectId == projectId)
            .MaxAsync(candidate => candidate.ClaimNumber, cancellationToken);
        if (claim.ClaimNumber == latestClaimNumber) return;

        throw new InvalidOperationException(
            $"{claim.DisplayName} is no longer the latest claim, so a statement frozen now would not be {claim.DisplayName}'s — record its invoice as a historic entry instead.");
    }

    private static async Task EnsureNoLiveInvoiceAsync(
        JpmsContext context, string valuationClaimId, ClaimIdentity claim, CancellationToken cancellationToken)
    {
        var liveInvoiceReference = await context.ValuationInvoices.AsNoTracking()
            .Where(invoice => invoice.ValuationClaimId == valuationClaimId
                              && invoice.Status != (int)ValuationInvoiceStatus.Cancelled)
            .OrderByDescending(invoice => invoice.Number)
            .Select(invoice => invoice.Reference)
            .FirstOrDefaultAsync(cancellationToken);
        if (liveInvoiceReference is null) return;

        throw new InvalidOperationException(
            $"{claim.DisplayName} already has invoice {liveInvoiceReference} against it — amend or cancel that one rather than raising a second.");
    }
}

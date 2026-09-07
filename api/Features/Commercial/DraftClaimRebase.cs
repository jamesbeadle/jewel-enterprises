using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial;

/// <summary>
/// Keeps the claim in progress honest when a valuation line is re-priced underneath it.
///
/// A claim entry stores the money as well as the percentage, and that stored money is what the next
/// report snapshot copies into the document the client is sent (ValuationReportSnapshotCapture) —
/// so leaving it alone after a re-price would issue a figure nobody chose. The percentage is what a
/// QS actually enters and the money is derived from it, so the percentage stays put and the money
/// follows the line, exactly as RecordClaimEntries computes it when the percentage itself is edited.
///
/// Only a Draft claim is touched. Preapproved and Confirmed claims keep the money they were claimed
/// at — that is what the client saw — which is also why a re-price is refused outright while the
/// latest claim is preapproved: its totals are already frozen, so its lines must not move under it.
/// </summary>
internal static class DraftClaimRebase
{
    /// <summary>
    /// Refuses the caller when the claim a snapshot would freeze next is preapproved and already
    /// covers one of these lines. A locked claim elsewhere on the project is none of our business —
    /// only a line it actually carries money for would move underneath it.
    /// </summary>
    public static async Task GuardNoClaimInFlightAsync(
        JpmsContext context, string projectId, IReadOnlyCollection<string> lineItemIds, CancellationToken cancellationToken)
    {
        if (lineItemIds.Count == 0) return;

        var latest = await context.ValuationClaims
            .Where(claim => claim.ProjectId == projectId)
            .OrderByDescending(claim => claim.ClaimNumber)
            .FirstOrDefaultAsync(cancellationToken);
        if (latest is null || latest.Status != (int)ValuationClaimStatus.Preapproved) return;

        var covered = await context.ClaimLines.AnyAsync(
            entry => entry.ValuationClaimId == latest.ValuationClaimId
                     && lineItemIds.Contains(entry.ValuationLineItemId),
            cancellationToken);
        if (covered)
            throw new InvalidOperationException(
                $"Claim {latest.ClaimNumber} is preapproved and its figures are locked, so the lines it covers can't be re-priced underneath it. Confirm it, or reopen it, first.");
    }

    /// <summary>
    /// Re-states the draft claim's money for each re-priced line. Pass only lines whose amount
    /// actually moved; entities are mutated on the caller's change tracker, not saved.
    /// </summary>
    public static async Task ApplyAsync(
        JpmsContext context,
        IReadOnlyCollection<ValuationLineItemEntity> repriced,
        CancellationToken cancellationToken)
    {
        if (repriced.Count == 0) return;

        var amountByLine = repriced.ToDictionary(line => line.ValuationLineItemId, line => line.LineAmount);
        var lineIds = amountByLine.Keys.ToList();

        var entries = await (
                from claimLine in context.ClaimLines
                join claim in context.ValuationClaims on claimLine.ValuationClaimId equals claim.ValuationClaimId
                where lineIds.Contains(claimLine.ValuationLineItemId)
                      && claim.Status == (int)ValuationClaimStatus.Draft
                select new { Entry = claimLine, claim.ProjectId, claim.ClaimNumber })
            .ToListAsync(cancellationToken);

        // The baseline is the line's cumulative on the claim immediately before the draft — the
        // same rule RecordClaimEntries applies (ClaimPeriodBaseline). The previous claim is locked
        // (or older), so its money does not move: the correction lands in the open period.
        foreach (var draft in entries.GroupBy(row => (row.ProjectId, row.ClaimNumber)))
        {
            var previousByLine = await ClaimPeriodBaseline.PreviousCumulativeByLineAsync(
                context, draft.Key.ProjectId, draft.Key.ClaimNumber, cancellationToken);

            foreach (var row in draft)
            {
                if (!amountByLine.TryGetValue(row.Entry.ValuationLineItemId, out var amount)) continue;

                var (cumulative, periodIncrement) = ValuationCalculations.RebasedClaim(
                    row.Entry.PercentComplete, amount,
                    previousByLine.GetValueOrDefault(row.Entry.ValuationLineItemId, 0m));
                row.Entry.CumulativeClaimed = cumulative;
                row.Entry.PeriodIncrement = periodIncrement;
            }
        }
    }
}

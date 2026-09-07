using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Commercial;

/// <summary>
/// The ONE rule for what a claim's "previous" and "this period" figures measure against.
///
/// A line's previous cumulative on claim N is its cumulative on the claim numbered immediately
/// before N — the highest claim number below it — whatever that claim's status. "This period" is
/// works measured since the last statement: the previous claim was the last statement whether or
/// not the client has paid it yet. Payment timing lives in "Certified to date", never in the
/// movement column. (The earlier rule measured against the latest CONFIRMED claim, so a claim
/// started while the one before was only preapproved carried two periods' movement as its own —
/// the statement said one thing and the invoice raised for it another.)
///
/// Every writer of <c>ClaimLineEntity.PeriodIncrement</c> takes its baseline from here, and the
/// readers that must be right whatever was stored (the snapshot capture behind every PDF and
/// spreadsheet, the assistant's report read) derive the figure from here too — so a stored
/// increment is a convenience copy, never the authority.
/// </summary>
internal static class ClaimPeriodBaseline
{
    /// <summary>
    /// The claim immediately before <paramref name="claimNumber"/> on the project, or null for
    /// the first claim.
    /// </summary>
    public static Task<ValuationClaimEntity?> PreviousClaimAsync(
        JpmsContext context, string projectId, int claimNumber, CancellationToken cancellationToken) =>
        context.ValuationClaims
            .Where(claim => claim.ProjectId == projectId && claim.ClaimNumber < claimNumber)
            .OrderByDescending(claim => claim.ClaimNumber)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Cumulative claimed per line on the claim immediately before <paramref name="claimNumber"/>.
    /// A line the previous claim never carried (or the first claim) has no entry — read it as 0.
    /// </summary>
    public static async Task<IReadOnlyDictionary<string, decimal>> PreviousCumulativeByLineAsync(
        JpmsContext context, string projectId, int claimNumber, CancellationToken cancellationToken)
    {
        var previous = await PreviousClaimAsync(context, projectId, claimNumber, cancellationToken);
        if (previous is null) return new Dictionary<string, decimal>();

        return await context.ClaimLines.AsNoTracking()
            .Where(line => line.ValuationClaimId == previous.ValuationClaimId)
            .ToDictionaryAsync(line => line.ValuationLineItemId, line => line.CumulativeClaimed, cancellationToken);
    }

    /// <summary>What this period adds to (or takes off) the line: cumulative now − previous cumulative.</summary>
    public static decimal PeriodIncrement(decimal cumulativeClaimed, IReadOnlyDictionary<string, decimal> previousByLine, string valuationLineItemId) =>
        cumulativeClaimed - previousByLine.GetValueOrDefault(valuationLineItemId, 0m);
}

using Jewel.JPMS.Models;

namespace Jewel.JPMS.Commercial;

// Pure functions for the valuation report summary maths. Kept free of EF/HTTP so the
// figures can be unit-tested directly against the By France workbook.
public static class ValuationCalculations
{
    private const decimal WholePercent = 100m;

    // qty x rate; an Omit line is always stored as a negative magnitude.
    public static decimal LineAmount(ValuationLineType lineType, decimal quantity, decimal rate)
    {
        var raw = quantity * rate;
        return lineType == ValuationLineType.Omit ? -Math.Abs(raw) : raw;
    }

    // Cumulative amount earned on a line at a given % complete.
    public static decimal CumulativeClaimed(decimal percentComplete, decimal lineAmount) =>
        percentComplete / WholePercent * lineAmount;

    // Where a re-priced line leaves the claim in progress. The percentage is what a QS entered, so
    // it stands; the money is derived, so it follows the new line amount. The period increment is
    // what that adds to — or takes back off — the figure the line stood at on the claim before
    // (whatever that claim's status), which was claimed at the old amount and does not move: the
    // correction lands in the open period instead of rewriting a closed one.
    public static (decimal CumulativeClaimed, decimal PeriodIncrement) RebasedClaim(
        decimal percentComplete, decimal lineAmount, decimal previousCumulative)
    {
        var cumulative = CumulativeClaimed(percentComplete, lineAmount);
        return (cumulative, cumulative - previousCumulative);
    }

    // Original contract sum = priced works + PC sums + contingency (excludes variations,
    // and excludes Declined/TBC lines).
    public static decimal ContractSum(IEnumerable<ValuationLineItem> lines) =>
        lines.Where(line => line.ElementType != ValuationElementType.Variation && line.CountsTowardTotals)
             .Sum(line => line.LineAmount);

    // Net of all variation lines (omits net against additions); Declined/TBC excluded.
    public static decimal NetVariations(IEnumerable<ValuationLineItem> lines) =>
        lines.Where(line => line.ElementType == ValuationElementType.Variation && line.CountsTowardTotals)
             .Sum(line => line.LineAmount);

    public static decimal RevisedContractSum(decimal contractSum, decimal netVariations) =>
        contractSum + netVariations;

    // Sum of cumulative claimed across every priced line in this claim.
    public static decimal TotalWorksComplete(IEnumerable<ClaimLine> claimLines) =>
        claimLines.Sum(line => line.CumulativeClaimed);

    public static decimal RetentionHeld(decimal totalWorksComplete, decimal retentionPercent) =>
        totalWorksComplete * retentionPercent / WholePercent;

    public static decimal RetentionReleased(decimal eligibleWorks, decimal retentionReleasePercent) =>
        eligibleWorks * retentionReleasePercent / WholePercent;

    // The cash-up-front deposit the client paid before works started: deposit % of the
    // original contract sum (variations never enlarge it).
    public static decimal DepositReceived(decimal contractSum, decimal depositPercent) =>
        contractSum * depositPercent / WholePercent;

    // Cumulative deposit released back to the client: deposit % of works complete on the
    // contract-side lines (contract works + PC sums + contingency — variations excluded),
    // capped at the deposit received so no claim can ever release more than was paid.
    public static decimal DepositReleased(
        decimal nonVariationWorksComplete, decimal depositPercent, decimal depositReceived) =>
        Math.Min(nonVariationWorksComplete * depositPercent / WholePercent, depositReceived);

    // What a claim actually deducts from its payment due: the release earned to date, less
    // the opening balance settled before the portal began deducting, less the credits
    // already embedded in issued/paid invoices (never negative — a balance ahead of the
    // earned release just waits for the works to catch up).
    public static decimal DepositDeduction(
        decimal depositReleasedToDate, decimal depositReleasedOpening, decimal depositCreditedToDate) =>
        Math.Max(0m, depositReleasedToDate - depositReleasedOpening - depositCreditedToDate);

    // Works complete on the contract-side lines only — the base the deposit releases
    // against. Pairs a claim's entries with their bill lines; entries whose line has been
    // removed (or is Declined/TBC) contribute nothing, matching CountsTowardTotals.
    public static decimal NonVariationWorksComplete(
        IEnumerable<ClaimLine> claimLines, IEnumerable<ValuationLineItem> lines)
    {
        var linesById = lines
            .Where(line => line.ElementType != ValuationElementType.Variation && line.CountsTowardTotals)
            .Select(line => line.ValuationLineItemId)
            .ToHashSet();
        return claimLines
            .Where(claimLine => linesById.Contains(claimLine.ValuationLineItemId))
            .Sum(claimLine => claimLine.CumulativeClaimed);
    }

    public static decimal PaymentDueExVat(
        decimal totalWorksComplete,
        decimal retentionHeld,
        decimal retentionReleased,
        decimal depositReleased,
        decimal certifiedToDate) =>
        totalWorksComplete - retentionHeld + retentionReleased - depositReleased - certifiedToDate;
}

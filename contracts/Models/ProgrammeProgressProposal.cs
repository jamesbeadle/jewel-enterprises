using System.Globalization;

namespace Jewel.JPMS.Models;

// The maths behind a draft programme update, pure so the figures can be tested against a real
// claim without a database. A task's proposed progress is the £-weighted completion of the
// valuation lines on its cost centres: Σ claimed ÷ Σ line amount. Weighting by money (not by
// line count) is what makes a 20-line plumbing centre with one £30k line at 50% read as "half
// done", the way the report itself does. Omits, declined and TBC lines never take part — a
// negative line has no progress, and an unpriced one has nothing to claim against.
public static class ProgrammeProgressProposal
{
    private const decimal WholePercent = 100m;
    private const int PercentDecimals = 1;

    // Shared by the two callers: a % that is never NaN, never over 100, one decimal place.
    public static decimal Percent(decimal claimed, decimal amount)
    {
        if (amount <= 0m) return 0m;
        var percent = Math.Round(claimed / amount * WholePercent, PercentDecimals, MidpointRounding.AwayFromZero);
        return Math.Clamp(percent, 0m, WholePercent);
    }

    // Whether a valuation line contributes to progress at all.
    public static bool ContributesToProgress(ValuationLineItem line) =>
        line.CountsTowardTotals && line.LineAmount > 0m && !string.IsNullOrWhiteSpace(line.CostCode);

    // The claim's cost centres: every centre with at least one contributing line, its lines
    // summed. Lines without an entry on the claim count as 0 claimed (the report's own rule).
    public static IReadOnlyList<ProgrammeDraftCostCentre> CostCentres(
        IEnumerable<ValuationLineItem> lines,
        IReadOnlyDictionary<string, decimal> claimedByLineId,
        Func<string, string> nameForCode)
    {
        return lines
            .Where(ContributesToProgress)
            .GroupBy(line => line.CostCode.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new ProgrammeDraftCostCentre(
                group.Key,
                nameForCode(group.Key),
                group.Count(),
                group.Sum(line => line.LineAmount),
                group.Sum(line => claimedByLineId.GetValueOrDefault(line.ValuationLineItemId, 0m))))
            .OrderBy(centre => centre.CostCode, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }

    // The proposal for a task mapped to these codes: null when nothing priced sits behind them.
    public static decimal? Propose(IReadOnlyList<ProgrammeDraftCostCentre> centres, IReadOnlyList<string> costCodes)
    {
        var matched = Matched(centres, costCodes);
        var amount = matched.Sum(centre => centre.Amount);
        if (amount <= 0m) return null;
        return Percent(matched.Sum(centre => centre.Claimed), amount);
    }

    // "MEC-PLM Plumber · 11 lines · £74,671 of £77,383 claimed (96.5%)" per centre — the trail
    // from the proposed figure back to the report, so a reviewer can check it in one glance.
    public static string Evidence(IReadOnlyList<ProgrammeDraftCostCentre> centres, IReadOnlyList<string> costCodes)
    {
        var matched = Matched(centres, costCodes);
        if (matched.Count == 0) return "";
        return string.Join("; ", matched.Select(centre =>
            $"{centre.CostCode} {centre.Name} · {centre.LineCount} line{(centre.LineCount == 1 ? "" : "s")} · "
            + $"£{Pounds(centre.Claimed)} of £{Pounds(centre.Amount)} claimed ({PercentText(centre.Percent)}%)"));
    }

    // Invariant formatting: the evidence is stored on the draft line and read back everywhere,
    // so it must not depend on the culture of whichever host wrote it.
    private static string Pounds(decimal amount) => amount.ToString("N0", CultureInfo.InvariantCulture);
    private static string PercentText(decimal percent) => percent.ToString("0.#", CultureInfo.InvariantCulture);

    private static IReadOnlyList<ProgrammeDraftCostCentre> Matched(
        IReadOnlyList<ProgrammeDraftCostCentre> centres, IReadOnlyList<string> costCodes) =>
        centres.Where(centre => costCodes.Contains(centre.CostCode, StringComparer.OrdinalIgnoreCase)).ToList();
}

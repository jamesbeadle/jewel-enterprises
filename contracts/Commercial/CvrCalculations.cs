using Jewel.JPMS.Models;

namespace Jewel.JPMS.Commercial;

public static class CvrCalculations
{
    private const decimal WholePercent = 100m;
    private const decimal DaysPerWeek = 7m;
    public const decimal DefaultSiteReportWeight = 0.5m;

    public static decimal ProfitOnCostPercent(decimal profit, decimal cost) =>
        cost == 0 ? 0 : profit / cost * WholePercent;

    public static decimal ProfitOnValuePercent(decimal profit, decimal value) =>
        value == 0 ? 0 : profit / value * WholePercent;

    public static decimal CostToComplete(decimal tenderedPackageCost, decimal completionPercent) =>
        tenderedPackageCost * (WholePercent - completionPercent) / WholePercent;

    public static decimal BlendedCompletionPercent(
        decimal siteReportedPercent, decimal timesheetBurnPercent, decimal siteReportWeight) =>
        siteReportedPercent * siteReportWeight + timesheetBurnPercent * (1m - siteReportWeight);

    public static decimal MovementSincePrior(decimal currentValue, decimal priorValue) =>
        currentValue - priorValue;

    public static decimal WeeksBehind(DateTimeOffset contractCompletion, DateTimeOffset anticipatedCompletion) =>
        (decimal)(anticipatedCompletion - contractCompletion).TotalDays / DaysPerWeek;

    public static decimal WeeklyPrelimRunRate(IReadOnlyList<PrelimForecastEntry> entries)
    {
        if (entries.Count == 0) return 0;
        var weeks = entries.Select(entry => entry.WeekNumber).Distinct().Count();
        return entries.Sum(entry => entry.TenderedAmount) / weeks;
    }

    public static decimal TimeRelatedPrelimOverspend(decimal weeksBehind, decimal weeklyPrelimRunRate) =>
        weeksBehind <= 0 ? 0 : weeksBehind * weeklyPrelimRunRate;
}

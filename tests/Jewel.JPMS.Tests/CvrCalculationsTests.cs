using Jewel.JPMS.Commercial;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

public sealed class CvrCalculationsTests
{
    [Fact]
    public void ProfitOnCostPercent_isProfitOverCost() =>
        Assert.Equal(25m, CvrCalculations.ProfitOnCostPercent(20_000m, 80_000m));

    [Fact]
    public void ProfitOnValuePercent_isProfitOverValue() =>
        Assert.Equal(20m, CvrCalculations.ProfitOnValuePercent(20_000m, 100_000m));

    [Fact]
    public void ProfitOnCostPercent_isZeroWhenCostIsZero() =>
        Assert.Equal(0m, CvrCalculations.ProfitOnCostPercent(5_000m, 0m));

    [Fact]
    public void CostToComplete_isRemainingFractionOfTenderedCost() =>
        Assert.Equal(32_000m, CvrCalculations.CostToComplete(80_000m, 60m));

    [Fact]
    public void BlendedCompletionPercent_weightsSiteAndBurnEqually() =>
        Assert.Equal(60m, CvrCalculations.BlendedCompletionPercent(50m, 70m, CvrCalculations.DefaultSiteReportWeight));

    [Fact]
    public void WeeklyPrelimRunRate_isMeanTenderedPerWeek()
    {
        var entries = new List<PrelimForecastEntry>
        {
            new("e1", "p1", 1, 2_500m, 0m, 0m),
            new("e2", "p1", 2, 2_500m, 0m, 0m),
            new("e3", "p1", 3, 2_500m, 0m, 0m),
            new("e4", "p1", 4, 2_500m, 0m, 0m)
        };
        Assert.Equal(2_500m, CvrCalculations.WeeklyPrelimRunRate(entries));
    }

    [Fact]
    public void TimeRelatedPrelimOverspend_isWeeksBehindTimesRunRate() =>
        Assert.Equal(5_000m, CvrCalculations.TimeRelatedPrelimOverspend(2m, 2_500m));

    [Fact]
    public void TimeRelatedPrelimOverspend_isZeroWhenAheadOfProgramme() =>
        Assert.Equal(0m, CvrCalculations.TimeRelatedPrelimOverspend(-3m, 2_500m));

    [Fact]
    public void WeeksBehind_isPositiveWhenAnticipatedAfterContract()
    {
        var contractCompletion = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(2m, CvrCalculations.WeeksBehind(contractCompletion, contractCompletion.AddDays(14)));
    }

    [Fact]
    public void WorkedExample_groundworksPackage_forecastAndMarginAreHandCheckable()
    {
        var package = new CvrPackageRow("PRJ-1", "Groundworks", 80_000m, 100_000m, 8_000m, 12_000m, 0m);
        Assert.Equal(20_000m, package.OrderProfit);
        Assert.Equal(4_000m, package.VariationProfit);
        Assert.Equal(88_000m, package.CombinedCost);
        Assert.Equal(112_000m, package.CombinedValue);
        Assert.Equal(24_000m, package.CombinedProfit);
        Assert.Equal(25m, package.OrderProfitOnCostPercent);
        Assert.Equal(20m, CvrCalculations.ProfitOnValuePercent(package.OrderProfit, package.OrderValue));

        var costToComplete = CvrCalculations.CostToComplete(80_000m, 60m);
        var forecast = new ForecastComponent("fc1", "PRJ-1", "Groundworks", 30_000m, 25_000m, 2_000m, 5_000m, costToComplete);
        Assert.Equal(32_000m, costToComplete);
        Assert.Equal(94_000m, forecast.ForecastFinalCost);
    }
}

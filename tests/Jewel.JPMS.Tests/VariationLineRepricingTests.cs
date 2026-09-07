using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Xunit;

// Re-pricing an approved variation whose lines have already been claimed against.
//
// Two rules carry the whole feature. First, a submitted row is matched to the report line it names,
// never to the row in the same position — a re-priced line keeps its id and therefore its claim
// history, and only a line no row names is dropped. Second, a settled claim keeps the money it was
// certified at, while the claim still being built keeps its % complete and has its money
// recalculated from the new line amount. These mirror what VariationLineRevision.Plan decides and
// what ReviseVariationOrderLinesHandler / DraftClaimRebase then compute from the same figures.
public sealed class VariationLineRepricingTests
{
    private const decimal OldLineAmount = 25_351.37m;   // V64 as approved
    private const decimal NewLineAmount = 9_742.99m;    // V64 re-priced

    private static VariationLineInput Row(string costCode, decimal rate, string? lineItemId = null) =>
        new(costCode, "", 1m, rate, lineItemId);

    // ---- Matching submitted rows to report lines ---------------------------

    [Fact]
    public void EveryRowNamingItsLine_isRepriced_andNothingIsAddedOrDropped()
    {
        var plan = VariationLineRevision.Plan(
            new[] { "line-a", "line-b" },
            new[] { Row("SUP-DOR", 9_742.99m, "line-a"), Row("CARP-1FX", 500m, "line-b") });

        Assert.Equal(new[] { "line-a", "line-b" }, plan.Repriced.Select(r => r.ValuationLineItemId));
        Assert.Equal(9_742.99m, plan.Repriced[0].Line.Rate);
        Assert.Empty(plan.Added);
        Assert.Empty(plan.Dropped);
    }

    [Fact]
    public void RemovingTheFirstOfTwoRows_dropsThatLine_andLeavesTheOtherOnItsOwnLine()
    {
        // The regression this design exists for: pairing by position would have written B's figures
        // onto line A — the row holding A's claim history — and dropped line B instead.
        var plan = VariationLineRevision.Plan(
            new[] { "line-a", "line-b" },
            new[] { Row("CARP-1FX", 500m, "line-b") });

        Assert.Equal(new[] { "line-b" }, plan.Repriced.Select(r => r.ValuationLineItemId));
        Assert.Equal("CARP-1FX", plan.Repriced[0].Line.CostCode);
        Assert.Equal(new[] { "line-a" }, plan.Dropped);
        Assert.Empty(plan.Added);
    }

    [Fact]
    public void ARowWithNoLine_isAdded_andLeavesTheExistingLinesAlone()
    {
        var plan = VariationLineRevision.Plan(
            new[] { "line-a" },
            new[] { Row("SUP-DOR", 9_742.99m, "line-a"), Row("INT-RDR", 250m) });

        Assert.Single(plan.Repriced);
        Assert.Equal("INT-RDR", Assert.Single(plan.Added).CostCode);
        Assert.Empty(plan.Dropped);
    }

    [Fact]
    public void ARowNamingALineThatIsNotOnTheReport_isAdded_notMatchedOntoSomethingElse()
    {
        var plan = VariationLineRevision.Plan(
            new[] { "line-a" },
            new[] { Row("SUP-DOR", 100m, "line-from-another-variation") });

        Assert.Empty(plan.Repriced);
        Assert.Single(plan.Added);
        Assert.Equal(new[] { "line-a" }, plan.Dropped);
    }

    [Fact]
    public void TwoRowsNamingTheSameLine_repriceItOnce_andTheSecondBecomesANewLine()
    {
        var plan = VariationLineRevision.Plan(
            new[] { "line-a" },
            new[] { Row("SUP-DOR", 100m, "line-a"), Row("SUP-DOR", 200m, "line-a") });

        Assert.Equal(100m, Assert.Single(plan.Repriced).Line.Rate);
        Assert.Equal(200m, Assert.Single(plan.Added).Rate);
        Assert.Empty(plan.Dropped);
    }

    // ---- What the re-price does to the claims ------------------------------

    [Fact]
    public void TheDraftClaim_keepsItsPercentage_andItsMoneyFollowsTheNewLineAmount()
    {
        const decimal percentComplete = 40m;
        var certified = ValuationCalculations.CumulativeClaimed(percentComplete, OldLineAmount);

        var (cumulative, periodIncrement) =
            ValuationCalculations.RebasedClaim(percentComplete, NewLineAmount, certified);

        // The same 40% is now worth less, because the line is worth less.
        Assert.Equal(10_140.548m, certified);
        Assert.Equal(3_897.196m, cumulative);
        Assert.Equal(cumulative, ValuationCalculations.CumulativeClaimed(percentComplete, NewLineAmount));
        // And the difference comes back off in the open period rather than altering what was certified.
        Assert.Equal(-6_243.352m, periodIncrement);
    }

    [Fact]
    public void ALineThatHasNeverBeenCertified_claimsItsWholeCumulativeThisPeriod()
    {
        var (cumulative, periodIncrement) =
            ValuationCalculations.RebasedClaim(50m, NewLineAmount, previousCumulative: 0m);

        Assert.Equal(4_871.495m, cumulative);
        Assert.Equal(cumulative, periodIncrement);
    }

    [Fact]
    public void RePricingToANegativeRate_omitsTheWork_andTheClaimFollowsItNegative()
    {
        var omitted = ValuationCalculations.LineAmount(ValuationLineType.Omit, 1m, -9_742.99m);

        Assert.Equal(-9_742.99m, omitted);
        Assert.Equal(-4_871.495m, ValuationCalculations.RebasedClaim(50m, omitted, 0m).CumulativeClaimed);
    }
}

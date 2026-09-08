using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The maths of a draft programme update: a task's proposed progress is the £-weighted
// completion of the valuation lines on its cost centres. Pure — no database, no clock.
public sealed class ProgrammeProgressProposalTests
{
    private static ValuationLineItem Line(string id, string costCode, decimal amount,
        ValuationLineType lineType = ValuationLineType.Priced, ValuationElementType element = ValuationElementType.ContractWorks) =>
        new(id, "P1", element, "R10", "Plumbing", "", "", lineType, costCode, $"Line {id}", "item", 1m, amount, amount, "", 0);

    private static IReadOnlyList<ProgrammeDraftCostCentre> Centres(
        IEnumerable<ValuationLineItem> lines, IReadOnlyDictionary<string, decimal> claimed) =>
        ProgrammeProgressProposal.CostCentres(lines, claimed, code => code == "MEC-PLM" ? "Plumber" : "Electrician");

    [Fact]
    public void ACentreSumsItsPricedLinesAndWhatTheyHaveClaimed()
    {
        var lines = new[] { Line("a", "MEC-PLM", 30_000m), Line("b", "MEC-PLM", 10_000m), Line("c", "ELE-STD", 5_000m) };
        var claimed = new Dictionary<string, decimal> { ["a"] = 15_000m, ["b"] = 10_000m };

        var centres = Centres(lines, claimed);

        var plumbing = Assert.Single(centres, centre => centre.CostCode == "MEC-PLM");
        Assert.Equal("Plumber", plumbing.Name);
        Assert.Equal(2, plumbing.LineCount);
        Assert.Equal(40_000m, plumbing.Amount);
        Assert.Equal(25_000m, plumbing.Claimed);
        Assert.Equal(62.5m, plumbing.Percent);

        // A line with no entry on the claim has claimed nothing — the report's own rule.
        var electrics = Assert.Single(centres, centre => centre.CostCode == "ELE-STD");
        Assert.Equal(0m, electrics.Claimed);
        Assert.Equal(0m, electrics.Percent);
    }

    [Fact]
    public void OmitsDeclinedAndTbcLinesNeverTakePart()
    {
        var lines = new[]
        {
            Line("a", "MEC-PLM", 10_000m),
            Line("omit", "MEC-PLM", -4_000m, ValuationLineType.Omit, ValuationElementType.Variation),
            Line("declined", "MEC-PLM", 9_000m, ValuationLineType.Declined),
            Line("tbc", "MEC-PLM", 9_000m, ValuationLineType.Tbc)
        };
        var claimed = new Dictionary<string, decimal> { ["a"] = 5_000m, ["omit"] = -4_000m, ["declined"] = 9_000m };

        var plumbing = Assert.Single(Centres(lines, claimed));

        Assert.Equal(1, plumbing.LineCount);
        Assert.Equal(10_000m, plumbing.Amount);
        Assert.Equal(50m, plumbing.Percent);
    }

    [Fact]
    public void AVariationAdditionCountsTowardsItsCentre()
    {
        var lines = new[]
        {
            Line("a", "MEC-PLM", 10_000m),
            Line("v", "MEC-PLM", 10_000m, ValuationLineType.Priced, ValuationElementType.Variation)
        };
        var claimed = new Dictionary<string, decimal> { ["a"] = 10_000m, ["v"] = 0m };

        Assert.Equal(50m, Assert.Single(Centres(lines, claimed)).Percent);
    }

    [Fact]
    public void AProposalIsWeightedByMoneyAcrossTheTasksCentres()
    {
        var lines = new[] { Line("a", "MEC-PLM", 90_000m), Line("b", "ELE-STD", 10_000m) };
        var claimed = new Dictionary<string, decimal> { ["a"] = 90_000m, ["b"] = 0m };
        var centres = Centres(lines, claimed);

        Assert.Equal(90m, ProgrammeProgressProposal.Propose(centres, new[] { "MEC-PLM", "ELE-STD" }));
        Assert.Equal(100m, ProgrammeProgressProposal.Propose(centres, new[] { "MEC-PLM" }));
        Assert.Equal(0m, ProgrammeProgressProposal.Propose(centres, new[] { "ELE-STD" }));
    }

    [Fact]
    public void NoPricedLinesMeansNoProposal()
    {
        var centres = Centres(new[] { Line("a", "MEC-PLM", 1_000m) }, new Dictionary<string, decimal>());

        Assert.Null(ProgrammeProgressProposal.Propose(centres, new[] { "ELE-STD" }));
        Assert.Null(ProgrammeProgressProposal.Propose(centres, Array.Empty<string>()));
    }

    [Fact]
    public void PercentIsOneDecimalAndNeverPast100()
    {
        Assert.Equal(66.7m, ProgrammeProgressProposal.Percent(2m, 3m));
        Assert.Equal(100m, ProgrammeProgressProposal.Percent(130m, 100m));
        Assert.Equal(0m, ProgrammeProgressProposal.Percent(-5m, 100m));
        Assert.Equal(0m, ProgrammeProgressProposal.Percent(5m, 0m));
    }

    [Fact]
    public void CodesMatchWhateverTheirCase()
    {
        var centres = Centres(new[] { Line("a", "MEC-PLM", 1_000m) }, new Dictionary<string, decimal> { ["a"] = 250m });

        Assert.Equal(25m, ProgrammeProgressProposal.Propose(centres, new[] { "mec-plm" }));
    }

    [Fact]
    public void EvidenceReadsAsTheTrailBackToTheReport()
    {
        var lines = new[] { Line("a", "MEC-PLM", 30_000m), Line("b", "MEC-PLM", 10_000m) };
        var claimed = new Dictionary<string, decimal> { ["a"] = 15_000m, ["b"] = 10_000m };
        var centres = Centres(lines, claimed);

        var evidence = ProgrammeProgressProposal.Evidence(centres, new[] { "MEC-PLM" });

        Assert.Equal("MEC-PLM Plumber · 2 lines · £25,000 of £40,000 claimed (62.5%)", evidence);
        Assert.Equal("", ProgrammeProgressProposal.Evidence(centres, new[] { "ELE-STD" }));
    }

    [Fact]
    public void ADraftLineAppliesTheReviewersFigureOverTheProposal()
    {
        var proposedOnly = new ProgrammeDraftLine("l1", "d1", "t1", "Plumbing", 40m, 62.5m, new[] { "MEC-PLM" },
            ProgrammeMappingSource.Rule, "", IsIncluded: true, ReviewedPercent: null);
        var reviewed = proposedOnly with { ReviewedPercent = 70m };
        var unchanged = proposedOnly with { CurrentPercent = 62.5m };
        var excluded = proposedOnly with { IsIncluded = false };

        Assert.Equal(62.5m, proposedOnly.PercentToApply);
        Assert.True(proposedOnly.ChangesTheTask);
        Assert.Equal(70m, reviewed.PercentToApply);
        Assert.False(unchanged.ChangesTheTask);
        Assert.False(excluded.ChangesTheTask);
    }
}

using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The trade-word rulebook behind a draft programme update, tried against the By France
// programme (the REV8 task titles) and the cost centres its valuation actually prices. A rule
// names the whole family a word could mean; the claim narrows it to what is priced.
public sealed class ProgrammeCostCentreRulesTests
{
    // The By France claim's cost centres (contract works, 2026-09-08).
    private static readonly string[] ByFranceCodes =
    {
        "PRELIMS-SMG", "ENABLE-SKP", "PRELIMS-PRO", "SCAFF-STD", "SUB-GWK", "ENABLE-DEM", "UTIL-STD",
        "MEC-PLM", "MASON-BRK", "STR-STL", "CARP-1FX", "INT-PLS", "ROOF-RFR", "PRELIMS-LAB", "WDR-ALU",
        "SUP-DOR", "ELE-STD", "MEC-AC", "STAIR-TIM", "Omit's", "FLR-LVT", "EXTW-PAV", "EXTW-FEN"
    };

    [Theory]
    [InlineData("Second Floor — Plumbing 2nd Fix", "MEC-PLM")]
    [InlineData("First Floor — Electrics 2nd Fix", "ELE-STD")]
    [InlineData("First Floor — Air Conditioning 2nd Fix", "MEC-AC")]
    [InlineData("Second Floor — FF/SF Staircase", "STAIR-TIM")]
    [InlineData("Second Floor — Window Installation TBC", "WDR-ALU")]
    [InlineData("Ground Floor — Entrance Door & Window Installation TBC", "WDR-ALU")]
    [InlineData("External Works — Fencing to Boundary TBC", "EXTW-FEN")]
    [InlineData("Ground Floor — Insulation/Dry Lining/Plaster", "INT-PLS")]
    public void ByFranceTitlesLandOnThePricedCentre(string title, string expected)
    {
        var codes = ProgrammeCostCentreRules.Match(title, ByFranceCodes);

        Assert.Equal(new[] { expected }, codes);
    }

    [Fact]
    public void ATitleSpanningTradesMapsToEveryPricedCentreItNames()
    {
        var codes = ProgrammeCostCentreRules.Match("External Works — Excavation & masonry - Entrance TBC", ByFranceCodes);

        Assert.Equal(new[] { "SUB-GWK", "MASON-BRK" }, codes);
    }

    [Fact]
    public void TheLongerPhraseSilencesTheShorterOneItContains()
    {
        var present = new[] { "CARP-1FX", "CARP-2FX" };

        Assert.Equal(new[] { "CARP-2FX" }, ProgrammeCostCentreRules.Match("Second Floor — Carpentry 2nd Fix", present));
        Assert.Equal(new[] { "CARP-1FX" }, ProgrammeCostCentreRules.Match("Roof — Carpentry First Fix", present));
        Assert.Equal(new[] { "CARP-1FX", "CARP-2FX" }, ProgrammeCostCentreRules.Match("Carpentry", present));
    }

    [Fact]
    public void AFamilyNarrowsToWhatTheClaimPrices()
    {
        Assert.Equal(new[] { "TIL-STN" }, ProgrammeCostCentreRules.Match("First Floor — Tiling Installation", new[] { "TIL-STN", "MEC-PLM" }));
        Assert.Empty(ProgrammeCostCentreRules.Match("First Floor — Tiling Installation", ByFranceCodes));
    }

    [Fact]
    public void ATitleWithNoTradeWordMatchesNothing()
    {
        Assert.Empty(ProgrammeCostCentreRules.Match("Handover meeting", ByFranceCodes));
        Assert.Empty(ProgrammeCostCentreRules.Match("", ByFranceCodes));
        Assert.Empty(ProgrammeCostCentreRules.Match("Plumbing", Array.Empty<string>()));
    }

    [Fact]
    public void MatchingIgnoresCaseAndPunctuation()
    {
        Assert.Equal(new[] { "MEC-PLM" }, ProgrammeCostCentreRules.Match("PLUMBING (2nd fix)", ByFranceCodes));
        Assert.Equal(new[] { "MEC-PLM" }, ProgrammeCostCentreRules.Match("plumbing", new[] { "mec-plm" }).Select(code => code.ToUpperInvariant()));
    }

    [Fact]
    public void AWordInsideAnotherWordDoesNotMatch()
    {
        // "electrics" is a word; "dielectric" is not it.
        Assert.Empty(ProgrammeCostCentreRules.Match("Dielectric testing", ByFranceCodes));
    }

    [Fact]
    public void EveryRuleNamesRealCodesAndPlainPhrases()
    {
        foreach (var rule in ProgrammeCostCentreRules.All)
        {
            Assert.NotEmpty(rule.Phrases);
            Assert.NotEmpty(rule.CostCodes);
            Assert.All(rule.Phrases, phrase => Assert.Equal(phrase, ProgrammeCostCentreRules.Normalise(phrase)));
            Assert.All(rule.CostCodes, code => Assert.Matches("^[A-Z]+(-[A-Z0-9]+)+$", code));
        }
    }
}

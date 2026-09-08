using Jewel.JPMS.Api.Features.Labour.Commands;
using Jewel.JPMS.Api.Features.Xero;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>Which of several live bills sharing a number the coding run takes (8 Sep 2026): the
/// schedule's net first, then the newest, and a tie is nobody's to break.</summary>
public sealed class ReissueChoiceTests
{
    private static readonly DateTime Nine = new(2026, 9, 8, 9, 0, 0);
    private static readonly DateTime Ten = new(2026, 9, 8, 10, 0, 0);

    [Fact]
    public void TheBillWhoseNetIsTheSchedulesWinsWhateverItsAge()
    {
        var (chosen, why) = ReissueChoice.Choose(new[] { Bill("a", 6270m, Ten), Bill("b", 6470m, Nine) }, 6470m);

        Assert.Equal("b", chosen!.InvoiceId);
        Assert.Equal("2 live bills carry the number \"INV-1252\": took b (DRAFT, net £6,470.00) because its net is the schedule's, over a (DRAFT, net £6,270.00). ", why);
    }

    [Fact]
    public void AmongSeveralMatchingTheNewestWins()
    {
        var (chosen, why) = ReissueChoice.Choose(new[] { Bill("a", 6470m, Nine), Bill("b", 6470m, Ten) }, 6470m);

        Assert.Equal("b", chosen!.InvoiceId);
        Assert.Contains("because of those whose net is the schedule's it is the newest", why);
    }

    [Fact]
    public void WithNoneMatchingTheNewestWins()
    {
        var (chosen, _) = ReissueChoice.Choose(new[] { Bill("a", 6000m, Ten), Bill("b", 6100m, Nine) }, 6470m);

        Assert.Equal("a", chosen!.InvoiceId);
    }

    [Fact]
    public void TwoTheRunCannotTellApartAreNobodysToBreak()
    {
        var (chosen, why) = ReissueChoice.Choose(new[] { Bill("a", 6470m, Ten), Bill("b", 6470m, Ten) }, 6470m);

        Assert.Null(chosen);
        Assert.Null(why);
    }

    [Fact]
    public void OneBillIsSimplyTheBill()
    {
        var only = Bill("a", 6000m, Ten);

        var (chosen, why) = ReissueChoice.Choose(new[] { only }, 6470m);

        Assert.Same(only, chosen);
        Assert.Null(why);
    }

    private static XeroBillSummary Bill(string id, decimal net, DateTime updatedUtc) =>
        new(id, "DRAFT", "INV-1252", null, "Jewel Property Serve Ltd", new DateTime(2026, 8, 31), "Exclusive",
            net, 0m, net, 0m, 0m, net, 1, "REVERSECHARGES", updatedUtc);
}

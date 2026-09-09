using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// Where an Extension of Time sits on the programme: from the completion it extends (baselined,
// else current) out by the days claimed, the days granted inside it.
public sealed class ProgrammeExtensionPlacementTests
{
    private static readonly DateTimeOffset Day0 = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    private static Request Eot(string id, int? claimed, int? granted, RequestType kind = RequestType.ExtensionOfTime) =>
        new(id, "proj-1", kind, $"EOT-{id}", "Steel delay", "", RequestStatus.Open, null, "pm@jewelbb.co.uk", Day0, null,
            EotDaysClaimed: claimed, EotDaysGranted: granted);

    private static ProgrammeMovement Movement(DateTimeOffset? baselineCompletion, DateTimeOffset? currentCompletion) =>
        new(baselineCompletion, currentCompletion, 0, Array.Empty<ProgrammeDelayEvent>());

    [Fact]
    public void OnlyExtensionsOfTimeAreListed()
    {
        var requests = new[] { Eot("1", 10, null), Eot("2", 5, null, RequestType.NoticeOfDelay) };

        var rows = ProgrammeExtensionPlacement.Place(requests, Movement(Day0, Day0));

        Assert.Equal(new[] { "1" }, rows.Select(row => row.Eot.RequestId));
    }

    [Fact]
    public void TheBarRunsFromBaselinedCompletionByDaysClaimedWithGrantedInsideIt()
    {
        var row = Assert.Single(ProgrammeExtensionPlacement.Place(new[] { Eot("1", 14, 10) }, Movement(Day0.AddDays(100), Day0.AddDays(110))));

        Assert.Equal(Day0.AddDays(100), row.From);
        Assert.Equal(Day0.AddDays(114), row.ClaimedTo);
        Assert.Equal(Day0.AddDays(110), row.GrantedTo);
        Assert.True(row.HasBar);
    }

    [Fact]
    public void WithoutABaselineTheCurrentCompletionIsExtended()
    {
        var row = Assert.Single(ProgrammeExtensionPlacement.Place(new[] { Eot("1", 14, null) }, Movement(null, Day0.AddDays(110))));

        Assert.Equal(Day0.AddDays(110), row.From);
        Assert.Null(row.GrantedTo);
    }

    [Fact]
    public void AnEotWithoutDaysListsButDrawsNothing()
    {
        var row = Assert.Single(ProgrammeExtensionPlacement.Place(new[] { Eot("1", null, null) }, Movement(Day0, Day0)));

        Assert.False(row.HasBar);
        Assert.Equal(0, row.DaysClaimed);
    }

    [Fact]
    public void WithNoProgrammeThereIsNothingToExtend()
    {
        var row = Assert.Single(ProgrammeExtensionPlacement.Place(new[] { Eot("1", 14, 10) }, Movement(null, null)));

        Assert.Null(row.From);
        Assert.False(row.HasBar);
    }
}

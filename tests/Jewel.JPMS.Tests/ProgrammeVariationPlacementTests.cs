using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// Where a variation lands on the programme and what it pushes — the reading the Gantt and the
// connector's get_programme share. Variations are read as they are; the push is the programme's
// own record; planned dates are never moved by either.
public sealed class ProgrammeVariationPlacementTests
{
    private static readonly DateTimeOffset Day0 = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    private static ProgrammeTask Task(string id, string title, int startDay, int endDay) =>
        new(id, "proj-1", title, Day0.AddDays(startDay), Day0.AddDays(endDay), 0m, null);

    private static VariationOrder Variation(string id, int number, VariationOrderStatus status, string? costCode, IReadOnlyList<VariationLineInput>? lines = null) =>
        new(id, "proj-1", "", number, $"VOQ-{number:0000}", $"Variation {number}", "", status,
            null, null, null, null, 0m, costCode, Day0.AddDays(number), "qs@jewelbb.co.uk", DraftLines: lines);

    private static ProgrammeTaskCostCentre Mapping(string taskId, string costCode) =>
        new($"map-{taskId}-{costCode}", "proj-1", taskId, costCode);

    private static ProgrammeVariationEffect Effect(string variationId, string taskId, int days) =>
        new($"eff-{variationId}-{taskId}", "proj-1", variationId, taskId, days, "", "pm@jewelbb.co.uk", Day0);

    private static readonly IReadOnlyList<ProgrammeTask> Tasks = new[]
    {
        Task("t-steel", "Structural steel", 0, 20),
        Task("t-roof", "Roof", 20, 40)
    };

    private static readonly IReadOnlyList<ProgrammeTaskCostCentre> Mappings = new[]
    {
        Mapping("t-steel", "STR-STL"),
        Mapping("t-roof", "ROOF-RFR")
    };

    [Fact]
    public void EveryVariationButARejectedOneIsOnTheProgramme()
    {
        var variations = new[]
        {
            Variation("v-quoting", 1, VariationOrderStatus.Quoting, "STR-STL"),
            Variation("v-issued", 2, VariationOrderStatus.Issued, "STR-STL"),
            Variation("v-awaiting", 3, VariationOrderStatus.AwaitingArchitectInstruction, "STR-STL"),
            Variation("v-approved", 4, VariationOrderStatus.Approved, "STR-STL"),
            Variation("v-rejected", 5, VariationOrderStatus.Rejected, "STR-STL")
        };

        var rows = ProgrammeVariationPlacement.Place(variations, Tasks, Mappings, Array.Empty<ProgrammeVariationEffect>());

        Assert.Equal(new[] { "v-quoting", "v-issued", "v-awaiting", "v-approved" }, rows.Select(row => row.Variation.VariationOrderId));
        Assert.Equal(new[] { false, false, false, true }, rows.Select(row => row.IsFirm));
    }

    [Fact]
    public void AVariationLandsOnEveryTaskMappedToOneOfItsCostCentres()
    {
        var lines = new[] { new VariationLineInput("ROOF-RFR", "Extra rooflight", 1m, 500m) };
        var variation = Variation("v-1", 1, VariationOrderStatus.Issued, "str-stl", lines);

        var row = Assert.Single(ProgrammeVariationPlacement.Place(new[] { variation }, Tasks, Mappings, Array.Empty<ProgrammeVariationEffect>()));

        Assert.Equal(new[] { "t-steel", "t-roof" }, row.LandsOn.Select(task => task.ProgrammeTaskId));
    }

    [Fact]
    public void AnUnpricedVariationLandsNowhereButStillLists()
    {
        var variation = Variation("v-1", 1, VariationOrderStatus.Quoting, null);

        var row = Assert.Single(ProgrammeVariationPlacement.Place(new[] { variation }, Tasks, Mappings, Array.Empty<ProgrammeVariationEffect>()));

        Assert.Empty(row.LandsOn);
        Assert.Empty(ProgrammeVariationPlacement.CostCodesOf(variation));
    }

    [Fact]
    public void ARecordedPushRunsFromTheTasksPlannedEndAndNeverMovesIt()
    {
        var variation = Variation("v-1", 1, VariationOrderStatus.Approved, "STR-STL");
        var effects = new[] { Effect("v-1", "t-steel", 7) };

        var row = Assert.Single(ProgrammeVariationPlacement.Place(new[] { variation }, Tasks, Mappings, effects));
        var push = Assert.Single(row.Pushes);

        Assert.Equal(Day0.AddDays(20), push.Start);
        Assert.Equal(Day0.AddDays(27), push.End);
        Assert.True(push.IsFirm);
        Assert.Equal(Day0.AddDays(20), Tasks[0].PlannedEnd);
    }

    [Fact]
    public void APushOnATaskThatNoLongerExistsIsSkipped()
    {
        var variation = Variation("v-1", 1, VariationOrderStatus.Issued, "STR-STL");
        var effects = new[] { Effect("v-1", "t-removed", 7) };

        var row = Assert.Single(ProgrammeVariationPlacement.Place(new[] { variation }, Tasks, Mappings, effects));

        Assert.Empty(row.Pushes);
    }
}

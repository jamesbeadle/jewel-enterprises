namespace Jewel.JPMS.Models;

// One push a variation makes on one task, placed on the timeline: the segment runs from the
// task's planned end for the recorded days. IsFirm once the variation is approved — until then
// the Gantt draws it dashed, because an unapproved variation is a push that MAY happen.
public sealed record ProgrammePushSegment(
    ProgrammeVariationEffect Effect,
    ProgrammeTask Task,
    DateTimeOffset Start,
    DateTimeOffset End,
    bool IsFirm);

// A variation as the programme sees it: the document, the tasks it lands on (every task mapped
// to one of its cost centres), and the pushes recorded against it.
public sealed record ProgrammeVariationRow(
    VariationOrder Variation,
    IReadOnlyList<ProgrammeTask> LandsOn,
    IReadOnlyList<ProgrammePushSegment> Pushes)
{
    public bool IsFirm => Variation.Status == VariationOrderStatus.Approved;
    public DateTimeOffset Marker => Variation.IssuedAt ?? Variation.CreatedAt;
}

// Pure placement of a project's variations against its programme. Shared by the Gantt and the
// Programme Agent so both agree on where a variation lands and what it pushes. No I/O, no clock.
public static class ProgrammeVariationPlacement
{
    public static IReadOnlyList<ProgrammeVariationRow> Place(
        IReadOnlyList<VariationOrder> variations,
        IReadOnlyList<ProgrammeTask> tasks,
        IReadOnlyList<ProgrammeTaskCostCentre> costCentres,
        IReadOnlyList<ProgrammeVariationEffect> effects)
    {
        var tasksById = tasks.ToDictionary(task => task.ProgrammeTaskId);
        return variations
            .Where(IsOnTheProgramme)
            .OrderBy(variation => variation.Number)
            .Select(variation => new ProgrammeVariationRow(
                variation,
                TasksLandedOn(variation, tasks, costCentres),
                PushesOf(variation, effects, tasksById)))
            .ToList()
            .AsReadOnly();
    }

    // Rejected variations leave the programme; everything else — approved or not — is on it.
    public static bool IsOnTheProgramme(VariationOrder variation) =>
        variation.Status != VariationOrderStatus.Rejected;

    // A variation names its cost centres twice: the primary code, and one per line of its
    // staged build-up. A task mapped to any of them is one the variation lands on.
    public static IReadOnlyList<string> CostCodesOf(VariationOrder variation)
    {
        var codes = new List<string>();
        if (!string.IsNullOrWhiteSpace(variation.CostCode)) codes.Add(variation.CostCode);
        codes.AddRange(variation.DraftLines?.Select(line => line.CostCode) ?? Enumerable.Empty<string>());
        return codes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }

    private static IReadOnlyList<ProgrammeTask> TasksLandedOn(
        VariationOrder variation, IReadOnlyList<ProgrammeTask> tasks, IReadOnlyList<ProgrammeTaskCostCentre> costCentres)
    {
        var codes = CostCodesOf(variation);
        var landedTaskIds = costCentres
            .Where(mapping => codes.Contains(mapping.CostCode, StringComparer.OrdinalIgnoreCase))
            .Select(mapping => mapping.ProgrammeTaskId)
            .ToHashSet();
        return tasks.Where(task => landedTaskIds.Contains(task.ProgrammeTaskId)).ToList().AsReadOnly();
    }

    private static IReadOnlyList<ProgrammePushSegment> PushesOf(
        VariationOrder variation, IReadOnlyList<ProgrammeVariationEffect> effects, IReadOnlyDictionary<string, ProgrammeTask> tasksById)
    {
        var isFirm = variation.Status == VariationOrderStatus.Approved;
        return effects
            .Where(effect => effect.VariationOrderId == variation.VariationOrderId)
            .Where(effect => tasksById.ContainsKey(effect.ProgrammeTaskId))
            .Select(effect => Segment(effect, tasksById[effect.ProgrammeTaskId], isFirm))
            .ToList()
            .AsReadOnly();
    }

    private static ProgrammePushSegment Segment(ProgrammeVariationEffect effect, ProgrammeTask task, bool isFirm) =>
        new(effect, task, task.PlannedEnd, task.PlannedEnd.AddDays(effect.DelayDays), isFirm);
}

using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Features.Site;

public partial class ProgrammeGanttChart
{
    [Parameter, EditorRequired] public string ProjectId { get; set; } = "";
    [Parameter] public IReadOnlyList<ProgrammeTask> Tasks { get; set; } = Array.Empty<ProgrammeTask>();
    [Parameter] public IReadOnlyList<ProgrammeTaskLink> Links { get; set; } = Array.Empty<ProgrammeTaskLink>();
    [Parameter] public IReadOnlyList<ProgrammeBaselineTask> BaselineTasks { get; set; } = Array.Empty<ProgrammeBaselineTask>();
    /// <summary>The programme measured against its baseline — the slip badges read from it.</summary>
    [Parameter, EditorRequired] public ProgrammeMovement Movement { get; set; } = default!;
    /// <summary>The variations on the programme, placed against the tasks by cost centre, with their recorded pushes.</summary>
    [Parameter] public IReadOnlyList<ProgrammeVariationRow> VariationRows { get; set; } = Array.Empty<ProgrammeVariationRow>();
    /// <summary>The extensions of time, placed from the completion they extend.</summary>
    [Parameter] public IReadOnlyList<ProgrammeExtensionRow> ExtensionRows { get; set; } = Array.Empty<ProgrammeExtensionRow>();
    [Parameter] public bool CanEdit { get; set; }
    [Parameter] public bool Busy { get; set; }
    [Parameter] public EventCallback<string> OnRemoveLink { get; set; }
    /// <summary>Writes an edited task through; true means it took and the editor closes.</summary>
    [Parameter, EditorRequired] public Func<ProgrammeTaskEditor.TaskEdit, Task<bool>> SaveTask { get; set; } = default!;
    /// <summary>Removes a task (and its dependencies); true means it took and the editor closes.</summary>
    [Parameter, EditorRequired] public Func<string, Task<bool>> RemoveTask { get; set; } = default!;
    /// <summary>Records a variation's push on a task; true means it took and the editor closes.</summary>
    [Parameter, EditorRequired] public Func<ProgrammeVariationEffectEditor.EffectEdit, Task<bool>> SaveEffect { get; set; } = default!;
    [Parameter, EditorRequired] public Func<string, Task<bool>> RemoveEffect { get; set; } = default!;
    /// <summary>Records an EOT's days claimed / granted; true means it took and the editor closes.</summary>
    [Parameter, EditorRequired] public Func<ProgrammeExtensionDaysEditor.DaysEdit, Task<bool>> SaveExtensionDays { get; set; } = default!;

    private string? editingTaskId;
    private ProgrammeTimeline Timeline { get; set; } = ProgrammeTimeline.Spanning(Array.Empty<DateTimeOffset>());

    // The ruler spans every date the chart will draw, so a push or an extension past the last
    // task never falls off the end.
    protected override void OnParametersSet() => Timeline = ProgrammeTimeline.Spanning(DatesDrawn());

    private IEnumerable<DateTimeOffset> DatesDrawn()
    {
        foreach (var task in Tasks) { yield return task.PlannedStart; yield return task.PlannedEnd; }
        foreach (var baselined in BaselineTasks) { yield return baselined.PlannedStart; yield return baselined.PlannedEnd; }
        foreach (var push in VariationRows.SelectMany(row => row.Pushes)) yield return push.End;
        foreach (var extension in ExtensionRows.Where(row => row.HasBar)) yield return extension.ClaimedTo!.Value;
    }

    private void ToggleEdit(ProgrammeTask task) =>
        editingTaskId = editingTaskId == task.ProgrammeTaskId ? null : task.ProgrammeTaskId;

    private ProgrammeBaselineTask? BaselineTaskFor(string programmeTaskId) =>
        BaselineTasks.FirstOrDefault(b => b.ProgrammeTaskId == programmeTaskId);

    private int SlipDaysFor(string programmeTaskId) =>
        Movement.DelayEvents.FirstOrDefault(e => e.ProgrammeTaskId == programmeTaskId)?.SlipDays ?? 0;

    private IEnumerable<ProgrammeTaskLink> PredecessorsOf(string programmeTaskId) =>
        Links.Where(l => l.SuccessorTaskId == programmeTaskId);

    private string TaskTitle(string programmeTaskId) =>
        Tasks.FirstOrDefault(t => t.ProgrammeTaskId == programmeTaskId)?.Title ?? "(removed task)";

    private IEnumerable<ProgrammePushSegment> PushesOn(string programmeTaskId) =>
        VariationRows.SelectMany(row => row.Pushes).Where(push => push.Task.ProgrammeTaskId == programmeTaskId);

    private VariationOrder VariationOf(ProgrammePushSegment push) =>
        VariationRows.First(row => row.Variation.VariationOrderId == push.Effect.VariationOrderId).Variation;

    private string VariationNumber(ProgrammePushSegment push) => VariationOf(push).DisplayNumber;
}

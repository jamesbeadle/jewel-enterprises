namespace Jewel.JPMS.Models;

// The programme's own record of what a variation does to one of its tasks: the days the task is
// pushed by it (decision 2026-09-09). Held on the programme side — like ProgrammeTaskCostCentre —
// so the variation document is never touched: the programme READS variations and keeps its own
// facts about them. Overlay only: the task's planned dates never move because of one; the Gantt
// draws the push as a segment on the task's end, dashed until the variation is approved. One
// effect per variation per task; recording again replaces it. No FK, by-id, like every JPMS link.
public sealed record ProgrammeVariationEffect(
    string ProgrammeVariationEffectId,
    string ProjectId,
    string VariationOrderId,
    string ProgrammeTaskId,
    int DelayDays,
    string Note,
    string RecordedByEmail,
    DateTimeOffset RecordedAt);

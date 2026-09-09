using Jewel.JPMS.Api.Features.Site.Commands;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class SiteAndProgressActions
{
    // Mirrors Features/Site/Commands' ProgrammeVariationEffectAuthorisation (Director + PM).
    private static IEnumerable<AiAction> ProgrammeVariationActions() => new AiAction[]
    {
        new AiAction(
            Name: "record_programme_variation_effect",
            Area: "Progress & programme",
            Description: "Records what a variation does to the programme: the task it pushes and "
                + "by how many days. The programme's OWN record — the variation document is not "
                + "touched, and the task's planned dates do not move; the Programme tab draws the "
                + "push as a segment on the task's end, dashed until the variation is approved. "
                + "One effect per variation per task: recording again re-states the days and note.",
            CommandType: typeof(RecordProgrammeVariationEffect),
            ResultType: typeof(ProgrammeVariationEffect),
            AuthorisationType: typeof(ProgrammeVariationEffectAuthorisation),
            ValidationType: typeof(RecordProgrammeVariationEffectValidation),
            VisibleTo: ProgrammePlanners,
            EmailStamps: new[] { "RecordedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "variationOrderId comes from list_variations or get_programme's "
                + "variationsOnProgramme (find_by_reference resolves V72). programmeTaskId comes "
                + "from get_programme — its landsOn list names the tasks the variation's cost "
                + "centres map to, the likeliest ones it pushes. delayDays is calendar days, at "
                + "least 1. Refused for a rejected variation."),

        new AiAction(
            Name: "remove_programme_variation_effect",
            Area: "Progress & programme",
            Description: "Removes a recorded variation push from the programme. The variation and "
                + "the task are untouched — only the programme's own record of the push goes.",
            CommandType: typeof(RemoveProgrammeVariationEffect),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(ProgrammeVariationEffectAuthorisation),
            ValidationType: typeof(RemoveProgrammeVariationEffectValidation),
            VisibleTo: ProgrammePlanners,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "programmeVariationEffectId is the effect id on a push in get_programme's "
                + "variationsOnProgramme.")
    };
}

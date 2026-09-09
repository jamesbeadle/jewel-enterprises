using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

// The two sections beneath the tasks on the Programme tab, as get_programme reports them: the
// variations on the programme (placed by cost centre, with their recorded pushes) and the
// extensions of time (drawn from completion by days claimed / granted). The same calculators
// the Gantt uses, so the connector and the screen never disagree about where a variation lands.
internal static partial class AiDeliveryTools
{
    private static async Task<object> ProgrammeVariationRowsAsync(AiToolContext context, string projectId, ProgrammeDetail detail, CancellationToken ct)
    {
        var variations = await Query<ListVariationOrdersForProject, IReadOnlyList<VariationOrder>>(
            context, new ListVariationOrdersForProject(projectId), ct);
        return ProgrammeVariationPlacement.Place(variations, detail.Tasks, detail.CostCentres, detail.VariationEffects)
            .Select(VariationRow);
    }

    private static async Task<object> ProgrammeExtensionRowsAsync(AiToolContext context, string projectId, ProgrammeDetail detail, CancellationToken ct)
    {
        var requests = await Query<ListRequestsForProject, IReadOnlyList<Request>>(
            context, new ListRequestsForProject(projectId), ct);
        var movement = ProgrammeMovementCalculator.Compare(detail.Tasks, detail.BaselineTasks);
        return ProgrammeExtensionPlacement.Place(requests, movement).Select(ExtensionRow);
    }

    private static object VariationRow(ProgrammeVariationRow row) => new
    {
        row.Variation.VariationOrderId,
        number = row.Variation.DisplayNumber,
        row.Variation.Title,
        status = row.Variation.Status.DisplayName(),
        isApproved = row.IsFirm,
        costCodes = ProgrammeVariationPlacement.CostCodesOf(row.Variation),
        marker = row.Marker,
        landsOn = row.LandsOn.Select(task => new { task.ProgrammeTaskId, task.Title }),
        pushes = row.Pushes.Select(push => new
        {
            push.Effect.ProgrammeVariationEffectId,
            push.Task.ProgrammeTaskId,
            taskTitle = push.Task.Title,
            push.Effect.DelayDays,
            push.Effect.Note,
            from = push.Start,
            to = push.End,
            isFirm = push.IsFirm,
            push.Effect.RecordedByEmail,
            push.Effect.RecordedAt
        }),
        route = $"/projects/{row.Variation.ProjectId}/variations/{row.Variation.VariationOrderId}"
    };

    private static object ExtensionRow(ProgrammeExtensionRow row) => new
    {
        row.Eot.RequestId,
        row.Eot.Reference,
        row.Eot.Title,
        status = row.Eot.Status.DisplayName(),
        daysClaimed = row.Eot.EotDaysClaimed,
        daysGranted = row.Eot.EotDaysGranted,
        row.Eot.RelatedNodRequestId,
        extendsCompletionFrom = row.From,
        claimedTo = row.ClaimedTo,
        grantedTo = row.GrantedTo,
        route = $"/projects/{row.Eot.ProjectId}/requests/view/{row.Eot.RequestId}"
    };
}

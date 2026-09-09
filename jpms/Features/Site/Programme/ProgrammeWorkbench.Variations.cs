using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Features.Site;

// The variations and extensions of time the programme shows beneath its tasks. Variations are
// READ from their own store and placed by cost centre; what the programme knows about a
// variation's push is its own record (ProgrammeVariationEffect) and is written here. An EOT's
// days are the request's own facts, restated onto the request.
public partial class ProgrammeWorkbench
{
    [Inject] private IVariationStore Variations { get; set; } = default!;

    // Null until the store has answered — an empty list is a real "no variations".
    private IReadOnlyList<VariationOrder>? variations;

    private IReadOnlyList<ProgrammeTaskCostCentre> CostCentres => programme?.CostCentres ?? Array.Empty<ProgrammeTaskCostCentre>();
    private IReadOnlyList<ProgrammeVariationEffect> VariationEffects => programme?.VariationEffects ?? Array.Empty<ProgrammeVariationEffect>();

    private IReadOnlyList<ProgrammeVariationRow> VariationRows =>
        ProgrammeVariationPlacement.Place(variations ?? Array.Empty<VariationOrder>(), Tasks, CostCentres, VariationEffects);

    private IReadOnlyList<ProgrammeExtensionRow> ExtensionRows =>
        ProgrammeExtensionPlacement.Place(RequestRegister.ForProject(ProjectId, RequestType.ExtensionOfTime), Movement);

    private async Task LoadVariationsAsync() =>
        variations = await Variations.ListForProjectAsync(ProjectId, CancellationToken.None);

    private Task<bool> SaveEffectAsync(ProgrammeVariationEffectEditor.EffectEdit edit) =>
        WriteAsync(new RecordProgrammeVariationEffect(
            ProjectId, edit.VariationOrderId, edit.ProgrammeTaskId, edit.DelayDays, edit.Note, Auth.CurrentUser!.Email),
            "Couldn't record the programme effect. Please try again.");

    private Task<bool> RemoveEffectAsync(string programmeVariationEffectId) =>
        WriteAsync(new RemoveProgrammeVariationEffect(programmeVariationEffectId), "Couldn't remove the programme effect. Please try again.");

    // The register is the writer for requests, so the days go through it (and it re-reads); the
    // command restates the request as it stands with only the days changed.
    private async Task<bool> SaveExtensionDaysAsync(ProgrammeExtensionDaysEditor.DaysEdit edit)
    {
        var eot = RequestRegister.ForProject(ProjectId, RequestType.ExtensionOfTime).FirstOrDefault(request => request.RequestId == edit.RequestId);
        if (eot is null || busy) return false;
        busy = true;
        error = null;
        try
        {
            await RequestRegister.UpdateAsync(RestatedWithDays(eot, edit), CancellationToken.None);
            return true;
        }
        catch
        {
            error = "Couldn't record the days on the Extension of Time. Please try again.";
            return false;
        }
        finally
        {
            busy = false;
        }
    }

    private static UpdateRequestDetails RestatedWithDays(Request eot, ProgrammeExtensionDaysEditor.DaysEdit edit) =>
        new(eot.RequestId, eot.Reference, eot.Title, eot.Description, eot.Status, eot.Value, eot.ResponseText,
            eot.RespondedByEmail, eot.ImpliesVariation, eot.DrawingRef, eot.ResponseDue, eot.RelatedDrawingSpec,
            eot.InternalNotes, eot.ClientNotes,
            EotDaysClaimed: edit.DaysClaimed, EotDaysGranted: edit.DaysGranted);
}

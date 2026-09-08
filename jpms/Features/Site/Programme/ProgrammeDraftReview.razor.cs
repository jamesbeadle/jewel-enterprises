using System.Globalization;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Features.Site;

public partial class ProgrammeDraftReview
{
    [Inject] private ICommandSender Commands { get; set; } = default!;
    [Inject] private AuthService Auth { get; set; } = default!;

    [Parameter, EditorRequired] public string ProjectId { get; set; } = "";
    /// <summary>The draft under review. The pane writes every change through and hands the fresh
    /// draft back through <see cref="OnChanged"/>; the workbench owns it.</summary>
    [Parameter, EditorRequired] public ProgrammeDraftDetail Detail { get; set; } = default!;
    [Parameter] public EventCallback<ProgrammeDraftDetail> OnChanged { get; set; }
    /// <summary>Applied or discarded — the draft is gone and the programme may have moved.</summary>
    [Parameter] public EventCallback OnClosed { get; set; }
    /// <summary>Whether the signed-in user may act on the draft (the programme-editing roles).
    /// A reader sees the draft; only an editor gets the controls and the automatic ask.</summary>
    [Parameter] public bool CanEdit { get; set; }

    private bool busy;
    private bool suggesting;
    private string? error;
    private bool showApplyDialog;

    private string? mappingEditorLineId;
    private readonly HashSet<string> editorCodes = new(StringComparer.OrdinalIgnoreCase);

    // Which draft Claude has been asked about from this pane, so a re-render never asks twice.
    private string? suggestedForDraftId;

    private string Strapline =>
        $"{Detail.MappedCount} of {Detail.Lines.Count} task{(Detail.Lines.Count == 1 ? "" : "s")} matched to cost centres"
        + (Detail.UnmappedCount > 0 ? $", {Detail.UnmappedCount} to map" : "")
        + $" · {Detail.ChangeCount} would change · opened {DateTimeText(Detail.Draft.CreatedAt)} by {Detail.Draft.CreatedByEmail}";

    private string ApplyLabel => $"Apply {Detail.ChangeCount} change{(Detail.ChangeCount == 1 ? "" : "s")}";

    private string ApplySummary =>
        $"{Detail.ChangeCount} task{(Detail.ChangeCount == 1 ? "" : "s")} will take the percentage shown; the rest stay as they are.";

    private string NameFor(string costCode) =>
        Detail.CostCentres.FirstOrDefault(centre => string.Equals(centre.CostCode, costCode, StringComparison.OrdinalIgnoreCase))?.Name ?? costCode;

    private static string PercentText(decimal percent) => percent.ToString("0.#", CultureInfo.InvariantCulture) + "%";

    private static string EditText(decimal? percent) =>
        percent is { } value ? value.ToString("0.#", CultureInfo.InvariantCulture) : "";

    // Claude is asked once per draft, the first time the pane sees it with tasks still unmatched
    // — the server stamps the ask, so a later open never repeats it; the toolbar button does.
    protected override async Task OnParametersSetAsync()
    {
        if (!CanEdit) return;
        if (Detail.Draft.SuggestionsRequestedAt is not null || Detail.UnmappedCount == 0) return;
        if (suggestedForDraftId == Detail.Draft.ProgrammeDraftId) return;
        suggestedForDraftId = Detail.Draft.ProgrammeDraftId;
        await SuggestAsync();
    }

    private async Task SuggestAsync()
    {
        if (busy || suggesting) return;
        suggesting = true;
        error = null;
        try
        {
            var refreshed = await Commands.SendAsync(new SuggestProgrammeDraftMappings(Detail.Draft.ProgrammeDraftId), CancellationToken.None);
            await OnChanged.InvokeAsync(refreshed);
        }
        catch (CommandFailedException refusal)
        {
            error = refusal.Message;
        }
        catch
        {
            error = "Couldn't ask Claude for suggestions. Please try again.";
        }
        finally
        {
            suggesting = false;
        }
    }

    // ---- One line at a time ------------------------------------------------------------------

    private Task SetIncludedAsync(ProgrammeDraftLine line, bool included) =>
        ReviewAsync(line, line.CostCodes, included, line.ReviewedPercent);

    // Typing a figure is also a tick: nobody types a number they do not want applied. Clearing
    // the box goes back to the proposal (and unticks a line that has none).
    private Task SetPercentAsync(ProgrammeDraftLine line, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ReviewAsync(line, line.CostCodes, line.ProposedPercent is not null && line.IsIncluded, null);
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var typed))
            return Task.CompletedTask;
        return ReviewAsync(line, line.CostCodes, true, Math.Clamp(typed, 0m, 100m));
    }

    private void OpenMappingEditor(ProgrammeDraftLine line)
    {
        mappingEditorLineId = line.ProgrammeDraftLineId;
        editorCodes.Clear();
        foreach (var code in line.CostCodes) editorCodes.Add(code);
    }

    private void CloseMappingEditor() => mappingEditorLineId = null;

    private void TickCentre(string costCode, bool ticked)
    {
        if (ticked) editorCodes.Add(costCode);
        else editorCodes.Remove(costCode);
    }

    private async Task SaveMappingAsync(ProgrammeDraftLine line)
    {
        var codes = Detail.CostCentres.Select(centre => centre.CostCode).Where(editorCodes.Contains).ToList();
        if (await ReviewAsync(line, codes, true, null))
            mappingEditorLineId = null;
    }

    private async Task<bool> ReviewAsync(ProgrammeDraftLine line, IReadOnlyList<string> costCodes, bool included, decimal? reviewedPercent)
    {
        if (busy) return false;
        busy = true;
        error = null;
        try
        {
            var saved = await Commands.SendAsync(
                new ReviewProgrammeDraftLine(line.ProgrammeDraftLineId, costCodes, included, reviewedPercent), CancellationToken.None);
            var lines = Detail.Lines.Select(existing => existing.ProgrammeDraftLineId == saved.ProgrammeDraftLineId ? saved : existing).ToList();
            await OnChanged.InvokeAsync(Detail with { Lines = lines });
            return true;
        }
        catch (CommandFailedException refusal)
        {
            error = refusal.Message;
            return false;
        }
        catch
        {
            error = "Couldn't save that change. Please try again.";
            return false;
        }
        finally
        {
            busy = false;
        }
    }

    // ---- The whole draft ---------------------------------------------------------------------

    private async Task ApplyAsync()
    {
        if (busy) return;
        busy = true;
        error = null;
        try
        {
            await Commands.SendAsync(new ApplyProgrammeDraft(Detail.Draft.ProgrammeDraftId, Auth.CurrentUser!.Email), CancellationToken.None);
            showApplyDialog = false;
            await OnClosed.InvokeAsync();
        }
        catch (CommandFailedException refusal)
        {
            error = refusal.Message;
        }
        catch
        {
            error = "Couldn't apply the draft. Please try again.";
        }
        finally
        {
            busy = false;
        }
    }

    private async Task DiscardAsync()
    {
        if (busy) return;
        busy = true;
        error = null;
        try
        {
            await Commands.SendAsync(new DiscardProgrammeDraft(Detail.Draft.ProgrammeDraftId, Auth.CurrentUser!.Email), CancellationToken.None);
            await OnClosed.InvokeAsync();
        }
        catch (CommandFailedException refusal)
        {
            error = refusal.Message;
        }
        catch
        {
            error = "Couldn't discard the draft. Please try again.";
        }
        finally
        {
            busy = false;
        }
    }
}

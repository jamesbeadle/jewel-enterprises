using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.RecordLinks;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;

namespace Jewel.JPMS.Pages;

public partial class ProjectVariationDetail
{
    // A staged build-up owns the figure (its total is the estimate), and approval moves the
    // money story to the contract Value — the edit only exists while the estimate is free-standing.
    private bool CanEditEstimate =>
        CanManage && order is not null && order.Status.IsPreApproval()
        && (order.DraftLines is null || order.DraftLines.Count == 0);

    private async Task SaveTitle()
    {
        if (busy || order is null) return;
        renameError = null;
        var title = renameTitle.Trim();
        if (title.Length == 0) { renameError = "A title is required."; return; }
        // An unchanged title is a cancel by another name — no round-trip, no audit noise.
        if (string.Equals(title, order.Title, StringComparison.Ordinal)) { renamingOrder = false; return; }
        try
        {
            busy = true;
            order = await Variations.RenameAsync(VariationOrderId, title);
            renamingOrder = false;
        }
        catch (CommandFailedException ex) { renameError = ex.Message; }
        catch { renameError = "Couldn't save the new title. Please try again."; }
        finally { busy = false; }
    }

    private async Task ApproveWithLines(VariationApprovePanel.ApproveRequest request)
    {
        if (busy) return;
        error = null;
        try
        {
            busy = true;
            // The panel supplies the priced build-up; the value and primary cost code are derived
            // from it. The server writes one report line per entry under this variation's V-ref.
            order = await Variations.ApproveAsync(VariationOrderId, request.PrimaryCostCode, request.Total, request.Lines);
            approveModalOpen = false;
            Valuation.Refresh(ProjectId); // pull the freshly written variation lines for the breakdown
            await ReloadAsync();
        }
        catch (System.Exception ex)
        {
            error = string.IsNullOrWhiteSpace(ex.Message) ? "Couldn't approve the variation order. Please try again." : ex.Message;
        }
        finally { busy = false; }
    }

    private async Task IssueWorkOrder()
    {
        if (busy || ApprovedOrder is not { } approved) return;
        error = null;
        try
        {
            busy = true;
            await Variations.IssueWorkOrderForVariationOrderAsync(approved.VariationOrderId);
            await ReloadAsync();
        }
        catch (CommandFailedException ex) { error = ex.Message; }
        catch { error = "Couldn't issue the work order. Please try again."; }
        finally { busy = false; }
    }

}

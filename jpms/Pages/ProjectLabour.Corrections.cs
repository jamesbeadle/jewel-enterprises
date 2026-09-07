namespace Jewel.JPMS.Pages;

public partial class ProjectLabour
{
    // ---- Corrections (2026-09-07, the accountant's ask): the MD/FD's way back once approval
    //      has posted. Unapprove puts a day back to Submitted (cost withdrawn); Move re-homes a
    //      day — any status — on the right project as it stands. Both take a mandatory reason
    //      that lands on the audit trail; the server holds the gate and the downstream guards
    //      (signed-off week part, settlement cover, Xero coding run) and says which step undoes
    //      them, and its words are shown as they come. ----

    // Mirrors the API's LabourRoleSets.CorrectApprovedTime (MD/FD/Admin) — the same members as
    // the over-budget override today, mirrored separately so the two can part later.
    private bool CanCorrect => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector);

    private TimesheetDetail? unapproving;
    private TimesheetDetail? moving;
    private string correctionReason = "";
    private string? correctionError;
    private bool isCorrecting;

    private string moveToProjectId = "";
    private string? moveBudgetBlock;
    private bool moveOverBudget;

    // Every live project except this one — every offered row is a real move. Project labels
    // come from the shared list read model (refreshed on first open of the modal).
    private IReadOnlyList<SearchSelect.Option> MoveProjectOptions =>
        (Projects.Current ?? (IReadOnlyList<Project>)Array.Empty<Project>())
            .Where(project => project.ProjectId != ProjectId)
            .Select(project => new SearchSelect.Option(project.ProjectId, $"{project.Reference} — {project.Name}"))
            .ToList();

    private void StartUnapprove(TimesheetDetail timesheet)
    {
        moving = null;
        unapproving = timesheet;
        correctionReason = ""; correctionError = null;
    }

    private async Task StartMove(TimesheetDetail timesheet)
    {
        unapproving = null;
        moving = timesheet;
        moveToProjectId = ""; moveBudgetBlock = null; moveOverBudget = false;
        correctionReason = ""; correctionError = null;
        // Labels only — a failed project read leaves the picker empty; the modal still says why.
        try { if (Projects.Current is null) await Projects.RefreshAsync(CancellationToken.None); }
        catch { correctionError = "Couldn't load the project list — reload and try again."; }
    }

    private void CloseCorrection()
    {
        unapproving = null;
        moving = null;
        correctionError = null;
    }

    private async Task ConfirmUnapproveAsync()
    {
        if (unapproving is null || string.IsNullOrWhiteSpace(correctionReason)) return;
        isCorrecting = true; correctionError = null;
        try
        {
            await Labour.UnapproveTimesheetAsync(ProjectId, unapproving.TimesheetId, correctionReason.Trim());
            selectedIds.Remove(unapproving.TimesheetId);
            CloseCorrection();
        }
        catch (Exception failure)
        {
            correctionError = DescribeFailure(failure, "Could not reverse the approval — check your connection and try again.");
        }
        finally { isCorrecting = false; }
    }

    private async Task ConfirmMoveAsync()
    {
        if (moving is null || moveToProjectId == "" || string.IsNullOrWhiteSpace(correctionReason)) return;
        isCorrecting = true; correctionError = null;
        try
        {
            var result = await Labour.MoveTimesheetAsync(ProjectId, moving.TimesheetId, moveToProjectId,
                correctionReason.Trim(), allowOverBudget: moveOverBudget);
            if (!result.Moved)
            {
                // The destination's budget hard-block, in the server's own words — the tick
                // below it is the MD/FD's deliberate override, never a default.
                moveBudgetBlock = result.BudgetBlockReason;
                moveOverBudget = false;
                return;
            }
            selectedIds.Remove(moving.TimesheetId);
            CloseCorrection();
        }
        catch (Exception failure)
        {
            correctionError = DescribeFailure(failure, "Could not move the day — check your connection and try again.");
        }
        finally { isCorrecting = false; }
    }
}

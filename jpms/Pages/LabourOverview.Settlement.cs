using Jewel.JPMS.Features.Labour;
using static Jewel.JPMS.Features.Labour.LabourDisplay;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Pages;

public partial class LabourOverview
{
    // ---- Settlement (scope §6, §6a) -------------------------------------------------------------

    private string? expandedScheduleWorkerId;
    private bool isRunningCoding;
    private IReadOnlyList<XeroCodingRunResult>? codingResults;
    private IReadOnlyList<XeroCodingRunResult>? codingPlan;
    private CodingResetModal? codingResetModal;
    private LabourBillApproveDialog? billApproveDialog;

    // Dry run, then confirm (2026-09-03, item E): "Code month into Xero" first asks the run what
    // it WOULD do — recode bill X, stage a draft, skip because Y — and the confirm modal shows
    // that list; only the confirm writes. A duplicate is far cheaper to prevent than to find.
    private async Task PreviewCodingAsync()
    {
        isRunningCoding = true; actionError = null; codingResults = null; codingPlan = null;
        try { codingPlan = await Labour.RunXeroCodingAsync(year, month, null, dryRun: true); }
        catch (Exception) { actionError = "Couldn't preview the coding run — check the error bar and try again. Nothing was written."; }
        finally { isRunningCoding = false; }
    }

    private async Task RunCodingAsync()
    {
        isRunningCoding = true; actionError = null; codingResults = null;
        try { codingResults = await Labour.RunXeroCodingAsync(year, month, null); codingPlan = null; }
        catch (Exception) { codingPlan = null; actionError = "The coding run failed — nothing may have reached Xero. Check the error bar and try again."; }
        finally { isRunningCoding = false; }
    }

    private void CancelCodingPlan() => codingPlan = null;

    private void OpenCodingReset(WorkerSettlementSchedule schedule) => codingResetModal!.Open(schedule);

    private void OpenBillApprove(WorkerSettlementSchedule schedule) => billApproveDialog!.Open(schedule);

    private void OpenSettleLine(WorkerSettlementSchedule schedule) => settlementLineModal!.Open(schedule);

    private async Task RemoveSettlementLineAsync(string workerSettlementLineId)
    {
        actionError = null;
        try { await Labour.RemoveSettlementLineAsync(year, month, workerSettlementLineId); }
        catch (Exception) { actionError = "Could not remove the line — try again."; }
    }
}

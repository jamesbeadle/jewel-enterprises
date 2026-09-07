using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Labour;
using static Jewel.JPMS.Api.Features.Labour.Commands.XeroCodingWording;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed partial class RunXeroCodingHandler
{
    private async Task<XeroCodingRunResult> CodeWorkerMonthAsync(WorkerRun run, CancellationToken cancellationToken)
    {
        var schedule = run.Schedule;

        // Gate 1: sign-off is the only trigger. Nothing reaches Xero from unsigned data.
        if (!schedule.FullySignedOff)
            return run.Skip("Not every week with approved time is signed off — sign the month off first.");

        // Gate 2: run-once — a worker-month already written stays written unless its bill is gone.
        var (alreadyCoded, preface) = await AlreadyCodedAsync(run, cancellationToken);
        if (alreadyCoded is not null) return alreadyCoded;

        // Gate 3: the mapping must answer for every line — a gap skips, it never guesses.
        var (xeroLines, gap) = ScheduleAsXeroLines(run);
        if (gap is not null) return run.Skip(gap);

        // Both routes need a settlement counterparty: covers key on it, and a recognised bill is
        // marked as settlement against it.
        if (schedule.SubcontractorId is null || string.IsNullOrWhiteSpace(schedule.SubcontractorName))
            return run.Skip("The worker has no settlement identity — link a subcontractor company or flag "
                + "them a sole trader (Workers page, or the allocation page's inline fix) so the run "
                + "knows whose bill to look for.");

        var (billId, wasCovered, tooMany) = FindWorkersBill(run);
        if (tooMany is not null) return run.Skip(tooMany);
        return billId is not null
            ? await RecodeExistingBillAsync(run, billId, wasCovered, xeroLines, preface, cancellationToken)
            : await StageDraftAsync(run, xeroLines, preface, cancellationToken);
    }

    /// <summary>
    /// Gate 2: a worker-month the automation has already written stays written — UNLESS the bill
    /// it wrote has since been deleted or voided in Xero (item D: a deleted staged draft must not
    /// trap the month). Re-running otherwise is a deliberate human decision, taken by resetting
    /// the coding outcome (with a reason) first. Returns the skip, or the preface a re-code of a
    /// vanished bill carries.
    /// </summary>
    private async Task<(XeroCodingRunResult? Skip, string Preface)> AlreadyCodedAsync(WorkerRun run, CancellationToken cancellationToken)
    {
        if (run.LatestRun is null
            || (XeroCodingOutcome)run.LatestRun.Outcome is not (XeroCodingOutcome.BillRecoded or XeroCodingOutcome.DraftStaged))
            return (null, "");
        var previous = (XeroCodingOutcome)run.LatestRun.Outcome;
        var stamp = $"{previous}, {run.LatestRun.RunAt:dd MMM HH:mm}";
        if (string.IsNullOrEmpty(run.LatestRun.XeroBillId))
            return (run.Skip($"Already coded ({stamp}). Reset the coding outcome to run this month again."), "");
        XeroBillSummary? written;
        try { written = await xero.GetBillAsync(run.LatestRun.XeroBillId, cancellationToken); }
        catch (XeroCallFailedException failure)
        {
            return (run.Skip($"Already coded ({stamp}) — and Xero couldn't confirm bill {run.LatestRun.XeroBillId} still stands: {failure.Message}"), "");
        }
        if (written is not null && !IsGone(written.Status))
            return (run.Skip($"Already coded ({stamp}): bill {BillLabel(written)} is {written.Status}, £{written.Total:N2}. "
                + "Reset the coding outcome to run this month again."), "");
        var preface = $"The bill this month was coded to on {run.LatestRun.RunAt:dd MMM HH:mm} ({previous}) is "
            + (written is null ? "no longer in Xero" : written.Status.ToLowerInvariant()) + " — coding again. ";
        return (null, preface);
    }
}

using Jewel.JPMS.Api.Features.Xero;
using static Jewel.JPMS.Api.Features.Labour.Commands.XeroCodingWording;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed partial class RunXeroCodingHandler
{
    /// <summary>What gate 2 found across the party: the workers already written to a bill that
    /// still stands (their skips), and which bill each was written to.</summary>
    private sealed record RunOnceVerdict(IReadOnlyList<XeroCodingRunResult> Skips, IReadOnlyDictionary<string, string> StandingBills)
    {
        public bool HoldsEveryWorkerOf(CodingParty party) => Skips.Count == party.Workers.Count;
    }

    /// <summary>
    /// Gate 2 for the party: run-once. Every worker-month on a bill that still stands holds the
    /// party. A company bill is written whole, so one open worker (never coded, reset, or coded
    /// to a bill now gone) writes every worker on it again — see <see cref="WriteAgain"/>.
    /// </summary>
    private async Task<RunOnceVerdict> AlreadyCodedAsync(CodingParty party, CancellationToken cancellationToken)
    {
        var skips = new List<XeroCodingRunResult>();
        var standingBills = new Dictionary<string, string>();
        foreach (var worker in party.Workers)
        {
            var (skip, preface, standingBillId) = await AlreadyCodedAsync(worker, cancellationToken);
            worker.Preface = preface;
            if (skip is null) continue;
            skips.Add(skip);
            if (standingBillId is not null) standingBills[worker.WorkerId] = standingBillId;
        }
        return new RunOnceVerdict(skips, standingBills);
    }

    /// <summary>Once the party's bill is known to be the one its coded workers stand on, those
    /// workers are written again with the open ones, and their outcome says why.</summary>
    private static void WriteAgain(CodingParty party, RunOnceVerdict runOnce)
    {
        var open = party.Workers.Where(worker => runOnce.Skips.All(skip => skip.WorkerId != worker.WorkerId)).ToList();
        foreach (var skip in runOnce.Skips)
        {
            var codedWorker = party.Worker(skip.WorkerId);
            codedWorker.Preface = $"Already coded ({RunStamp(codedWorker)}) — written again because {party.CounterpartyName}'s bill is coded whole "
                + $"and {party.NamesOf(open)}'s {party.MonthStart:MMM yyyy} is open. ";
        }
    }

    /// <summary>
    /// A worker-month the automation has already written stays written — UNLESS the bill it
    /// wrote has since been deleted or voided in Xero (item D: a deleted staged draft must not
    /// trap the month). Re-running otherwise is a deliberate human decision, taken by resetting
    /// the coding outcome (with a reason) first. Returns the skip, or the preface a re-code of a
    /// vanished bill carries; StandingBillId is the bill a skipped month was written to.
    /// </summary>
    private async Task<(XeroCodingRunResult? Skip, string Preface, string? StandingBillId)> AlreadyCodedAsync(
        WorkerRun run, CancellationToken cancellationToken)
    {
        if (run.LatestRun is null
            || (XeroCodingOutcome)run.LatestRun.Outcome is not (XeroCodingOutcome.BillRecoded or XeroCodingOutcome.DraftStaged))
            return (null, "", null);
        var previous = (XeroCodingOutcome)run.LatestRun.Outcome;
        var stamp = RunStamp(run);
        if (string.IsNullOrEmpty(run.LatestRun.XeroBillId))
            return (run.Skip($"Already coded ({stamp}). Reset the coding outcome to run this month again."), "", null);
        XeroBillSummary? written;
        try { written = await xero.GetBillAsync(run.LatestRun.XeroBillId, cancellationToken); }
        catch (XeroCallFailedException failure)
        {
            return (run.Skip($"Already coded ({stamp}) — and Xero couldn't confirm bill {run.LatestRun.XeroBillId} still stands: {failure.Message}"), "", null);
        }
        if (written is not null && !IsGone(written.Status))
            return (run.Skip($"Already coded ({stamp}): bill {BillLabel(written)} is {written.Status}, £{written.Total:N2}. "
                + "Reset the coding outcome to run this month again."), "", written.InvoiceId);
        var preface = $"The bill this month was coded to on {run.LatestRun.RunAt:dd MMM HH:mm} ({previous}) is "
            + (written is null ? "no longer in Xero" : written.Status.ToLowerInvariant()) + " — coding again. ";
        return (null, preface, null);
    }

    private static string RunStamp(WorkerRun run) => $"{(XeroCodingOutcome)run.LatestRun!.Outcome}, {run.LatestRun.RunAt:dd MMM HH:mm}";

    /// <summary>A worker already written to one standing bill while the party's bill resolves to
    /// another (or to none) would put that worker's month on two bills — a person decides.</summary>
    private static string? StandingBillMismatch(CodingParty party, IReadOnlyDictionary<string, string> standingBills, string? billId)
    {
        var mismatch = standingBills.FirstOrDefault(pair => pair.Value != billId);
        if (mismatch.Key is null) return null;
        var worker = party.Worker(mismatch.Key);
        return $"{worker.WorkerName}'s {party.MonthStart:MMM yyyy} is already coded to bill {mismatch.Value}, but {party.Owner}'s bill for the month "
            + (billId is null ? "was not found" : $"resolves to {billId}")
            + " — reset their coding outcome, or mark the right bill as settlement on the Cost allocation page's Labour tab, then re-run.";
    }
}

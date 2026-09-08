using static Jewel.JPMS.Api.Features.Labour.Commands.XeroCodingWording;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed partial class RunXeroCodingHandler
{
    private async Task<IReadOnlyList<XeroCodingRunResult>> CodePartyMonthAsync(CodingParty party, CancellationToken cancellationToken)
    {
        // Gate 1: sign-off is the only trigger. Nothing reaches Xero from unsigned data — and a
        // company bill is coded whole, so one unsigned worker holds every worker on it.
        var unsigned = party.Workers.Where(worker => !worker.Schedule.FullySignedOff)
            .ToDictionary(worker => worker.WorkerId, _ => NotSignedOff);
        if (unsigned.Count > 0) return NotReady(party, unsigned);

        // Gate 2: run-once — a worker-month already written stays written unless its bill is gone.
        var runOnce = await AlreadyCodedAsync(party, cancellationToken);
        if (runOnce.HoldsEveryWorkerOf(party)) return runOnce.Skips;

        // Gate 3: the mapping must answer for every line — a gap skips, it never guesses.
        var (codedLines, gaps) = ScheduleAsXeroLines(party);
        if (gaps.Count > 0) return NotReady(party, gaps);

        // Both routes need a settlement counterparty: covers key on it, and a recognised bill is
        // marked as settlement against it.
        if (party.CounterpartyId is null || string.IsNullOrWhiteSpace(party.CounterpartyName))
            return party.Each(worker => worker.Skip(NoSettlementIdentity));

        var (billId, wasCovered, tooMany) = await FindPartysBillAsync(party, cancellationToken);
        if (tooMany is not null) return party.Each(worker => worker.Skip(tooMany));
        if (StandingBillMismatch(party, runOnce.StandingBills, billId) is { } mismatch) return party.Each(worker => worker.Skip(mismatch));
        WriteAgain(party, runOnce);
        return billId is not null
            ? await RecodeExistingBillAsync(party, billId, wasCovered, codedLines, cancellationToken)
            : await StageDraftAsync(party, codedLines, cancellationToken);
    }

    /// <summary>A company bill is coded whole: a worker whose month is not ready holds every
    /// worker on the bill, and each outcome says who is waiting for whom.</summary>
    private static IReadOnlyList<XeroCodingRunResult> NotReady(CodingParty party, IReadOnlyDictionary<string, string> reasonsByWorkerId) =>
        party.Each(worker => worker.Skip(
            reasonsByWorkerId.TryGetValue(worker.WorkerId, out var reason) ? reason : WaitingFor(party, reasonsByWorkerId)));

    private static string WaitingFor(CodingParty party, IReadOnlyDictionary<string, string> reasonsByWorkerId)
    {
        var waitingOn = party.Workers.Where(worker => reasonsByWorkerId.ContainsKey(worker.WorkerId)).ToList();
        return $"Waiting: {party.CounterpartyName}'s bill for {party.MonthStart:MMM yyyy} covers {party.NamesOf(waitingOn)} too, "
            + "and their month is not ready — "
            + string.Join("; ", waitingOn.Select(worker => $"{worker.WorkerName}: {reasonsByWorkerId[worker.WorkerId]}"))
            + " A company bill is coded once every worker on it is ready.";
    }
}

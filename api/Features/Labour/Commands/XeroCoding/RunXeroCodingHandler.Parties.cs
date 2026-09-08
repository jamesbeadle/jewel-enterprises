namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed partial class RunXeroCodingHandler
{
    /// <summary>
    /// The workers who settle through one counterparty this month, coded as one (J, 2026-09-08):
    /// a company that bills several workers on one invoice has that invoice recoded to every
    /// worker's lines at once, so its workers are one party and share one bill search, one
    /// recode and one draft. A sole trader is a party of one. A worker with no settlement
    /// identity stands alone and skips at the identity gate.
    /// </summary>
    private sealed record CodingParty(string? CounterpartyId, string CounterpartyName, IReadOnlyList<WorkerRun> Workers)
    {
        public bool IsCompanyBill => Workers.Count > 1;
        public CodingMonth Month => Workers[0].Month;
        public DateTimeOffset MonthStart => Month.Start;
        public bool DryRun => Workers[0].DryRun;

        /// <summary>Whose bill the run looks for — the worker's own, or the company's.</summary>
        public string Owner => IsCompanyBill ? CounterpartyName : Workers[0].WorkerName;

        /// <summary>The names a bill's contact may carry: a worker bills under their own name or
        /// their company's; a company bill covering several workers is the company's alone.</summary>
        public IReadOnlyList<string> ContactNames => IsCompanyBill
            ? new List<string> { CounterpartyName }
            : new[] { Workers[0].WorkerName, CounterpartyName }.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToList();

        public decimal ScheduleTotal => Workers.Sum(worker => worker.Schedule.GrossTotal);

        public string WorkerNames => string.Join(", ", Workers.Select(worker => worker.WorkerName));

        public string NamesOf(IEnumerable<WorkerRun> some) => string.Join(", ", some.Select(worker => worker.WorkerName));

        public WorkerRun Worker(string workerId) => Workers.Single(worker => worker.WorkerId == workerId);

        public IReadOnlyList<XeroCodingRunResult> Each(Func<WorkerRun, XeroCodingRunResult> outcome) => Workers.Select(outcome).ToList();
    }

    /// <summary>Every worker with a schedule this month, grouped by settlement counterparty in the
    /// order the schedule lists them — a worker without one is their own party.</summary>
    private static IEnumerable<CodingParty> SettlementParties(
        SettlementScheduleSnapshot snapshot, CodingMonth month, string runByEmail, bool dryRun) =>
        snapshot.Workers
            .Where(schedule => schedule.Verdict != ScheduleVerdict.Nothing)
            .Select(schedule => new WorkerRun(
                schedule, month, month.LatestRuns.TryGetValue(schedule.WorkerId, out var latest) ? latest : null, runByEmail, dryRun))
            .GroupBy(run => run.Schedule.SubcontractorId ?? run.WorkerId)
            .Select(group => new CodingParty(group.First().Schedule.SubcontractorId, group.First().Schedule.SubcontractorName, group.ToList()));
}

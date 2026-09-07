using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

/// <summary>
/// The §6a coding run: one worker-month at a time, through the gates in <c>.Gates</c>, to either
/// a recode of the worker's own bill (<c>.Recode</c>, re-pointing the ledger and cover in
/// <c>.Repoint</c>) or a staged draft (<c>.Draft</c>). The month's reads live in
/// <c>.MonthReads</c>, bill recognition in <c>.Recognition</c>, the words in <c>.Wording</c>.
/// </summary>
public sealed partial class RunXeroCodingHandler : ICommandHandler<RunXeroCoding, IReadOnlyList<XeroCodingRunResult>>
{
    private readonly JpmsContext context;
    private readonly SettlementScheduleBuilder builder;
    private readonly IXeroClient xero;
    private readonly XeroOptions xeroOptions;

    public RunXeroCodingHandler(JpmsContext context, SettlementScheduleBuilder builder, IXeroClient xero, XeroOptions xeroOptions)
    { this.context = context; this.builder = builder; this.xero = xero; this.xeroOptions = xeroOptions; }

    public Task<IReadOnlyList<XeroCodingRunResult>> HandleAsync(RunXeroCoding command, CancellationToken cancellationToken) =>
        HandleAsync(command, runByEmail: "", cancellationToken);

    public async Task<IReadOnlyList<XeroCodingRunResult>> HandleAsync(RunXeroCoding command, string runByEmail, CancellationToken cancellationToken)
    {
        var snapshot = await builder.BuildAsync(command.Year, command.Month, cancellationToken);
        var month = await ReadMonthAsync(command.Year, command.Month, cancellationToken);
        var wanted = command.WorkerIds is { Count: > 0 } ? command.WorkerIds.ToHashSet() : null;

        var results = new List<XeroCodingRunResult>();
        foreach (var schedule in snapshot.Workers)
        {
            if (wanted is not null && !wanted.Contains(schedule.WorkerId)) continue;
            if (schedule.Verdict == ScheduleVerdict.Nothing) continue;

            var latest = month.LatestRuns.TryGetValue(schedule.WorkerId, out var run) ? run : null;
            var result = await CodeWorkerMonthAsync(new WorkerRun(schedule, month, latest, runByEmail, command.DryRun), cancellationToken);
            results.Add(result);
            if (!command.DryRun) context.XeroCodingRuns.Add(RunRecord(result, month.Start, runByEmail));
        }
        // A dry run has changed nothing tracked; saving is harmless but pointless.
        if (!command.DryRun) await context.SaveChangesAsync(cancellationToken);
        return results;
    }

    private static XeroCodingRunEntity RunRecord(XeroCodingRunResult result, DateTimeOffset monthStart, string runByEmail) => new()
    {
        XeroCodingRunId = LabourIdentifierFactory.NextXeroCodingRunId(),
        WorkerId = result.WorkerId,
        Month = monthStart,
        Outcome = (int)result.Outcome,
        XeroBillId = result.XeroBillId,
        Detail = XeroCodingWording.Truncate(result.Detail, 2000)!,
        RunByEmail = runByEmail,
        RunAt = DateTimeOffset.UtcNow,
    };

    /// <summary>One worker's pass through the run: their schedule, the month's shared reads,
    /// the latest outcome recorded for them, and who is running.</summary>
    private sealed record WorkerRun(
        WorkerSettlementSchedule Schedule,
        CodingMonth Month,
        XeroCodingRunEntity? LatestRun,
        string RunByEmail,
        bool DryRun)
    {
        public DateTimeOffset MonthStart => Month.Start;
        public DateTimeOffset MonthEnd => Month.End;

        public XeroCodingRunResult Outcome(XeroCodingOutcome outcome, string detail, string billId = "") =>
            new(Schedule.WorkerId, Schedule.WorkerName, outcome, detail, billId);

        public XeroCodingRunResult Skip(string why, string billId = "") => Outcome(XeroCodingOutcome.Skipped, why, billId);

        public XeroCodingRunResult Failed(string why, string billId = "") => Outcome(XeroCodingOutcome.Failed, why, billId);
    }
}

using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

/// <summary>
/// The §6a coding run: one settlement party at a time — a sole trader alone, or every worker a
/// company bills on one invoice (<c>.Parties</c>) — through the gates in <c>.Gates</c> and
/// <c>.RunOnce</c>, to either a recode of the party's own bill (<c>.Recode</c>, re-pointing the
/// ledger and cover in <c>.Repoint</c>) or a staged draft (<c>.Draft</c>). The month's reads live
/// in <c>.MonthReads</c>, bill recognition in <c>.FindBill</c> / <c>.Recognition</c>, a voided
/// bill's re-issue in <c>.Reissue</c>, the words in <c>.Sentences</c> and <c>XeroCodingWording</c>.
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
        foreach (var party in SettlementParties(snapshot, month, runByEmail, command.DryRun))
        {
            if (wanted is not null && !party.Workers.Any(worker => wanted.Contains(worker.WorkerId))) continue;

            var outcomes = await CodePartyMonthAsync(party, cancellationToken);
            results.AddRange(outcomes);
            if (!command.DryRun) context.XeroCodingRuns.AddRange(outcomes.Select(outcome => RunRecord(outcome, month.Start, runByEmail)));
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
        /// <summary>What every outcome for this worker starts with — why a month already written
        /// is being written again, or that the bill the ledger named has been re-issued. Empty
        /// until a gate sets it.</summary>
        public string Preface { get; set; } = "";
        public string WorkerId => Schedule.WorkerId;
        public string WorkerName => Schedule.WorkerName;
        public DateTimeOffset MonthStart => Month.Start;
        public DateTimeOffset MonthEnd => Month.End;

        public XeroCodingRunResult Outcome(XeroCodingOutcome outcome, string detail, string billId = "") =>
            new(Schedule.WorkerId, Schedule.WorkerName, outcome, Preface + detail, billId);

        public XeroCodingRunResult Skip(string why, string billId = "") => Outcome(XeroCodingOutcome.Skipped, why, billId);

        public XeroCodingRunResult Failed(string why, string billId = "") => Outcome(XeroCodingOutcome.Failed, why, billId);
    }
}

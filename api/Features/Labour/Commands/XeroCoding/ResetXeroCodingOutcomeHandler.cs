using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed class ResetXeroCodingOutcomeHandler : ICommandHandler<ResetXeroCodingOutcome, Acknowledgement>
{
    public const string ReasonRequired = "Say why the coding outcome is being reset — the reason is recorded against the worker-month.";

    private readonly JpmsContext context;
    public ResetXeroCodingOutcomeHandler(JpmsContext context) { this.context = context; }

    public Task<Acknowledgement> HandleAsync(ResetXeroCodingOutcome command, CancellationToken cancellationToken) =>
        HandleAsync(command, resetByEmail: "", cancellationToken);

    public async Task<Acknowledgement> HandleAsync(ResetXeroCodingOutcome command, string resetByEmail, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Reason)) throw new InvalidOperationException(ReasonRequired);

        var monthStart = new DateTimeOffset(new DateTime(command.Year, command.Month, 1), TimeSpan.Zero);
        var worker = await context.Workers.AsNoTracking()
            .FirstOrDefaultAsync(row => row.WorkerId == command.WorkerId, cancellationToken)
            ?? throw new InvalidOperationException("No such worker.");
        var latest = await LatestOutcomeAsync(command.WorkerId, monthStart, cancellationToken);
        if (latest is null)
            throw new InvalidOperationException(
                $"{worker.Name}'s {monthStart:MMM yyyy} has no coding outcome to reset — the run has never written it, so it will run as it is.");
        var previous = (XeroCodingOutcome)latest.Outcome;
        if (previous is not (XeroCodingOutcome.BillRecoded or XeroCodingOutcome.DraftStaged))
            throw new InvalidOperationException(
                $"{worker.Name}'s {monthStart:MMM yyyy} reads {previous} ({latest.RunAt:dd MMM HH:mm}) — that does not block the run, so there is nothing to reset.");

        var entity = ResetRecord(command, latest, previous, monthStart, resetByEmail);
        context.XeroCodingRuns.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(entity.XeroCodingRunId);
    }

    private Task<XeroCodingRunEntity?> LatestOutcomeAsync(string workerId, DateTimeOffset monthStart, CancellationToken cancellationToken) =>
        context.XeroCodingRuns.AsNoTracking()
            .Where(run => run.WorkerId == workerId && run.Month == monthStart)
            .OrderByDescending(run => run.RunAt)
            .FirstOrDefaultAsync(cancellationToken);

    private static XeroCodingRunEntity ResetRecord(
        ResetXeroCodingOutcome command, XeroCodingRunEntity latest, XeroCodingOutcome previous, DateTimeOffset monthStart, string resetByEmail)
    {
        var detail = $"Reset by {(string.IsNullOrWhiteSpace(resetByEmail) ? "the portal" : resetByEmail)}: {command.Reason.Trim()} "
            + $"(was {previous} at {latest.RunAt:dd MMM yyyy HH:mm}"
            + (string.IsNullOrEmpty(latest.XeroBillId) ? ")" : $", bill {latest.XeroBillId})")
            + " — the next run takes this month again.";
        return new XeroCodingRunEntity
        {
            XeroCodingRunId = LabourIdentifierFactory.NextXeroCodingRunId(),
            WorkerId = command.WorkerId,
            Month = monthStart,
            Outcome = (int)XeroCodingOutcome.Reset,
            XeroBillId = latest.XeroBillId,
            Detail = XeroCodingWording.Truncate(detail, 2000)!,
            RunByEmail = resetByEmail,
            RunAt = DateTimeOffset.UtcNow,
        };
    }
}

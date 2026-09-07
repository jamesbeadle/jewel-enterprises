using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// The connector's correction leg (2026-09-07, the accountant's ask): unapprove_worker_day and
// move_worker_day are by-name wrappers over UnapproveTimesheetHandler / MoveTimesheetHandler —
// the same handlers the Labour tab's row actions post to — resolving worker name + date the way
// reject_worker_day does (WorkerWeekTimesheets), so the two surfaces share one gate, one set of
// downstream guards and one audit trail. Same MD/FD/Admin set as the endpoints, never wider.

// ---- unapprove_worker_day ---------------------------------------------------------------------

public sealed class UnapproveWorkerDayByNameAuthorisation
{
    public bool Allows(SignedInUser user, UnapproveWorkerDayByName command) =>
        LabourRoleSets.CorrectApprovedTime.IncludesAny(user.Roles);
}

public sealed class UnapproveWorkerDayByNameValidation
{
    public ValidationOutcome Check(UnapproveWorkerDayByName command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("projectId is required — it comes from list_projects.");
        if (string.IsNullOrWhiteSpace(command.WorkerName)) errors.Add("Worker name is required.");
        if (string.IsNullOrWhiteSpace(command.Reason))
            errors.Add("A reason is required — it is written to the audit trail with the approval being reversed.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class UnapproveWorkerDayByNameHandler : ICommandHandler<UnapproveWorkerDayByName, TimesheetDetail>
{
    private readonly JpmsContext context;
    private readonly UnapproveTimesheetHandler unapprove;
    public UnapproveWorkerDayByNameHandler(JpmsContext context, UnapproveTimesheetHandler unapprove)
    { this.context = context; this.unapprove = unapprove; }

    public async Task<TimesheetDetail> HandleAsync(UnapproveWorkerDayByName command, CancellationToken cancellationToken)
    {
        var (worker, dayTimesheets, date) = await WorkerDayTimesheets.FindAsync(
            context, command.ProjectId, command.WorkerName, command.Date, "reversing their approval", cancellationToken);
        var approved = dayTimesheets.Where(timesheet => timesheet.Status == (int)TimesheetStatus.Approved).ToList();
        if (approved.Count == 0)
            throw new InvalidOperationException(
                $"{worker.Name}'s {date:ddd dd MMM} on this project isn't approved — there is no approval to reverse "
                + "(view_labour_week shows the day's status).");

        // A date normally carries one row; a manual duplicate is reversed with it.
        TimesheetDetail last = null!;
        foreach (var timesheet in approved)
            last = await unapprove.HandleAsync(new UnapproveTimesheet(timesheet.TimesheetId, command.Reason),
                command.ReversedByEmail, cancellationToken);
        return last;
    }
}

// ---- move_worker_day --------------------------------------------------------------------------

public sealed class MoveWorkerDayByNameAuthorisation
{
    public bool Allows(SignedInUser user, MoveWorkerDayByName command) =>
        LabourRoleSets.CorrectApprovedTime.IncludesAny(user.Roles);
}

public sealed class MoveWorkerDayByNameValidation
{
    public ValidationOutcome Check(MoveWorkerDayByName command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("projectId is required — it comes from list_projects.");
        if (string.IsNullOrWhiteSpace(command.ToProjectId)) errors.Add("toProjectId is required — it comes from list_projects.");
        if (string.IsNullOrWhiteSpace(command.WorkerName)) errors.Add("Worker name is required.");
        if (string.IsNullOrWhiteSpace(command.Reason))
            errors.Add("A reason is required — it is written to the audit trail on both projects.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class MoveWorkerDayByNameHandler : ICommandHandler<MoveWorkerDayByName, TimesheetMoveResult>
{
    private readonly JpmsContext context;
    private readonly MoveTimesheetHandler move;
    public MoveWorkerDayByNameHandler(JpmsContext context, MoveTimesheetHandler move)
    { this.context = context; this.move = move; }

    public async Task<TimesheetMoveResult> HandleAsync(MoveWorkerDayByName command, CancellationToken cancellationToken)
    {
        var (_, dayTimesheets, _) = await WorkerDayTimesheets.FindAsync(
            context, command.ProjectId, command.WorkerName, command.Date, "moving their timesheet", cancellationToken);

        // A date normally carries one row; a manual duplicate moves with it. A budget refusal
        // on the first row stops the move before anything has changed.
        TimesheetMoveResult last = null!;
        foreach (var timesheet in dayTimesheets)
        {
            last = await move.HandleAsync(
                new MoveTimesheet(timesheet.TimesheetId, command.ToProjectId, command.Reason, command.AllowOverBudget),
                command.MovedByEmail, cancellationToken);
            if (!last.Moved) break;
        }
        return last;
    }
}

// ---- Shared resolution ------------------------------------------------------------------------

internal static class WorkerDayTimesheets
{
    /// <summary>The named worker's timesheets on one project on one date (normalised to the
    /// work date), plus the resolved worker. Throws the model-facing guidance when the project,
    /// the worker or the day is not there.</summary>
    public static async Task<(WorkerEntity Worker, List<TimesheetEntity> Timesheets, DateTimeOffset Date)> FindAsync(
        JpmsContext context, string projectId, string workerName, DateTimeOffset date, string activityPhrase,
        CancellationToken cancellationToken)
    {
        var day = SiteClock.WorkDateOf(date);
        var (worker, weekTimesheets, _) = await WorkerWeekTimesheets.FindAsync(
            context, projectId, workerName, day, activityPhrase, cancellationToken);
        var dayTimesheets = weekTimesheets
            .Where(timesheet => SiteClock.WorkDateOf(timesheet.WorkedOn) == day)
            .ToList();
        if (dayTimesheets.Count == 0)
            throw new InvalidOperationException(
                $"{worker.Name} has no timesheet on {day:ddd dd MMM} on this project — view_labour_week shows what the week holds.");
        return (worker, dayTimesheets, day);
    }
}

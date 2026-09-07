using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// The connector's coding/approval leg (2026-08-28, the accountant's ask — "if he can approve in
// the portal he can approve in Claude"): CodeWorkerWeekByName, ApproveWorkerWeekByName and
// RejectWorkerDayByName are by-name wrappers over the Labour grid's OWN handlers
// (AdjustTimesheetHandler, ApproveTimesheetsHandler, RejectTimesheetHandler), so the two
// surfaces cannot drift: same LabourRoleSets.ApproveTimesheets gate as every grid endpoint, same
// uncoded refusal, same per-cost-code budget hard-block, same approved-rows-are-immutable rule.
// There are no HTTP endpoints — the portal grid holds timesheet ids and posts the id-keyed
// commands; an AI caller meets workers as names and days as dates, so these resolve name → the
// register (WorkerNameResolver, shared with week entry) and dates → that week's timesheets.

// ---- Shared resolution ------------------------------------------------------------------------

internal static class WorkerWeekTimesheets
{
    /// <summary>The named worker's timesheets on one project in the week from
    /// <paramref name="weekStart"/> — plus the resolved worker and the normalised Monday.
    /// Throws the model-facing guidance (unknown project, unknown/ambiguous worker) the
    /// executor returns as answers.</summary>
    public static async Task<(WorkerEntity Worker, List<TimesheetEntity> Timesheets, DateTimeOffset WeekStart)>
        FindAsync(JpmsContext context, string projectId, string workerName, DateTimeOffset weekStart,
            string activityPhrase, CancellationToken cancellationToken)
    {
        if (!await context.Projects.AsNoTracking().AnyAsync(project => project.ProjectId == projectId, cancellationToken))
            throw new InvalidOperationException(
                $"No project with id \"{projectId}\" — the id comes from list_projects.");

        var workers = await context.Workers.AsNoTracking().ToListAsync(cancellationToken);
        var worker = WorkerNameResolver.Resolve(workers, workerName, activityPhrase);

        var start = SiteClock.WorkDateOf(weekStart);
        var end = start.AddDays(7);
        var timesheets = await context.Timesheets.AsNoTracking()
            .Where(timesheet => timesheet.ProjectId == projectId
                                && timesheet.WorkerId == worker.WorkerId
                                && timesheet.WorkedOn >= start && timesheet.WorkedOn < end)
            .OrderBy(timesheet => timesheet.WorkedOn)
            .ToListAsync(cancellationToken);
        return (worker, timesheets, start);
    }

    /// <summary>Narrows the week to the requested dates (normalised, de-duplicated); null means
    /// the whole week. The second list is requested dates that carry no timesheet at all.</summary>
    public static (List<TimesheetEntity> InScope, List<DateTimeOffset> MissingDates) Narrow(
        List<TimesheetEntity> timesheets, IReadOnlyList<DateTimeOffset>? dates)
    {
        if (dates is not { Count: > 0 }) return (timesheets, new List<DateTimeOffset>());
        var wanted = dates.Select(SiteClock.WorkDateOf).Distinct().OrderBy(date => date).ToList();
        var inScope = timesheets
            .Where(timesheet => wanted.Contains(SiteClock.WorkDateOf(timesheet.WorkedOn)))
            .ToList();
        var covered = inScope.Select(timesheet => SiteClock.WorkDateOf(timesheet.WorkedOn)).ToHashSet();
        return (inScope, wanted.Where(date => !covered.Contains(date)).ToList());
    }

    public static string NotSubmittedDetail(TimesheetEntity timesheet) =>
        timesheet.Status == (int)TimesheetStatus.Approved
            ? "already approved — cost has posted; only the MD/FD can reverse it (unapprove_worker_day, with a reason) or move it to another project (move_worker_day)"
            : "rejected — the worker must resubmit before it can be acted on";

    /// <summary>In-week check shared by the validations, so a date outside the stated week is
    /// refused up front rather than silently matching nothing.</summary>
    public static void CheckDatesInWeek(List<string> errors, DateTimeOffset weekStart, IReadOnlyList<DateTimeOffset>? dates)
    {
        if (dates is not { Count: > 0 }) return;
        var start = SiteClock.WorkDateOf(weekStart);
        foreach (var date in dates.Select(SiteClock.WorkDateOf).Distinct())
        {
            if (date < start || date >= start.AddDays(7))
                errors.Add($"{date:ddd dd MMM} is not in the week commencing {start:dd MMM}.");
        }
    }
}

// ---- code_worker_week -------------------------------------------------------------------------

public sealed class CodeWorkerWeekByNameAuthorisation
{
    // Same gate as AdjustTimesheetAuthorisation — coding IS adjusting, in bulk.
    public bool Allows(SignedInUser user, CodeWorkerWeekByName command) =>
        LabourRoleSets.ApproveTimesheets.IncludesAny(user.Roles);
}

public sealed class CodeWorkerWeekByNameValidation
{
    public ValidationOutcome Check(CodeWorkerWeekByName command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId))
            errors.Add("projectId is required — it comes from list_projects.");
        if (string.IsNullOrWhiteSpace(command.WorkerName))
            errors.Add("Worker name is required.");
        if (string.IsNullOrWhiteSpace(command.CostCode))
            errors.Add("A cost code is required — pick one from list_cost_codes.");
        WorkerWeekTimesheets.CheckDatesInWeek(errors, command.WeekStart, command.Dates);
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class CodeWorkerWeekByNameHandler : ICommandHandler<CodeWorkerWeekByName, WorkerWeekCodingResult>
{
    private readonly JpmsContext context;
    private readonly ICommandHandler<AdjustTimesheet, TimesheetDetail> adjust;
    public CodeWorkerWeekByNameHandler(JpmsContext context, ICommandHandler<AdjustTimesheet, TimesheetDetail> adjust)
    { this.context = context; this.adjust = adjust; }

    public async Task<WorkerWeekCodingResult> HandleAsync(CodeWorkerWeekByName command, CancellationToken cancellationToken)
    {
        // The portal's picker constrains the code; the connector has no picker, so the master is
        // checked here — a free-typed code would send real money to the wrong place unnoticed.
        var code = await context.CostCenters.AsNoTracking()
            .FirstOrDefaultAsync(centre => centre.Code == command.CostCode, cancellationToken);
        if (code is null || !code.IsActive)
            throw new InvalidOperationException(
                $"\"{command.CostCode}\" is not an active cost code — call list_cost_codes and use a "
                + "Code exactly as it comes back.");

        var (worker, timesheets, weekStart) = await WorkerWeekTimesheets.FindAsync(
            context, command.ProjectId, command.WorkerName, command.WeekStart,
            "coding their timesheets", cancellationToken);
        var (inScope, missingDates) = WorkerWeekTimesheets.Narrow(timesheets, command.Dates);

        if (inScope.Count == 0 && missingDates.Count == 0)
            throw new InvalidOperationException(
                $"{worker.Name} has no timesheets on this project in the week commencing "
                + $"{weekStart:dd MMM} — view_labour_week shows what the week holds.");

        var outcomes = new List<WorkerDayCodingOutcome>();
        foreach (var timesheet in inScope)
        {
            if (timesheet.Status != (int)TimesheetStatus.Submitted)
            {
                outcomes.Add(new WorkerDayCodingOutcome(timesheet.WorkedOn, false,
                    WorkerWeekTimesheets.NotSubmittedDetail(timesheet)));
                continue;
            }
            try
            {
                // The grid's own Adjust, row by row: hours pass through unchanged.
                await adjust.HandleAsync(
                    new AdjustTimesheet(timesheet.TimesheetId, timesheet.Hours, command.CostCode),
                    cancellationToken);
                outcomes.Add(new WorkerDayCodingOutcome(timesheet.WorkedOn, true,
                    $"coded to {command.CostCode} ({timesheet.Hours}h)"));
            }
            catch (InvalidOperationException refusal)
            {
                outcomes.Add(new WorkerDayCodingOutcome(timesheet.WorkedOn, false, refusal.Message));
            }
        }
        outcomes.AddRange(missingDates.Select(date =>
            new WorkerDayCodingOutcome(date, false, "no timesheet on this day")));

        return new WorkerWeekCodingResult(worker.WorkerId, worker.Name, weekStart, command.CostCode,
            outcomes.OrderBy(outcome => outcome.Date).ToList());
    }
}

// ---- approve_worker_week ----------------------------------------------------------------------

public sealed class ApproveWorkerWeekByNameAuthorisation
{
    // Same gates as ApproveTimesheetsEndpoint's inline checks: the approval roles for the plain
    // path, and — when the command asks to approve PAST the budget hard-block — the narrower
    // MD/FD/Admin override set on top, so the connector cannot hand the override to anyone the
    // Labour tab would refuse.
    public bool Allows(SignedInUser user, ApproveWorkerWeekByName command) =>
        LabourRoleSets.ApproveTimesheets.IncludesAny(user.Roles)
        && (!command.AllowOverBudget || LabourRoleSets.OverrideBudgetBlock.IncludesAny(user.Roles));
}

public sealed class ApproveWorkerWeekByNameValidation
{
    public ValidationOutcome Check(ApproveWorkerWeekByName command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId))
            errors.Add("projectId is required — it comes from list_projects.");
        if (string.IsNullOrWhiteSpace(command.WorkerName))
            errors.Add("Worker name is required.");
        if (command.AllowOverBudget && string.IsNullOrWhiteSpace(command.OverBudgetReason))
            errors.Add("An over-budget approval needs a reason — it is written to the audit trail.");
        WorkerWeekTimesheets.CheckDatesInWeek(errors, command.WeekStart, command.Dates);
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class ApproveWorkerWeekByNameHandler : ICommandHandler<ApproveWorkerWeekByName, WorkerWeekApprovalResult>
{
    private readonly JpmsContext context;
    private readonly ApproveTimesheetsHandler approver;
    public ApproveWorkerWeekByNameHandler(JpmsContext context, ApproveTimesheetsHandler approver)
    { this.context = context; this.approver = approver; }

    public async Task<WorkerWeekApprovalResult> HandleAsync(ApproveWorkerWeekByName command, CancellationToken cancellationToken)
    {
        var (worker, timesheets, weekStart) = await WorkerWeekTimesheets.FindAsync(
            context, command.ProjectId, command.WorkerName, command.WeekStart,
            "approving their timesheets", cancellationToken);
        var (inScope, missingDates) = WorkerWeekTimesheets.Narrow(timesheets, command.Dates);

        var submitted = inScope.Where(timesheet => timesheet.Status == (int)TimesheetStatus.Submitted).ToList();
        if (submitted.Count == 0)
        {
            var approvedCount = inScope.Count(timesheet => timesheet.Status == (int)TimesheetStatus.Approved);
            var rejectedCount = inScope.Count(timesheet => timesheet.Status == (int)TimesheetStatus.Rejected);
            throw new InvalidOperationException(
                $"Nothing to approve for {worker.Name} in the week commencing {weekStart:dd MMM} on this "
                + $"project — {approvedCount} already approved, {rejectedCount} rejected, "
                + $"{missingDates.Count} of the requested days without a timesheet. view_labour_week "
                + "shows the week as the Labour tab sees it.");
        }

        // The grid's own batch approval: rate resolution, cost snapshot, uncoded refusal and the
        // per-cost-code budget hard-block all live in the ONE handler both surfaces share.
        var result = await approver.HandleAsync(
            new ApproveTimesheets(command.ProjectId, submitted.Select(timesheet => timesheet.TimesheetId).ToList(),
                command.AllowOverBudget, command.OverBudgetReason),
            command.ApprovedByEmail, cancellationToken);

        var byId = submitted.ToDictionary(timesheet => timesheet.TimesheetId);
        var outcomes = new List<WorkerDayApprovalOutcome>();
        outcomes.AddRange(result.Approved.Select(detail => new WorkerDayApprovalOutcome(
            detail.WorkedOn, true, detail.Hours, detail.CostCode, "approved — cost posted")));
        outcomes.AddRange(result.Failures.Select(failure =>
        {
            var timesheet = byId.GetValueOrDefault(failure.TimesheetId);
            return new WorkerDayApprovalOutcome(
                timesheet?.WorkedOn ?? weekStart, false, timesheet?.Hours ?? 0m,
                timesheet?.CostCode ?? "", failure.Reason);
        }));
        outcomes.AddRange(inScope.Where(timesheet => timesheet.Status != (int)TimesheetStatus.Submitted)
            .Select(timesheet => new WorkerDayApprovalOutcome(
                timesheet.WorkedOn, false, timesheet.Hours, timesheet.CostCode,
                WorkerWeekTimesheets.NotSubmittedDetail(timesheet))));
        outcomes.AddRange(missingDates.Select(date =>
            new WorkerDayApprovalOutcome(date, false, 0m, "", "no timesheet on this day")));

        return new WorkerWeekApprovalResult(worker.WorkerId, worker.Name, weekStart,
            outcomes.OrderBy(outcome => outcome.Date).ToList());
    }
}

// ---- reject_worker_day ------------------------------------------------------------------------

public sealed class RejectWorkerDayByNameAuthorisation
{
    // Same gate as RejectTimesheetEndpoint's inline check.
    public bool Allows(SignedInUser user, RejectWorkerDayByName command) =>
        LabourRoleSets.ApproveTimesheets.IncludesAny(user.Roles);
}

public sealed class RejectWorkerDayByNameValidation
{
    public ValidationOutcome Check(RejectWorkerDayByName command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId))
            errors.Add("projectId is required — it comes from list_projects.");
        if (string.IsNullOrWhiteSpace(command.WorkerName))
            errors.Add("Worker name is required.");
        if (string.IsNullOrWhiteSpace(command.Reason))
            errors.Add("A rejection reason is required — the worker reads it to know what to fix.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class RejectWorkerDayByNameHandler : ICommandHandler<RejectWorkerDayByName, TimesheetDetail>
{
    private readonly JpmsContext context;
    private readonly ICommandHandler<RejectTimesheet, TimesheetDetail> reject;
    public RejectWorkerDayByNameHandler(JpmsContext context, ICommandHandler<RejectTimesheet, TimesheetDetail> reject)
    { this.context = context; this.reject = reject; }

    public async Task<TimesheetDetail> HandleAsync(RejectWorkerDayByName command, CancellationToken cancellationToken)
    {
        var date = SiteClock.WorkDateOf(command.Date);
        var (worker, weekTimesheets, _) = await WorkerWeekTimesheets.FindAsync(
            context, command.ProjectId, command.WorkerName, date,
            "rejecting their timesheet", cancellationToken);
        var dayTimesheets = weekTimesheets
            .Where(timesheet => SiteClock.WorkDateOf(timesheet.WorkedOn) == date)
            .ToList();

        if (dayTimesheets.Count == 0)
            throw new InvalidOperationException(
                $"{worker.Name} has no timesheet on {date:ddd dd MMM} on this project — "
                + "view_labour_week shows what the week holds.");

        var submitted = dayTimesheets.Where(timesheet => timesheet.Status == (int)TimesheetStatus.Submitted).ToList();
        if (submitted.Count == 0)
            throw new InvalidOperationException(
                $"{worker.Name}'s {date:ddd dd MMM} is "
                + WorkerWeekTimesheets.NotSubmittedDetail(dayTimesheets[0]) + ".");

        // A date normally carries one row; a manual duplicate gets rejected with it, exactly as a
        // person would tick both on the grid.
        TimesheetDetail last = null!;
        foreach (var timesheet in submitted)
            last = await reject.HandleAsync(new RejectTimesheet(timesheet.TimesheetId, command.Reason), cancellationToken);
        return last;
    }
}

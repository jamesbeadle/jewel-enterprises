using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// Move (2026-09-07, the accountant's ask): a timesheet entered or approved on the wrong project
// goes to the right one as it stands — date, hours, cost code, status and, when approved, the
// rate/cost snapshot, approver and approval time. The rate is the worker's, not the project's,
// so the snapshot stays true; what changes is WHICH project's Financials carry the cost, so an
// approved day re-meets the destination's per-cost-code budget hard-block exactly as approval
// would, with the same MD/FD/Admin override and the same LabourBudgetOverridden audit row. A
// linked site-register row moves with the timesheet so the register agrees with the cost.
// MD/FD/Admin only for every status (one gate, one rule); guarded against a settled or coded
// source month like unapprove — but not against sign-off, which the day keeps satisfying.

public sealed class MoveTimesheetAuthorisation
{
    public bool Allows(SignedInUser user, MoveTimesheet command) =>
        LabourRoleSets.CorrectApprovedTime.IncludesAny(user.Roles);
}

public sealed class MoveTimesheetValidation
{
    public ValidationOutcome Check(MoveTimesheet command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.TimesheetId)) errors.Add("timesheetId is required.");
        if (string.IsNullOrWhiteSpace(command.ToProjectId)) errors.Add("The destination project is required.");
        if (string.IsNullOrWhiteSpace(command.Reason))
            errors.Add("A reason is required — moving a day changes which project carries its cost, and the reason is the audit record of why.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class MoveTimesheetEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly MoveTimesheetAuthorisation authorisation;
    private readonly MoveTimesheetValidation validation;
    private readonly MoveTimesheetHandler handler;
    public MoveTimesheetEndpoint(SignedInUserResolver users, MoveTimesheetAuthorisation authorisation,
        MoveTimesheetValidation validation, MoveTimesheetHandler handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(MoveTimesheet))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/timesheets/{timesheetId}/move")] HttpRequest request, string timesheetId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var body = await request.ReadFromJsonAsync<MoveTimesheet>();
        if (body is null) return new BadRequestResult();
        var command = body with { TimesheetId = timesheetId };
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = validation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, signedInUser.Email, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException refusal)
        {
            return new BadRequestObjectResult(new[] { refusal.Message });
        }
    }
}

public sealed class MoveTimesheetHandler : ICommandHandler<MoveTimesheet, TimesheetMoveResult>
{
    private readonly JpmsContext context;
    private readonly AuditTrail audit;
    public MoveTimesheetHandler(JpmsContext context, AuditTrail audit)
    { this.context = context; this.audit = audit; }

    public Task<TimesheetMoveResult> HandleAsync(MoveTimesheet command, CancellationToken cancellationToken) =>
        HandleAsync(command, movedByEmail: "", cancellationToken);

    public async Task<TimesheetMoveResult> HandleAsync(MoveTimesheet command, string movedByEmail, CancellationToken cancellationToken)
    {
        var timesheet = await context.Timesheets.FindAsync(new object[] { command.TimesheetId }, cancellationToken)
            ?? throw new InvalidOperationException($"Timesheet {command.TimesheetId} not found.");
        if (timesheet.ProjectId == command.ToProjectId)
            throw new InvalidOperationException("That is the project the timesheet is already on.");
        var projects = await context.Projects.AsNoTracking()
            .Where(project => project.ProjectId == timesheet.ProjectId || project.ProjectId == command.ToProjectId)
            .ToDictionaryAsync(project => project.ProjectId, cancellationToken);
        if (!projects.TryGetValue(command.ToProjectId, out var destination))
            throw new InvalidOperationException($"No project with id \"{command.ToProjectId}\" — the id comes from list_projects.");
        var source = projects.GetValueOrDefault(timesheet.ProjectId);
        var worker = timesheet.WorkerId == "" ? null
            : await context.Workers.FindAsync(new object[] { timesheet.WorkerId }, cancellationToken);
        var workerName = worker?.Name ?? timesheet.PersonEmail;

        if (worker is not null
            && await TimesheetCorrectionGuards.FirstBlockAsync(context, timesheet, worker,
                checkSignOff: false, includeWorkerMonthCovers: false, cancellationToken) is { } block)
            throw new InvalidOperationException(block);

        // An approved day carries posted cost into the destination: the same hard-block as
        // approval, against the destination's budget and its already-approved labour.
        string? overrideBlock = null;
        if (timesheet.Status == (int)TimesheetStatus.Approved)
        {
            var blockReason = await DestinationBudgetBlockAsync(timesheet, command.ToProjectId, cancellationToken);
            if (blockReason is not null && !command.AllowOverBudget)
                return new TimesheetMoveResult(false, timesheet.ToDetail(workerName), blockReason);
            overrideBlock = blockReason;
        }

        var fromProjectId = timesheet.ProjectId;
        timesheet.ProjectId = command.ToProjectId;
        if (timesheet.SiteAttendanceId != "")
        {
            var attendance = await context.SiteAttendances.FindAsync(new object[] { timesheet.SiteAttendanceId }, cancellationToken);
            if (attendance is not null) attendance.ProjectId = command.ToProjectId;
        }
        await context.SaveChangesAsync(cancellationToken);

        var status = ((TimesheetStatus)timesheet.Status).ToString();
        var money = timesheet.Status == (int)TimesheetStatus.Approved ? $", £{timesheet.CostAmount:N2} approved" : "";
        var day = $"{workerName} {timesheet.WorkedOn:ddd dd MMM yyyy} ({timesheet.Hours}h on {timesheet.CostCode}, {status}{money})";
        var reason = $"Reason given: {command.Reason.Trim()}";
        await audit.WriteAsync(AuditEventType.LabourDayMoved,
            $"{day} moved to {Describe(destination)}. {reason}",
            projectId: fromProjectId, actorEmail: movedByEmail, cancellationToken: cancellationToken);
        await audit.WriteAsync(AuditEventType.LabourDayMoved,
            $"{day} moved here from {(source is null ? fromProjectId : Describe(source))}. {reason}",
            projectId: command.ToProjectId, actorEmail: movedByEmail, cancellationToken: cancellationToken);
        if (overrideBlock is not null)
            await audit.WriteAsync(AuditEventType.LabourBudgetOverridden,
                $"{workerName} {timesheet.WorkedOn:ddd dd MMM} (£{timesheet.CostAmount:N2} against {timesheet.CostCode}) "
                + $"moved in over budget — {overrideBlock} {reason}",
                projectId: command.ToProjectId, actorEmail: movedByEmail, cancellationToken: cancellationToken);

        return new TimesheetMoveResult(true, timesheet.ToDetail(workerName), null);
    }

    private async Task<string?> DestinationBudgetBlockAsync(Data.Entities.TimesheetEntity timesheet, string toProjectId,
        CancellationToken cancellationToken)
    {
        var budget = await context.CostCodeBudgets.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ProjectId == toProjectId && row.CostCode == timesheet.CostCode, cancellationToken);
        var alreadyApproved = await context.Timesheets.AsNoTracking()
            .Where(row => row.ProjectId == toProjectId && row.Status == (int)TimesheetStatus.Approved
                          && row.CostCode == timesheet.CostCode)
            .SumAsync(row => row.CostAmount, cancellationToken);
        return LabourRules.BudgetBlockReason(timesheet.CostCode, timesheet.CostAmount,
            budget is null ? null : (budget.AllocatedAmount, budget.SpentAmount, budget.CommittedAmount),
            alreadyApproved);
    }

    private static string Describe(Data.Entities.ProjectEntity project) => $"{project.Reference} {project.Name}";
}

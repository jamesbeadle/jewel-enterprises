using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// Unapprove (2026-09-07, the accountant's ask): the deliberate reversal of a posted approval.
// Approval snapshots rate + cost onto the row and every cost read filters on Status ==
// Approved, so putting the row back to Submitted and clearing the snapshot IS the reversal —
// Financials, the settlement view, the site P&L and the labour forecast all stop counting it in
// the same save. What the row loses (rate, £, approver, when) survives on the audit row with
// the reason, which is why the reason is mandatory. MD/FD/Admin only; the downstream guards in
// TimesheetCorrectionGuards refuse once the month has gone past the point of quiet correction.

public sealed class UnapproveTimesheetAuthorisation
{
    public bool Allows(SignedInUser user, UnapproveTimesheet command) =>
        LabourRoleSets.CorrectApprovedTime.IncludesAny(user.Roles);
}

public sealed class UnapproveTimesheetValidation
{
    public ValidationOutcome Check(UnapproveTimesheet command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.TimesheetId)) errors.Add("timesheetId is required.");
        if (string.IsNullOrWhiteSpace(command.Reason))
            errors.Add("A reason is required — reversing an approval withdraws posted cost, and the reason is the audit record of why.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class UnapproveTimesheetEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UnapproveTimesheetAuthorisation authorisation;
    private readonly UnapproveTimesheetValidation validation;
    private readonly UnapproveTimesheetHandler handler;
    public UnapproveTimesheetEndpoint(SignedInUserResolver users, UnapproveTimesheetAuthorisation authorisation,
        UnapproveTimesheetValidation validation, UnapproveTimesheetHandler handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(UnapproveTimesheet))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/timesheets/{timesheetId}/unapproval")] HttpRequest request, string timesheetId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var body = await request.ReadFromJsonAsync<UnapproveTimesheet>();
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

public sealed class UnapproveTimesheetHandler : ICommandHandler<UnapproveTimesheet, TimesheetDetail>
{
    private readonly JpmsContext context;
    private readonly AuditTrail audit;
    public UnapproveTimesheetHandler(JpmsContext context, AuditTrail audit)
    { this.context = context; this.audit = audit; }

    public Task<TimesheetDetail> HandleAsync(UnapproveTimesheet command, CancellationToken cancellationToken) =>
        HandleAsync(command, reversedByEmail: "", cancellationToken);

    public async Task<TimesheetDetail> HandleAsync(UnapproveTimesheet command, string reversedByEmail, CancellationToken cancellationToken)
    {
        var timesheet = await context.Timesheets.FindAsync(new object[] { command.TimesheetId }, cancellationToken)
            ?? throw new InvalidOperationException($"Timesheet {command.TimesheetId} not found.");
        if (timesheet.Status != (int)TimesheetStatus.Approved)
            throw new InvalidOperationException($"{timesheet.WorkedOn:ddd dd MMM} isn't approved — there is no approval to reverse.");
        var worker = timesheet.WorkerId == "" ? null
            : await context.Workers.FindAsync(new object[] { timesheet.WorkerId }, cancellationToken);
        var workerName = worker?.Name ?? timesheet.PersonEmail;

        if (worker is not null
            && await TimesheetCorrectionGuards.FirstBlockAsync(context, timesheet, worker,
                checkSignOff: true, includeWorkerMonthCovers: true, cancellationToken) is { } block)
            throw new InvalidOperationException(block);

        // What the row is about to lose, kept for the audit row before it is cleared.
        var snapshot = $"{timesheet.Hours}h on {timesheet.CostCode} at £{timesheet.RateApplied:N2}/h = £{timesheet.CostAmount:N2}, "
                     + $"approved by {timesheet.ApprovedByEmail} on {timesheet.ApprovedAt?.ToString("dd MMM yyyy HH:mm")}";

        timesheet.Status = (int)TimesheetStatus.Submitted;
        timesheet.IsApproved = false;
        timesheet.RateApplied = 0m;
        timesheet.CostAmount = 0m;
        timesheet.ApprovedByEmail = "";
        timesheet.ApprovedAt = null;
        timesheet.RejectionReason = "";
        await context.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(
            AuditEventType.LabourApprovalReversed,
            $"{workerName} {timesheet.WorkedOn:ddd dd MMM yyyy} approval reversed — back to Submitted, cost withdrawn "
            + $"(was {snapshot}). Reason given: {command.Reason.Trim()}",
            projectId: timesheet.ProjectId,
            actorEmail: reversedByEmail,
            cancellationToken: cancellationToken);

        return timesheet.ToDetail(workerName);
    }
}

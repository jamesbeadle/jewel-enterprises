using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger.WorkOrderBills;

public sealed class UndoWorkOrderBillApprovalEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly Audit.AuditActor auditActor;
    private readonly UndoWorkOrderBillApprovalAuthorisation authorisation;
    private readonly UndoWorkOrderBillApprovalValidation validation;
    private readonly ICommandHandler<UndoWorkOrderBillApproval, WorkOrderBillUndoOutcome> handler;

    public UndoWorkOrderBillApprovalEndpoint(
        SignedInUserResolver users,
        Audit.AuditActor auditActor,
        UndoWorkOrderBillApprovalAuthorisation authorisation,
        UndoWorkOrderBillApprovalValidation validation,
        ICommandHandler<UndoWorkOrderBillApproval, WorkOrderBillUndoOutcome> handler)
    {
        this.users = users;
        this.auditActor = auditActor;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(UndoWorkOrderBillApproval))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "xero/work-order-bills/undo")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        auditActor.Email = signedInUser.Email;

        var command = await request.ReadFromJsonAsync<UndoWorkOrderBillApproval>();
        if (command is null) return new BadRequestResult();
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(string.Join(" ", validationOutcome.Errors));

        command = command with { UndoneBy = signedInUser.Email };
        try
        {
            var outcome = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
            return new OkObjectResult(outcome);
        }
        catch (InvalidOperationException refusal)
        {
            return new BadRequestObjectResult(refusal.Message);
        }
    }
}

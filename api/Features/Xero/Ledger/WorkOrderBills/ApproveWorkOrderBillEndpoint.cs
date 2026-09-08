using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger.WorkOrderBills;

public sealed class ApproveWorkOrderBillEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly Audit.AuditActor auditActor;
    private readonly ApproveWorkOrderBillAuthorisation authorisation;
    private readonly ApproveWorkOrderBillValidation validation;
    private readonly ICommandHandler<ApproveWorkOrderBill, WorkOrderBillApprovalOutcome> handler;

    public ApproveWorkOrderBillEndpoint(
        SignedInUserResolver users,
        Audit.AuditActor auditActor,
        ApproveWorkOrderBillAuthorisation authorisation,
        ApproveWorkOrderBillValidation validation,
        ICommandHandler<ApproveWorkOrderBill, WorkOrderBillApprovalOutcome> handler)
    {
        this.users = users;
        this.auditActor = auditActor;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(ApproveWorkOrderBill))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "xero/work-order-bills/approve")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        auditActor.Email = signedInUser.Email;

        var command = await request.ReadFromJsonAsync<ApproveWorkOrderBill>();
        if (command is null) return new BadRequestResult();
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(string.Join(" ", validationOutcome.Errors));

        // ApprovedBy is stamped server-side — never trusted from the client.
        command = command with { ApprovedBy = signedInUser.Email };
        try
        {
            var outcome = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
            return new OkObjectResult(outcome);
        }
        catch (InvalidOperationException refusal)
        {
            // Bare string so HttpCommandSender surfaces it verbatim on the card.
            return new BadRequestObjectResult(refusal.Message);
        }
    }
}

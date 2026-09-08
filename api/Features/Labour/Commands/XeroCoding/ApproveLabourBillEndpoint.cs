using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

/// <summary>The Settlement view's "Approve in Xero" (2026-09-08): the covered labour bill goes
/// DRAFT → AUTHORISED once every worker on it reads Matches. A refusal (a worker not yet
/// Matches, a bill Xero won't approve) comes back as the message, not a 500.</summary>
public sealed class ApproveLabourBillEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ApproveLabourBillHandler handler;
    public ApproveLabourBillEndpoint(SignedInUserResolver users, ApproveLabourBillHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(ApproveLabourBill))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/xero-coding/approve-bill")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageSettlement.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<ApproveLabourBill>();
        if (command is null) return new BadRequestResult();
        var validation = new ApproveLabourBillValidation().Check(command);
        if (validation.HasFailed) return new BadRequestObjectResult(validation.Errors);
        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, signedInUser.Email, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException rejection)
        {
            return new BadRequestObjectResult(new[] { rejection.Message });
        }
    }
}

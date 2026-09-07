using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

/// <summary>
/// Resets a worker-month's coding outcome (item D, 2026-09-03). The run-once gate reads the
/// LATEST recorded outcome, so a worker-month whose staged bill was deleted by hand sat behind
/// DraftStaged for ever. A reset APPENDS a Reset outcome — who, why, what it was — so the history
/// reads staged → reset → recoded and nothing is erased. Touches nothing in Xero.
/// </summary>
public sealed class ResetXeroCodingOutcomeEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ResetXeroCodingOutcomeHandler handler;
    public ResetXeroCodingOutcomeEndpoint(SignedInUserResolver users, ResetXeroCodingOutcomeHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(ResetXeroCodingOutcome))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/xero-coding/reset")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageSettlement.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<ResetXeroCodingOutcome>();
        if (command is null || string.IsNullOrWhiteSpace(command.WorkerId)) return new BadRequestResult();
        if (command.Year < 2020 || command.Year > 2100 || command.Month < 1 || command.Month > 12)
            return new BadRequestResult();
        if (string.IsNullOrWhiteSpace(command.Reason))
            return new BadRequestObjectResult(new[] { ResetXeroCodingOutcomeHandler.ReasonRequired });
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

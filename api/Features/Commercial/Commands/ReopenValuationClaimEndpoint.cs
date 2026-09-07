using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>POST /api/valuation-claims/{claimId}/reopen — undo a preapproval (Preapproved -> Draft).</summary>
public sealed class ReopenValuationClaimEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ValuationReportAuthorisation authorisation;
    private readonly ICommandHandler<ReopenValuationClaim, ValuationClaim> handler;
    public ReopenValuationClaimEndpoint(SignedInUserResolver users, ValuationReportAuthorisation authorisation, ICommandHandler<ReopenValuationClaim, ValuationClaim> handler)
    { this.users = users; this.authorisation = authorisation; this.handler = handler; }

    [Function(nameof(ReopenValuationClaim))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "valuation-claims/{claimId}/reopen")] HttpRequest request, string claimId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new ReopenValuationClaim(claimId);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            // A refused reopen (confirmed, or a live invoice against the claim) — the reason
            // reaches the claim card as a 400, never a bodiless 500.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}

using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SetBidPackageEmailDispositionEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SetBidPackageEmailDispositionAuthorisation authorisation;
    private readonly SetBidPackageEmailDispositionValidation validation;
    private readonly ICommandHandler<SetBidPackageEmailDisposition, IReadOnlyList<BidPackageEmailDisposition>> handler;

    public SetBidPackageEmailDispositionEndpoint(
        SignedInUserResolver users,
        SetBidPackageEmailDispositionAuthorisation authorisation,
        SetBidPackageEmailDispositionValidation validation,
        ICommandHandler<SetBidPackageEmailDisposition, IReadOnlyList<BidPackageEmailDisposition>> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(SetBidPackageEmailDisposition))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bid-packages/{bidPackageId}/email-dispositions")] HttpRequest request,
        string bidPackageId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<SetBidPackageEmailDisposition>();
        if (posted is null) return new BadRequestResult();
        if (posted.BidPackageId != bidPackageId) return new BadRequestObjectResult("Route bidPackageId does not match body.");
        // Who judged is stamped here, never trusted from the body.
        var command = posted with { SetByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}

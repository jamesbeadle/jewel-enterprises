using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class ReviewProgrammeDraftLineEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ProgrammeDraftAuthorisation authorisation;
    private readonly ReviewProgrammeDraftLineValidation validation;
    private readonly ICommandHandler<ReviewProgrammeDraftLine, ProgrammeDraftLine> handler;
    public ReviewProgrammeDraftLineEndpoint(SignedInUserResolver users, ProgrammeDraftAuthorisation authorisation, ReviewProgrammeDraftLineValidation validation, ICommandHandler<ReviewProgrammeDraftLine, ProgrammeDraftLine> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(ReviewProgrammeDraftLine))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "programme-draft-lines/{programmeDraftLineId}")] HttpRequest request, string programmeDraftLineId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<ReviewProgrammeDraftLine>();
        if (command is null) return new BadRequestResult();
        if (command.ProgrammeDraftLineId != programmeDraftLineId) return new BadRequestObjectResult("Route programmeDraftLineId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException reason)
        {
            return new BadRequestObjectResult(new[] { reason.Message });
        }
    }
}

using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class DiscardProgrammeDraftEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ProgrammeDraftAuthorisation authorisation;
    private readonly DiscardProgrammeDraftValidation validation;
    private readonly ICommandHandler<DiscardProgrammeDraft, ProgrammeDraft> handler;
    public DiscardProgrammeDraftEndpoint(SignedInUserResolver users, ProgrammeDraftAuthorisation authorisation, DiscardProgrammeDraftValidation validation, ICommandHandler<DiscardProgrammeDraft, ProgrammeDraft> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(DiscardProgrammeDraft))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "programme-drafts/{programmeDraftId}/discard")] HttpRequest request, string programmeDraftId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<DiscardProgrammeDraft>();
        if (command is null) return new BadRequestResult();
        if (command.ProgrammeDraftId != programmeDraftId) return new BadRequestObjectResult("Route programmeDraftId does not match body.");
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

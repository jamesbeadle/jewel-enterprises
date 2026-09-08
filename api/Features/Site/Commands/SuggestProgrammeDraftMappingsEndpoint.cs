using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class SuggestProgrammeDraftMappingsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ProgrammeDraftAuthorisation authorisation;
    private readonly ICommandHandler<SuggestProgrammeDraftMappings, ProgrammeDraftDetail> handler;
    public SuggestProgrammeDraftMappingsEndpoint(SignedInUserResolver users, ProgrammeDraftAuthorisation authorisation, ICommandHandler<SuggestProgrammeDraftMappings, ProgrammeDraftDetail> handler)
    { this.users = users; this.authorisation = authorisation; this.handler = handler; }

    [Function(nameof(SuggestProgrammeDraftMappings))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "programme-drafts/{programmeDraftId}/suggestions")] HttpRequest request, string programmeDraftId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new SuggestProgrammeDraftMappings(programmeDraftId);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
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

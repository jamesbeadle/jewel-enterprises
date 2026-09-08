using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class DraftProgrammeFromValuationEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ProgrammeDraftAuthorisation authorisation;
    private readonly DraftProgrammeFromValuationValidation validation;
    private readonly ICommandHandler<DraftProgrammeFromValuation, ProgrammeDraftDetail> handler;
    public DraftProgrammeFromValuationEndpoint(SignedInUserResolver users, ProgrammeDraftAuthorisation authorisation, DraftProgrammeFromValuationValidation validation, ICommandHandler<DraftProgrammeFromValuation, ProgrammeDraftDetail> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(DraftProgrammeFromValuation))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/programme/drafts")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<DraftProgrammeFromValuation>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");
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

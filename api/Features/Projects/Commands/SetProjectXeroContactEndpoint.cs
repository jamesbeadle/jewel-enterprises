using Jewel.JPMS.Contracts.Projects;

namespace Jewel.JPMS.Api.Features.Projects.Commands;

public sealed class SetProjectXeroContactEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SetProjectXeroContactAuthorisation authorisation;
    private readonly ICommandHandler<SetProjectXeroContact, Project> handler;

    public SetProjectXeroContactEndpoint(
        SignedInUserResolver users,
        SetProjectXeroContactAuthorisation authorisation,
        ICommandHandler<SetProjectXeroContact, Project> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.handler = handler;
    }

    [Function(nameof(SetProjectXeroContact))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "projects/{projectId}/xero-contact")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<SetProjectXeroContact>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        try
        {
            var project = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
            return new OkObjectResult(project);
        }
        catch (InvalidOperationException refusal)
        {
            return new ConflictObjectResult(refusal.Message);
        }
    }
}

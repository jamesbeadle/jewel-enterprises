using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Queries;

public sealed class GetOpenProgrammeDraftEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetOpenProgrammeDraft, ProgrammeDraftDetail?> handler;
    public GetOpenProgrammeDraftEndpoint(SignedInUserResolver users, IQueryHandler<GetOpenProgrammeDraft, ProgrammeDraftDetail?> handler) { this.users = users; this.handler = handler; }

    // Same reach as the programme itself (GetProgrammeDetailEndpoint): internal roles only.
    private static readonly RoleSet RolesThatMayReadSite = JpmsRoleSets.AllInternal;

    [Function(nameof(GetOpenProgrammeDraft))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/programme/draft")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadSite.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var detail = await handler.HandleAsync(new GetOpenProgrammeDraft(projectId), request.HttpContext.RequestAborted);
        if (detail is null) return new NoContentResult();
        return new OkObjectResult(detail);
    }
}

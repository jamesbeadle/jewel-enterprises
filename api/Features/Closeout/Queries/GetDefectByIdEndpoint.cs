using Jewel.JPMS.Contracts.Closeout;

namespace Jewel.JPMS.Api.Features.Closeout.Queries;

public sealed class GetDefectByIdEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetDefectById, Defect?> handler;
    public GetDefectByIdEndpoint(SignedInUserResolver users, IQueryHandler<GetDefectById, Defect?> handler) { this.users = users; this.handler = handler; }

    // Same gate as the register: closeout reads are internal-only.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(GetDefectById))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "defects/{defectId}")] HttpRequest request, string defectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new GetDefectById(defectId), request.HttpContext.RequestAborted));
    }
}

using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Cvr.Queries;

public sealed class ListCvrPackagesForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListCvrPackagesForProject, IReadOnlyList<CvrPackageRow>> handler;

    public ListCvrPackagesForProjectEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListCvrPackagesForProject, IReadOnlyList<CvrPackageRow>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListCvrPackagesForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/cvr-packages")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var packages = await handler.HandleAsync(new ListCvrPackagesForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(packages);
    }
}

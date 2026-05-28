using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class CaptureCvrSnapshotEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CaptureCvrSnapshotAuthorisation authorisation;
    private readonly CaptureCvrSnapshotValidation validation;
    private readonly ICommandHandler<CaptureCvrSnapshot, CvrSnapshot> handler;

    public CaptureCvrSnapshotEndpoint(
        SignedInUserResolver users,
        CaptureCvrSnapshotAuthorisation authorisation,
        CaptureCvrSnapshotValidation validation,
        ICommandHandler<CaptureCvrSnapshot, CvrSnapshot> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CaptureCvrSnapshot))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/cvr-snapshots")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<CaptureCvrSnapshot>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var snapshot = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(snapshot);
    }
}

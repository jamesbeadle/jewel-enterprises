using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cashflow;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Cashflow.Commands;

public sealed class CaptureCashflowSnapshotEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CaptureCashflowSnapshotAuthorisation authorisation;
    private readonly CaptureCashflowSnapshotValidation validation;
    private readonly ICommandHandler<CaptureCashflowSnapshot, CashflowSnapshot> handler;

    public CaptureCashflowSnapshotEndpoint(
        SignedInUserResolver users,
        CaptureCashflowSnapshotAuthorisation authorisation,
        CaptureCashflowSnapshotValidation validation,
        ICommandHandler<CaptureCashflowSnapshot, CashflowSnapshot> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CaptureCashflowSnapshot))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cashflow/snapshots")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<CaptureCashflowSnapshot>();
        if (command is null) return new BadRequestResult();

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var snapshot = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(snapshot);
    }
}

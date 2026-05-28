using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cashflow;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Cashflow.Queries;

public sealed class GetLatestCashflowSnapshotEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetLatestCashflowSnapshot, CashflowSnapshot?> handler;

    public GetLatestCashflowSnapshotEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetLatestCashflowSnapshot, CashflowSnapshot?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetLatestCashflowSnapshot))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cashflow/latest")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var snapshot = await handler.HandleAsync(new GetLatestCashflowSnapshot(), request.HttpContext.RequestAborted);
        return new OkObjectResult(snapshot);
    }
}

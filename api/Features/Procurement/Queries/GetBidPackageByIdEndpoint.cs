using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class GetBidPackageByIdEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetBidPackageById, BidPackage?> handler;

    public GetBidPackageByIdEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetBidPackageById, BidPackage?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetBidPackageById))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bid-packages/{bidPackageId}")] HttpRequest request,
        string bidPackageId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var package = await handler.HandleAsync(new GetBidPackageById(bidPackageId), request.HttpContext.RequestAborted);
        return new OkObjectResult(package);
    }
}

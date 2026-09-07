using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class ListBidPackageEmailDispositionsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListBidPackageEmailDispositions, IReadOnlyList<BidPackageEmailDisposition>> handler;

    public ListBidPackageEmailDispositionsEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListBidPackageEmailDispositions, IReadOnlyList<BidPackageEmailDisposition>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Same audience as the package's emails themselves (ListBidPackageEmailsEndpoint).
    private static readonly RoleSet RolesThatMayReadProcurement = JpmsRoleSets.AllInternal;

    [Function(nameof(ListBidPackageEmailDispositions))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bid-packages/{bidPackageId}/email-dispositions")] HttpRequest request,
        string bidPackageId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadProcurement.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListBidPackageEmailDispositions(bidPackageId), request.HttpContext.RequestAborted));
    }
}

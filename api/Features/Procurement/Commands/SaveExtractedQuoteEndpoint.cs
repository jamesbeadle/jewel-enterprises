using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SaveExtractedQuoteEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SaveExtractedQuoteAuthorisation authorisation;
    private readonly SaveExtractedQuoteValidation validation;
    private readonly ICommandHandler<SaveExtractedQuote, Quote> handler;

    public SaveExtractedQuoteEndpoint(SignedInUserResolver users, SaveExtractedQuoteAuthorisation authorisation, SaveExtractedQuoteValidation validation, ICommandHandler<SaveExtractedQuote, Quote> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(SaveExtractedQuote))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bid-packages/{bidPackageId}/extracted-quotes")] HttpRequest request,
        string bidPackageId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<SaveExtractedQuote>();
        if (posted is null) return new BadRequestResult();
        if (posted.BidPackageId != bidPackageId) return new BadRequestObjectResult("Route bidPackageId does not match body.");
        // Who saved is stamped here (it names the email's Extracted verdict), never trusted from the body.
        var command = posted with { SavedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}

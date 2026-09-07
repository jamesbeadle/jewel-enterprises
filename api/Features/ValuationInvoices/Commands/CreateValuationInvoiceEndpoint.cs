using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>
/// POST /api/projects/{projectId}/valuation-invoices — raise a monthly valuation invoice. Body: { periodMonth,
/// amount, valuationClaimId? }.
/// </summary>
public sealed class CreateValuationInvoiceEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateValuationInvoiceAuthorisation authorisation;
    private readonly CreateValuationInvoiceValidation validation;
    private readonly ICommandHandler<CreateValuationInvoice, ValuationInvoice> handler;

    public CreateValuationInvoiceEndpoint(
        SignedInUserResolver users,
        CreateValuationInvoiceAuthorisation authorisation,
        CreateValuationInvoiceValidation validation,
        ICommandHandler<CreateValuationInvoice, ValuationInvoice> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CreateValuationInvoice))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/valuation-invoices")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<CreateValuationInvoice>();
        if (body is null) return new BadRequestResult();

        var command = body with { ProjectId = projectId };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            // A refused raise (claim still a draft, invoice already live) — the reason travels to
            // the page as a 400, never a bodiless 500.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}

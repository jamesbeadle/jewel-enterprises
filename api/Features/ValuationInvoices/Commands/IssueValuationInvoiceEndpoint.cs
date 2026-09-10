using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>POST /api/valuation-invoices/{valuationInvoiceId}/issue — mark the client invoice as sent. Body: { xeroInvoiceNumber? } for one raised in Xero by hand.</summary>
public sealed class IssueValuationInvoiceEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IssueValuationInvoiceAuthorisation authorisation;
    private readonly IssueValuationInvoiceValidation validation;
    private readonly ICommandHandler<IssueValuationInvoice, ValuationInvoice> handler;

    public IssueValuationInvoiceEndpoint(
        SignedInUserResolver users,
        IssueValuationInvoiceAuthorisation authorisation,
        IssueValuationInvoiceValidation validation,
        ICommandHandler<IssueValuationInvoice, ValuationInvoice> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(IssueValuationInvoice))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "valuation-invoices/{valuationInvoiceId}/issue")] HttpRequest request,
        string valuationInvoiceId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        // The body is optional: a hand-raised Xero number travels in it (2026-09-10); the id is the route's.
        IssueValuationInvoice? body = null;
        if (request.ContentLength is not 0)
        {
            try { body = await request.ReadFromJsonAsync<IssueValuationInvoice>(); }
            catch (JsonException) { body = null; } // an empty or non-JSON body is "no options", not an error
        }
        var command = new IssueValuationInvoice(valuationInvoiceId, body?.XeroInvoiceNumber);

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}

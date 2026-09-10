using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>POST /api/valuation-invoices/{valuationInvoiceId}/xero-number — record the number of a sales invoice raised in Xero by hand. Body: { xeroInvoiceNumber }.</summary>
public sealed class RecordValuationInvoiceXeroNumberEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RecordValuationInvoiceXeroNumberAuthorisation authorisation;
    private readonly RecordValuationInvoiceXeroNumberValidation validation;
    private readonly ICommandHandler<RecordValuationInvoiceXeroNumber, ValuationInvoice> handler;

    public RecordValuationInvoiceXeroNumberEndpoint(
        SignedInUserResolver users,
        RecordValuationInvoiceXeroNumberAuthorisation authorisation,
        RecordValuationInvoiceXeroNumberValidation validation,
        ICommandHandler<RecordValuationInvoiceXeroNumber, ValuationInvoice> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RecordValuationInvoiceXeroNumber))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "valuation-invoices/{valuationInvoiceId}/xero-number")] HttpRequest request,
        string valuationInvoiceId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<RecordValuationInvoiceXeroNumber>();
        if (body is null) return new BadRequestResult();

        // RecordedBy is stamped server-side — never trusted from the client.
        var command = body with { ValuationInvoiceId = valuationInvoiceId, RecordedBy = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException refusal)
        {
            return new BadRequestObjectResult(refusal.Message);
        }
    }
}

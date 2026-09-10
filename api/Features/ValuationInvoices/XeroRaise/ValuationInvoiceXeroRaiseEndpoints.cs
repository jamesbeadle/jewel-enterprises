using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.XeroRaise;

/// <summary>GET /api/valuation-invoices/{id}/xero-raise?invoiceDate=yyyy-MM-dd&amp;dueDate=yyyy-MM-dd — what raising would do; nothing is written.</summary>
public sealed class PreviewValuationInvoiceXeroRaiseEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<PreviewValuationInvoiceXeroRaise, ValuationInvoiceXeroRaisePreview> handler;

    public PreviewValuationInvoiceXeroRaiseEndpoint(SignedInUserResolver users, IQueryHandler<PreviewValuationInvoiceXeroRaise, ValuationInvoiceXeroRaisePreview> handler)
    {
        this.users = users; this.handler = handler;
    }

    [Function(nameof(PreviewValuationInvoiceXeroRaise))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "valuation-invoices/{valuationInvoiceId}/xero-raise")] HttpRequest request,
        string valuationInvoiceId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ValuationInvoiceRoles.AllowedToManageValuationInvoices.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        // The user's own dates for this call; blank means today / the certificate rule.
        var invoiceDate = DateOf(request.Query["invoiceDate"]);
        var dueDate = DateOf(request.Query["dueDate"]);
        try
        {
            return new OkObjectResult(await handler.HandleAsync(
                new PreviewValuationInvoiceXeroRaise(valuationInvoiceId, invoiceDate, dueDate), request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }

    private static DateTime? DateOf(string? raw) =>
        !string.IsNullOrWhiteSpace(raw) && DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var parsed)
            ? parsed.Date
            : null;
}

/// <summary>POST /api/valuation-invoices/{id}/xero-raise — raise the AUTHORISED sales invoice in Xero and issue. Body: { invoiceDate?, dueDate? }.</summary>
public sealed class RaiseValuationInvoiceInXeroEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly Audit.AuditActor auditActor;
    private readonly RaiseValuationInvoiceInXeroAuthorisation authorisation;
    private readonly RaiseValuationInvoiceInXeroValidation validation;
    private readonly ICommandHandler<RaiseValuationInvoiceInXero, ValuationInvoiceXeroRaiseOutcome> handler;

    public RaiseValuationInvoiceInXeroEndpoint(
        SignedInUserResolver users,
        Audit.AuditActor auditActor,
        RaiseValuationInvoiceInXeroAuthorisation authorisation,
        RaiseValuationInvoiceInXeroValidation validation,
        ICommandHandler<RaiseValuationInvoiceInXero, ValuationInvoiceXeroRaiseOutcome> handler)
    {
        this.users = users;
        this.auditActor = auditActor;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RaiseValuationInvoiceInXero))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "valuation-invoices/{valuationInvoiceId}/xero-raise")] HttpRequest request,
        string valuationInvoiceId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        auditActor.Email = signedInUser.Email;

        // The body carries the user's dates (optional — an empty body is a raise on the defaults);
        // RaisedBy is stamped server-side — never trusted from the client.
        RaiseValuationInvoiceInXero? body = null;
        if (request.ContentLength is not 0)
        {
            try { body = await request.ReadFromJsonAsync<RaiseValuationInvoiceInXero>(); }
            catch (JsonException) { body = null; } // an empty or non-JSON body is "no options", not an error
        }
        var command = new RaiseValuationInvoiceInXero(valuationInvoiceId, signedInUser.Email, body?.InvoiceDate, body?.DueDate);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(string.Join(" ", validationOutcome.Errors));

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException refusal)
        {
            return new BadRequestObjectResult(refusal.Message);
        }
    }
}

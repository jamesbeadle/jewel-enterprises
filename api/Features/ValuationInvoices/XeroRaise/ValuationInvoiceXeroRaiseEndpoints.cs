using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.XeroRaise;

/// <summary>GET /api/valuation-invoices/{id}/xero-raise — what raising would do; nothing is written.</summary>
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
        try
        {
            return new OkObjectResult(await handler.HandleAsync(new PreviewValuationInvoiceXeroRaise(valuationInvoiceId), request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }
}

/// <summary>POST /api/valuation-invoices/{id}/xero-raise — raise the AUTHORISED sales invoice in Xero and issue.</summary>
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

        // RaisedBy is stamped server-side — never trusted from the client.
        var command = new RaiseValuationInvoiceInXero(valuationInvoiceId, signedInUser.Email);
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

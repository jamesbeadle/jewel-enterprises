using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>POST /api/valuation-invoices/{valuationInvoiceId}/approve — record the client's approval. Body: { note? }.</summary>
public sealed class ApproveValuationInvoiceEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ValuationInvoiceWorkflowAuthorisation authorisation;
    private readonly ICommandHandler<ApproveValuationInvoice, ValuationInvoice> handler;
    private readonly AuditActor auditActor;
    public ApproveValuationInvoiceEndpoint(SignedInUserResolver users, ValuationInvoiceWorkflowAuthorisation authorisation, ICommandHandler<ApproveValuationInvoice, ValuationInvoice> handler, AuditActor auditActor)
    { this.users = users; this.authorisation = authorisation; this.handler = handler; this.auditActor = auditActor; }

    [Function(nameof(ApproveValuationInvoice))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "valuation-invoices/{valuationInvoiceId}/approve")] HttpRequest request,
        string valuationInvoiceId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var body = await request.ReadFromJsonAsync<ApproveValuationInvoice>();
        var command = new ApproveValuationInvoice(valuationInvoiceId, body?.Note);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        auditActor.Email = signedInUser.Email; // the draft programme update the approval opens records who approved
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}

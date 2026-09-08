using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class LinkDirectoryRecordToXeroContactEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly LinkDirectoryRecordToXeroContactAuthorisation authorisation;
    private readonly LinkDirectoryRecordToXeroContactValidation validation;
    private readonly ICommandHandler<LinkDirectoryRecordToXeroContact, Subcontractor> handler;

    public LinkDirectoryRecordToXeroContactEndpoint(SignedInUserResolver users, AuditActor auditActor,
        LinkDirectoryRecordToXeroContactAuthorisation authorisation, LinkDirectoryRecordToXeroContactValidation validation,
        ICommandHandler<LinkDirectoryRecordToXeroContact, Subcontractor> handler)
    {
        this.users = users; this.auditActor = auditActor; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(LinkDirectoryRecordToXeroContact))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subcontractors/{subcontractorId}/xero-link")] HttpRequest request,
        string subcontractorId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<LinkDirectoryRecordToXeroContact>();
        if (body is null) return new BadRequestResult();
        var command = body with { SubcontractorId = subcontractorId };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        auditActor.Email = signedInUser.Email; // the link records who made it

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException guard)
        {
            // Either side already linked, contact gone from Xero, Xero unreachable: answers, not faults.
            return new BadRequestObjectResult(guard.Message);
        }
    }
}

using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class UnlinkDirectoryRecordFromXeroContactEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly UnlinkDirectoryRecordFromXeroContactAuthorisation authorisation;
    private readonly UnlinkDirectoryRecordFromXeroContactValidation validation;
    private readonly ICommandHandler<UnlinkDirectoryRecordFromXeroContact, Subcontractor> handler;

    public UnlinkDirectoryRecordFromXeroContactEndpoint(SignedInUserResolver users, AuditActor auditActor,
        UnlinkDirectoryRecordFromXeroContactAuthorisation authorisation, UnlinkDirectoryRecordFromXeroContactValidation validation,
        ICommandHandler<UnlinkDirectoryRecordFromXeroContact, Subcontractor> handler)
    {
        this.users = users; this.auditActor = auditActor; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(UnlinkDirectoryRecordFromXeroContact))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "subcontractors/{subcontractorId}/xero-link/{xeroContactId}")] HttpRequest request,
        string subcontractorId, string xeroContactId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new UnlinkDirectoryRecordFromXeroContact(subcontractorId, xeroContactId);

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        auditActor.Email = signedInUser.Email;

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException guard)
        {
            return new BadRequestObjectResult(guard.Message);
        }
    }
}

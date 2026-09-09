using Jewel.JPMS.Api.Features.Subcontractors.Commands;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.XeroContacts;

public sealed class PreviewXeroContactPushEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<PreviewXeroContactPush, XeroContactPushPreview> handler;

    public PreviewXeroContactPushEndpoint(SignedInUserResolver users, IQueryHandler<PreviewXeroContactPush, XeroContactPushPreview> handler)
    {
        this.users = users; this.handler = handler;
    }

    [Function(nameof(PreviewXeroContactPush))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "subcontractors/{subcontractorId}/xero-contact-push")] HttpRequest request,
        string subcontractorId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!DirectoryXeroLinkRoles.MayLink.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        try
        {
            return new OkObjectResult(await handler.HandleAsync(new PreviewXeroContactPush(subcontractorId), request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }
}

public sealed class PushDirectoryContactsToXeroContactEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly PushDirectoryContactsToXeroContactAuthorisation authorisation;
    private readonly PushDirectoryContactsToXeroContactValidation validation;
    private readonly ICommandHandler<PushDirectoryContactsToXeroContact, XeroContactPushOutcome> handler;

    public PushDirectoryContactsToXeroContactEndpoint(
        SignedInUserResolver users,
        PushDirectoryContactsToXeroContactAuthorisation authorisation,
        PushDirectoryContactsToXeroContactValidation validation,
        ICommandHandler<PushDirectoryContactsToXeroContact, XeroContactPushOutcome> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(PushDirectoryContactsToXeroContact))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subcontractors/{subcontractorId}/xero-contact-push")] HttpRequest request,
        string subcontractorId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<PushDirectoryContactsToXeroContact>();
        if (command is null) return new BadRequestResult();
        if (command.SubcontractorId != subcontractorId) return new BadRequestObjectResult("Route subcontractorId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            // Not linked, Xero unreachable, or Xero refused the write — answers, not faults.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}

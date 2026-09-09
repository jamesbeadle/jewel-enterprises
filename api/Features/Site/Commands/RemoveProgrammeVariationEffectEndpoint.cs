using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class RemoveProgrammeVariationEffectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ProgrammeVariationEffectAuthorisation authorisation;
    private readonly RemoveProgrammeVariationEffectValidation validation;
    private readonly ICommandHandler<RemoveProgrammeVariationEffect, Acknowledgement> handler;
    public RemoveProgrammeVariationEffectEndpoint(SignedInUserResolver users, ProgrammeVariationEffectAuthorisation authorisation, RemoveProgrammeVariationEffectValidation validation, ICommandHandler<RemoveProgrammeVariationEffect, Acknowledgement> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(RemoveProgrammeVariationEffect))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "programme-variation-effects/{programmeVariationEffectId}")] HttpRequest request, string programmeVariationEffectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new RemoveProgrammeVariationEffect(programmeVariationEffectId);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}

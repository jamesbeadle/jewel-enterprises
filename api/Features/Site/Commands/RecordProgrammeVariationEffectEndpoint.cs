using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class RecordProgrammeVariationEffectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ProgrammeVariationEffectAuthorisation authorisation;
    private readonly RecordProgrammeVariationEffectValidation validation;
    private readonly ICommandHandler<RecordProgrammeVariationEffect, ProgrammeVariationEffect> handler;
    public RecordProgrammeVariationEffectEndpoint(SignedInUserResolver users, ProgrammeVariationEffectAuthorisation authorisation, RecordProgrammeVariationEffectValidation validation, ICommandHandler<RecordProgrammeVariationEffect, ProgrammeVariationEffect> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(RecordProgrammeVariationEffect))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/programme/variation-effects")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<RecordProgrammeVariationEffect>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException reason)
        {
            return new BadRequestObjectResult(new[] { reason.Message });
        }
    }
}

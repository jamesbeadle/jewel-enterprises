using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class RecordCisVerificationEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RecordCisVerificationAuthorisation authorisation;
    private readonly RecordCisVerificationValidation validation;
    private readonly ICommandHandler<RecordCisVerification, Subcontractor> handler;

    public RecordCisVerificationEndpoint(
        SignedInUserResolver users,
        RecordCisVerificationAuthorisation authorisation,
        RecordCisVerificationValidation validation,
        ICommandHandler<RecordCisVerification, Subcontractor> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(RecordCisVerification))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "subcontractors/{subcontractorId}/cis-verification")] HttpRequest request,
        string subcontractorId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<RecordCisVerification>();
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
            // A record that is gone is an answer, not a fault — 400 so the dialog shows the reason.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}

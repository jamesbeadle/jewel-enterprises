using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

/// <summary>PUT /api/subcontractors/{subcontractorId}/compliance/{complianceDocumentId}/details —
/// the record page's "Edit details…" on a current compliance document.</summary>
public sealed class SetComplianceDocumentDetailsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SetComplianceDocumentDetailsAuthorisation authorisation;
    private readonly SetComplianceDocumentDetailsValidation validation;
    private readonly ICommandHandler<SetComplianceDocumentDetails, ComplianceDocument> handler;

    public SetComplianceDocumentDetailsEndpoint(
        SignedInUserResolver users,
        SetComplianceDocumentDetailsAuthorisation authorisation,
        SetComplianceDocumentDetailsValidation validation,
        ICommandHandler<SetComplianceDocumentDetails, ComplianceDocument> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(SetComplianceDocumentDetails))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "subcontractors/{subcontractorId}/compliance/{complianceDocumentId}/details")] HttpRequest request,
        string subcontractorId,
        string complianceDocumentId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<SetComplianceDocumentDetails>();
        if (command is null) return new BadRequestResult();
        if (command.SubcontractorId != subcontractorId) return new BadRequestObjectResult("Route subcontractorId does not match body.");
        if (command.ComplianceDocumentId != complianceDocumentId) return new BadRequestObjectResult("Route complianceDocumentId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            // A missing or superseded document is an answer, not a fault — 400 so the dialog shows it.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}

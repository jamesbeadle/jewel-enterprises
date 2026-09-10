using Jewel.JPMS.Api.Features.Subcontractors;
using Jewel.JPMS.Api.Features.Subcontractors.Storage;
using Jewel.JPMS.Api.Features.Subcontractors.Commands;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Portal.Commands;

/// <summary>
/// POST /api/portal/my/documents — multipart/form-data upload of a compliance document by the
/// signed-in subcontractor. Form fields: <c>file</c>, <c>kind</c> (e.g. "Public liability
/// insurance"), optional <c>expiresAt</c> (ISO-8601). The subcontractor id comes from the session
/// (SubcontractorScope), never the request. Re-uploading a Kind supersedes the previous version.
/// </summary>
public sealed class UploadMyComplianceDocumentEndpoint
{
    // Matches the client-side cap in HttpPortalStore.
    private const long MaxUploadBytes = 100L * 1024 * 1024;

    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IComplianceBlobStore blobStore;
    private readonly ICommandHandler<AddComplianceDocumentVersion, ComplianceDocument> handler;

    public UploadMyComplianceDocumentEndpoint(
        SignedInUserResolver users,
        JpmsContext context,
        IComplianceBlobStore blobStore,
        ICommandHandler<AddComplianceDocumentVersion, ComplianceDocument> handler)
    {
        this.users = users;
        this.context = context;
        this.blobStore = blobStore;
        this.handler = handler;
    }

    [Function("UploadMyComplianceDocument")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "portal/my/documents")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var subcontractorId = SubcontractorScope.OwnSubcontractorId(signedInUser);
        if (subcontractorId is null) return new StatusCodeResult(403);

        var subcontractorExists = await context.Subcontractors
            .AnyAsync(row => row.SubcontractorId == subcontractorId, cancellationToken);
        if (!subcontractorExists) return new NotFoundObjectResult("Your subcontractor record no longer exists.");

        if (!request.HasFormContentType) return new BadRequestObjectResult("Expected multipart/form-data.");
        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
        if (file is null || file.Length == 0) return new BadRequestObjectResult("A non-empty file is required.");
        if (file.Length > MaxUploadBytes) return new BadRequestObjectResult("The file is too large (100 MB max).");

        var kind = form["kind"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(kind)) return new BadRequestObjectResult("A document kind is required.");
        if (kind.Length > 128) return new BadRequestObjectResult("The document kind is too long (128 characters max).");

        DateTimeOffset? expiresAt = null;
        var expiresAtRaw = form["expiresAt"].ToString().Trim();
        if (!string.IsNullOrWhiteSpace(expiresAtRaw))
        {
            if (!DateTimeOffset.TryParse(expiresAtRaw, out var parsed))
                return new BadRequestObjectResult("expiresAt must be an ISO-8601 date.");
            expiresAt = parsed;
        }

        // Optional: the certificate's public liability limit of indemnity, in pounds.
        if (!PublicLiabilityCoverField.TryRead(form, out var publicLiabilityCover, out var coverError))
            return new BadRequestObjectResult(coverError);

        // Clamp to the column widths so an over-long browser filename can't fail the row insert
        // after the blob is already stored (which would orphan the blob).
        var fileName = Path.GetFileName(string.IsNullOrWhiteSpace(file.FileName) ? "document" : file.FileName);
        if (string.IsNullOrWhiteSpace(fileName)) fileName = "document";
        if (fileName.Length > 256) fileName = fileName[^256..];
        var contentType = string.IsNullOrWhiteSpace(file.ContentType) || file.ContentType.Length > 256
            ? "application/octet-stream" : file.ContentType;
        var documentId = SubcontractorIdentifierFactory.NextComplianceDocumentId();

        string blobPath;
        try
        {
            await using var stream = file.OpenReadStream();
            blobPath = await blobStore.UploadAsync(
                subcontractorId, documentId, fileName, contentType, stream, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Storage misconfigured/unreachable — fail clearly, no orphan row (mirrors drawings).
            return new ObjectResult($"Could not store the document — check the compliance storage configuration. ({ex.Message})")
            {
                StatusCode = StatusCodes.Status502BadGateway
            };
        }

        var document = await handler.HandleAsync(
            new AddComplianceDocumentVersion(documentId, subcontractorId, kind, fileName, expiresAt, blobPath, contentType, file.Length, publicLiabilityCover),
            cancellationToken);
        return new OkObjectResult(document);
    }
}

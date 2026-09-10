using Jewel.JPMS.Api.Features.Subcontractors.Storage;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

/// <summary>
/// POST /api/subcontractors/{subcontractorId}/compliance/file — multipart/form-data upload of a
/// compliance document WITH its file bytes, by an office user onto any subcontractor's record
/// (the direct "Add document" on the Subcontractor page). Form fields: <c>file</c>, <c>kind</c>
/// (e.g. "Public liability insurance"), optional <c>expiresAt</c> (ISO-8601). Mirrors the portal's
/// UploadMyComplianceDocument body-for-body so both routes land in the same versioned history —
/// re-uploading a Kind supersedes the previous version (kept for audit), matching case-insensitively
/// so "insurance" and "Insurance" don't fork histories. The JSON UploadComplianceDocument endpoint
/// stays as the legacy metadata-only record (no file).
/// </summary>
public sealed class UploadComplianceDocumentFileEndpoint
{
    // Matches the portal upload's cap and the client-side cap in HttpSubcontractorStore.
    private const long MaxUploadBytes = 100L * 1024 * 1024;

    private readonly SignedInUserResolver users;
    private readonly UploadComplianceDocumentFileAuthorisation authorisation;
    private readonly JpmsContext context;
    private readonly IComplianceBlobStore blobStore;
    private readonly ICommandHandler<AddComplianceDocumentVersion, ComplianceDocument> handler;

    public UploadComplianceDocumentFileEndpoint(
        SignedInUserResolver users,
        UploadComplianceDocumentFileAuthorisation authorisation,
        JpmsContext context,
        IComplianceBlobStore blobStore,
        ICommandHandler<AddComplianceDocumentVersion, ComplianceDocument> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.context = context;
        this.blobStore = blobStore;
        this.handler = handler;
    }

    [Function(nameof(UploadComplianceDocumentFileEndpoint))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subcontractors/{subcontractorId}/compliance/file")] HttpRequest request,
        string subcontractorId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!authorisation.Allows(signedInUser)) return new StatusCodeResult(403);

        var subcontractorExists = await context.Subcontractors
            .AnyAsync(row => row.SubcontractorId == subcontractorId, cancellationToken);
        if (!subcontractorExists) return new NotFoundObjectResult("No such subcontractor.");

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
            // Storage misconfigured/unreachable — fail clearly, no orphan row (mirrors the portal upload).
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

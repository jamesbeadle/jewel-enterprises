using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

/// <summary>
/// Legacy metadata-only record (no file bytes). Routed through the same versioning writer as file
/// uploads so a metadata record of an existing Kind supersedes rather than duplicating it.
/// </summary>
public sealed class UploadComplianceDocumentHandler
    : ICommandHandler<UploadComplianceDocument, ComplianceDocument>
{
    private readonly ICommandHandler<AddComplianceDocumentVersion, ComplianceDocument> versionWriter;

    public UploadComplianceDocumentHandler(
        ICommandHandler<AddComplianceDocumentVersion, ComplianceDocument> versionWriter)
    {
        this.versionWriter = versionWriter;
    }

    public Task<ComplianceDocument> HandleAsync(UploadComplianceDocument command, CancellationToken cancellationToken) =>
        versionWriter.HandleAsync(
            new AddComplianceDocumentVersion(
                SubcontractorIdentifierFactory.NextComplianceDocumentId(),
                command.SubcontractorId, command.Kind, command.FileName, command.ExpiresAt,
                BlobPath: "", ContentType: "", FileSize: 0, command.PublicLiabilityCover),
            cancellationToken);
}

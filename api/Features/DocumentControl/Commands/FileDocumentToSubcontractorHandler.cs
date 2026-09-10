using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.DocumentControl.Storage;
using Jewel.JPMS.Api.Features.Subcontractors;
using Jewel.JPMS.Api.Features.Subcontractors.Storage;
using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

// Files a pending Document Control item onto a subcontractor's record as a versioned compliance
// document. The bytes are copied into the compliance blob store and the versioning itself is the
// existing AddComplianceDocumentVersion handler — shared, not forked — so superseding, version
// numbers and the portal upload flow all stay one behaviour.
public sealed class FileDocumentToSubcontractorHandler
    : ICommandHandler<FileDocumentToSubcontractor, DocumentControlItem>
{
    private readonly JpmsContext context;
    private readonly IDocumentControlBlobStore documentBlobs;
    private readonly IComplianceBlobStore complianceBlobs;
    private readonly ICommandHandler<AddComplianceDocumentVersion, ComplianceDocument> addVersion;
    private readonly AuditActor actor;
    private readonly AuditTrail auditTrail;

    public FileDocumentToSubcontractorHandler(
        JpmsContext context, IDocumentControlBlobStore documentBlobs, IComplianceBlobStore complianceBlobs,
        ICommandHandler<AddComplianceDocumentVersion, ComplianceDocument> addVersion,
        AuditActor actor, AuditTrail auditTrail)
    {
        this.context = context; this.documentBlobs = documentBlobs; this.complianceBlobs = complianceBlobs;
        this.addVersion = addVersion; this.actor = actor; this.auditTrail = auditTrail;
    }

    public async Task<DocumentControlItem> HandleAsync(
        FileDocumentToSubcontractor command, CancellationToken cancellationToken)
    {
        var item = await context.DocumentControlItems
            .FirstOrDefaultAsync(row => row.DocumentControlItemId == command.DocumentControlItemId, cancellationToken)
            ?? throw new InvalidOperationException("That document is no longer in Document Triage.");
        if (item.Status != (int)DocumentControlStatus.Pending)
            throw new InvalidOperationException("That document has already been filed or discarded — restore it to the queue first.");

        var subcontractor = await context.Subcontractors
            .FirstOrDefaultAsync(row => row.SubcontractorId == command.SubcontractorId, cancellationToken)
            ?? throw new InvalidOperationException("Select the subcontractor this document belongs to.");

        var sourceBlob = await documentBlobs.OpenAsync(item.BlobRef, cancellationToken)
            ?? throw new InvalidOperationException("The stored file could not be found in Document Triage's storage.");

        // Pre-generated so the row id matches the blob path segment — same rule as the upload endpoints.
        var complianceDocumentId = SubcontractorIdentifierFactory.NextComplianceDocumentId();
        string blobPath;
        await using (var content = sourceBlob.Content)
        {
            blobPath = await complianceBlobs.UploadAsync(
                command.SubcontractorId, complianceDocumentId,
                item.FileName, item.ContentType, content, cancellationToken);
        }

        var kind = command.Kind.Trim();
        var document = await addVersion.HandleAsync(
            new AddComplianceDocumentVersion(
                complianceDocumentId, command.SubcontractorId, kind,
                item.FileName, command.ExpiresAt, blobPath, item.ContentType, item.FileSizeBytes,
                command.PublicLiabilityCover),
            cancellationToken);

        item.Status = (int)DocumentControlStatus.Filed;
        item.ResolvedBy = actor.Email;
        item.ResolvedAt = DateTimeOffset.UtcNow;
        item.FiledAsKind = (int)DocumentFiledAs.SubcontractorDocument;
        item.FiledRecordId = document.ComplianceDocumentId;
        item.FiledLabel = $"{kind} v{document.Version} on {subcontractor.CompanyName}";
        await context.SaveChangesAsync(cancellationToken);

        await auditTrail.WriteAsync(
            AuditEventType.DocumentFiled,
            $"Filed \"{item.FileName}\" from Document Triage as {item.FiledLabel}",
            emailMessageId: item.MessageId,
            internetMessageId: item.InternetMessageId,
            cancellationToken: cancellationToken);

        return item.ToModel();
    }
}

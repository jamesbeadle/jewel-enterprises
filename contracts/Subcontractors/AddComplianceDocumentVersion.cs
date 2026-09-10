using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Subcontractors;

/// <summary>
/// Records a stored compliance file as the new current version of its Kind, superseding (never
/// replacing) the previous version. Constructed SERVER-SIDE by the multipart upload endpoints
/// after the blob is stored — not sent by clients, so it has no client route registration.
/// </summary>
public sealed record AddComplianceDocumentVersion(
    string ComplianceDocumentId,
    string SubcontractorId,
    string Kind,
    string FileName,
    DateTimeOffset? ExpiresAt,
    string BlobPath,
    string ContentType,
    long FileSize,
    decimal? PublicLiabilityCover = null) : ICommand<ComplianceDocument>;

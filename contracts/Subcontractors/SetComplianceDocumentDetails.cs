using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Subcontractors;

/// <summary>Corrects the facts recorded against the CURRENT version of a compliance document —
/// its expiry date and the public liability cover it certifies — without a re-upload (2026-09-10,
/// the accountant's ask: the insurance documents already on file needed their £ figure keyed in,
/// and until now the only way to change anything on a document was to file the same PDF again
/// as a new version). The file, kind and version are untouched; a superseded version is history
/// and is refused. Both values are the whole new reading: null clears.</summary>
public sealed record SetComplianceDocumentDetails(
    string SubcontractorId,
    string ComplianceDocumentId,
    DateTimeOffset? ExpiresAt,
    decimal? PublicLiabilityCover) : ICommand<ComplianceDocument>;


namespace Jewel.JPMS.Services;

/// <summary>
/// The Document Control register — the attachment triage queue and its history. Uncached, like
/// the Architect's Instruction register: one short list read on entry to one page, and a document
/// landing while someone is triaging is exactly the moment a stale copy would mislead them.
/// </summary>
public interface IDocumentControlStore
{
    /// <summary>Every item, all statuses, newest received first — the page splits its
    /// Queue / Filed / Discarded views client-side.</summary>
    Task<IReadOnlyList<DocumentControlItem>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Files an item into a project's drawings as an Unapproved revision — of the given
    /// drawing when <paramref name="drawingId"/> is set, else matched by code within the project
    /// (a new or blank code registers a new drawing, filed under <paramref name="drawingFolderId"/>
    /// when given).</summary>
    Task<DocumentControlItem> FileAsDrawingAsync(
        string documentControlItemId, string projectId, string drawingCode, string title,
        string revisionLabel, string? drawingId = null, string? drawingFolderId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Files an item as a payment certificate on a project, optionally tied to the
    /// valuation claim it certifies.</summary>
    Task<DocumentControlItem> FileAsPaymentCertificateAsync(
        string documentControlItemId, string projectId, string certificateNumber,
        decimal? certifiedAmount, DateTimeOffset issuedDate, string? valuationClaimId,
        CancellationToken cancellationToken = default);

    /// <summary>Files an item onto a subcontractor's record as a versioned compliance document
    /// (RAMS, Insurance, Drawings / Specifications…).</summary>
    Task<DocumentControlItem> FileToSubcontractorAsync(
        string documentControlItemId, string subcontractorId, string kind, DateTimeOffset? expiresAt,
        decimal? publicLiabilityCover = null, CancellationToken cancellationToken = default);

    Task<DocumentControlItem> DiscardAsync(string documentControlItemId, CancellationToken cancellationToken = default);

    Task<DocumentControlItem> RestoreAsync(string documentControlItemId, CancellationToken cancellationToken = default);

    /// <summary>Splits a pending zip item into one queue item per contained file — each is then
    /// previewed and filed individually. Returns the newly created items.</summary>
    Task<IReadOnlyList<DocumentControlItem>> ExtractArchiveAsync(
        string documentControlItemId, CancellationToken cancellationToken = default);

    /// <summary>The API URL that streams an item's stored file (proxied — the container is private).</summary>
    string FileUrl(string documentControlItemId, bool inline = false) =>
        $"api/document-control/items/{documentControlItemId}/file" + (inline ? "?inline=1" : "");
}

using Jewel.JPMS.Contracts.DocumentControl;

namespace Jewel.JPMS.Services;

public sealed class HttpDocumentControlStore : IDocumentControlStore
{
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpDocumentControlStore(IQueryClient queries, ICommandSender commands)
    {
        this.queries = queries;
        this.commands = commands;
    }

    public Task<IReadOnlyList<DocumentControlItem>> ListAsync(CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListDocumentControlItems(), cancellationToken);

    public Task<DocumentControlItem> FileAsDrawingAsync(
        string documentControlItemId, string projectId, string drawingCode, string title,
        string revisionLabel, string? drawingId = null, string? drawingFolderId = null,
        CancellationToken cancellationToken = default) =>
        commands.SendAsync(
            new FileDocumentAsDrawing(documentControlItemId, projectId, drawingCode, title, revisionLabel, drawingId, drawingFolderId),
            cancellationToken);

    public Task<DocumentControlItem> FileAsPaymentCertificateAsync(
        string documentControlItemId, string projectId, string certificateNumber,
        decimal? certifiedAmount, DateTimeOffset issuedDate, string? valuationClaimId,
        CancellationToken cancellationToken = default) =>
        commands.SendAsync(
            new FileDocumentAsPaymentCertificate(
                documentControlItemId, projectId, certificateNumber, certifiedAmount, issuedDate, valuationClaimId),
            cancellationToken);

    public Task<DocumentControlItem> FileToSubcontractorAsync(
        string documentControlItemId, string subcontractorId, string kind, DateTimeOffset? expiresAt,
        decimal? publicLiabilityCover = null, CancellationToken cancellationToken = default) =>
        commands.SendAsync(
            new FileDocumentToSubcontractor(documentControlItemId, subcontractorId, kind, expiresAt, publicLiabilityCover),
            cancellationToken);

    public Task<DocumentControlItem> DiscardAsync(string documentControlItemId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new DiscardDocumentControlItem(documentControlItemId), cancellationToken);

    public Task<DocumentControlItem> RestoreAsync(string documentControlItemId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new RestoreDocumentControlItem(documentControlItemId), cancellationToken);

    public Task<IReadOnlyList<DocumentControlItem>> ExtractArchiveAsync(
        string documentControlItemId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new ExtractDocumentControlArchive(documentControlItemId), cancellationToken);
}

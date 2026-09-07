using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

// A revision's data view. Markups come from SQL; the structured read and the text pages come
// back off their blobs (they can run long, and the register never queries into them). A blob
// that has gone missing degrades to null/empty rather than failing the whole view — the row's
// summary and status still render.
public sealed class GetDrawingExtractionHandler : IQueryHandler<GetDrawingExtraction, DrawingExtractionView?>
{
    private readonly JpmsContext context;
    private readonly IDrawingBlobStore drawingBlobs;

    public GetDrawingExtractionHandler(JpmsContext context, IDrawingBlobStore drawingBlobs)
    {
        this.context = context; this.drawingBlobs = drawingBlobs;
    }

    public async Task<DrawingExtractionView?> HandleAsync(GetDrawingExtraction query, CancellationToken cancellationToken)
    {
        var extraction = await context.DrawingExtractions
            .FirstOrDefaultAsync(row => row.DrawingRevisionId == query.DrawingRevisionId, cancellationToken);
        if (extraction is null) return null;

        var markups = await context.DrawingMarkups
            .Where(row => row.DrawingExtractionId == extraction.DrawingExtractionId)
            .OrderBy(row => row.PageNumber)
            .ThenBy(row => row.MarkupType)
            .ToListAsync(cancellationToken);

        var structure = await ReadBlobAsync<DrawingStructure>(extraction.StructureBlobRef, cancellationToken);
        var textPages = await ReadBlobAsync<List<DrawingTextPage>>(extraction.TextBlobRef, cancellationToken);
        return new DrawingExtractionView(
            extraction.ToModel(),
            structure,
            markups.Select(markup => markup.ToModel()).ToList(),
            textPages ?? new List<DrawingTextPage>());
    }

    private async Task<T?> ReadBlobAsync<T>(string? blobRef, CancellationToken cancellationToken) where T : class
    {
        if (string.IsNullOrWhiteSpace(blobRef)) return null;
        var blob = await drawingBlobs.OpenAsync(blobRef, cancellationToken);
        if (blob is null) return null;

        await using var content = blob.Content;
        try
        {
            return await JsonSerializer.DeserializeAsync<T>(content, cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

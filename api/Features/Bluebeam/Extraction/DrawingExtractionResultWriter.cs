using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

/// <summary>
/// Persists an extraction's outcome. Success stores the payloads as blobs under the revision's
/// own key prefix — text as [{page,text}] JSON, the positioned geometry, the structured read,
/// and Bluebeam's markups verbatim when they were read — replaces the revision's markup rows,
/// stamps the extraction row's summary (counts, title block, scale) AND the revision's
/// MetadataExtractedAt (the register's badge) in one save, then writes the audit row directly —
/// the worker doesn't link AuditTrail, and audit stays best-effort either way. Failure stamps
/// the row so the UI can show the error the queue's retries keep hitting.
/// </summary>
public sealed class DrawingExtractionResultWriter
{
    private readonly JpmsContext context;
    private readonly IDrawingBlobStore drawingBlobs;
    private readonly ILogger<DrawingExtractionResultWriter> logger;

    public DrawingExtractionResultWriter(
        JpmsContext context, IDrawingBlobStore drawingBlobs, ILogger<DrawingExtractionResultWriter> logger)
    {
        this.context = context; this.drawingBlobs = drawingBlobs; this.logger = logger;
    }

    public async Task RecordSuccessAsync(
        DrawingExtractionEntity extraction, DrawingRevisionEntity revision,
        DrawingExtractionOutcome outcome, string requestedBy, CancellationToken cancellationToken)
    {
        extraction.TextBlobRef = await UploadJsonAsync(
            extraction, "extraction-text.json", JsonSerializer.Serialize(outcome.TextLayer.Pages), cancellationToken);
        extraction.GeometryBlobRef = await UploadJsonAsync(
            extraction, "extraction-geometry.json", JsonSerializer.Serialize(outcome.Geometry), cancellationToken);
        extraction.StructureBlobRef = await UploadJsonAsync(
            extraction, "extraction-structure.json", JsonSerializer.Serialize(outcome.Structure), cancellationToken);
        extraction.MarkupsBlobRef = outcome.MarkupsRawJson is null
            ? null
            : await UploadJsonAsync(extraction, "extraction-markups.json", outcome.MarkupsRawJson, cancellationToken);

        var markups = outcome.MarkupsRawJson is null
            ? new List<DrawingMarkupEntity>()
            : BluebeamMarkupParser.Parse(outcome.MarkupsRawJson, extraction.DrawingExtractionId, extraction.DrawingRevisionId);
        var existingRows = await context.DrawingMarkups
            .Where(row => row.DrawingRevisionId == extraction.DrawingRevisionId)
            .ToListAsync(cancellationToken);
        context.DrawingMarkups.RemoveRange(existingRows);
        context.DrawingMarkups.AddRange(markups);

        var now = DateTimeOffset.UtcNow;
        extraction.Status = (int)DrawingExtractionStatus.Succeeded;
        extraction.CompletedAt = now;
        extraction.ErrorMessage = null;
        extraction.PageCount = outcome.TextLayer.Pages.Count;
        extraction.PagesJson = PdfTextLayerExtractor.GeometryJson(outcome.TextLayer.Geometry);
        extraction.MarkupCount = outcome.MarkupsRawJson is null ? null : markups.Count;
        extraction.MarkupsNote = outcome.MarkupsNote;
        StampSummary(extraction, outcome.Structure);
        revision.MetadataExtractedAt = now;
        await context.SaveChangesAsync(cancellationToken);

        await WriteAuditRowAsync(extraction, revision, requestedBy, cancellationToken);
    }

    // The register-facing summary: what the sheet says it is and how much the read found.
    private static void StampSummary(DrawingExtractionEntity extraction, DrawingStructure structure)
    {
        var scale = structure.Scales.FirstOrDefault(page => page.MmPerPoint is not null);
        extraction.DimensionCount = structure.Dimensions.Count;
        extraction.CalloutCount = structure.Callouts.Count;
        extraction.ShapeCount = structure.Shapes.Count;
        extraction.Scale = scale is null ? null : ScaleLabel(scale);
        extraction.ScaleVerified = scale is null ? null : scale.Verified;
        extraction.DrawingNumber = Clip(structure.TitleBlock.DrawingNumber, 128);
        extraction.RevisionLabel = Clip(structure.TitleBlock.Revision ?? structure.Revisions.LastOrDefault()?.Id, 32);
    }

    public static string ScaleLabel(DrawingPageScale scale) =>
        scale.Declared ?? (scale.MmPerPoint is { } mm ? $"1:{Math.Round(mm * 72 / 25.4)}" : "");

    private static string? Clip(string? value, int maxLength) =>
        value is null ? null : value.Length <= maxLength ? value : value[..maxLength];

    public async Task RecordFailureAsync(
        DrawingExtractionEntity extraction, Exception failure, CancellationToken cancellationToken)
    {
        // The success path may have staged half its changes on this same context before throwing
        // (markup rows, blob refs, even MetadataExtractedAt). Saving the Failed stamp must not
        // flush those — a row marked Failed that also reads "extracted" would hide the revision
        // from every later bulk queue. Drop the staged state and stamp a freshly-loaded row.
        context.ChangeTracker.Clear();
        var row = await context.DrawingExtractions
            .FirstOrDefaultAsync(
                candidate => candidate.DrawingExtractionId == extraction.DrawingExtractionId,
                CancellationToken.None);
        if (row is null) return;
        row.Status = (int)DrawingExtractionStatus.Failed;
        row.CompletedAt = DateTimeOffset.UtcNow;
        row.ErrorMessage = failure.Message.Length <= 2048 ? failure.Message : failure.Message[..2048];
        row.BluebeamSessionId = extraction.BluebeamSessionId;
        await context.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<string> UploadJsonAsync(
        DrawingExtractionEntity extraction, string fileName, string json, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        return await drawingBlobs.UploadAsync(
            extraction.ProjectId, extraction.DrawingId, extraction.DrawingRevisionId,
            fileName, "application/json", stream, cancellationToken);
    }

    // Best-effort, after the result is safely saved — an audit hiccup must never fail the run.
    private async Task WriteAuditRowAsync(
        DrawingExtractionEntity extraction, DrawingRevisionEntity revision, string requestedBy,
        CancellationToken cancellationToken)
    {
        try
        {
            var scale = extraction.Scale is null ? "no scale"
                : $"scale {extraction.Scale}{(extraction.ScaleVerified == true ? " (verified)" : " (unverified)")}";
            var markups = extraction.MarkupCount is { } count ? $"{count} Revu markup(s)" : "no Revu markups";
            context.AuditEvents.Add(new AuditEventEntity
            {
                AuditEventId = Guid.NewGuid().ToString("N"),
                OccurredAt = DateTimeOffset.UtcNow,
                ActorEmail = requestedBy,
                EventType = (int)AuditEventType.DrawingDataExtracted,
                Pathway = "",
                ProjectId = extraction.ProjectId,
                RecordReference = "",
                Detail = $"Extracted drawing data from \"{revision.FileName}\" — "
                    + $"{extraction.DimensionCount ?? 0} dimension(s), {extraction.CalloutCount ?? 0} note(s), "
                    + $"{extraction.ShapeCount ?? 0} shape(s), {scale}, {markups}"
            });
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Audit write failed for extraction {ExtractionId}.", extraction.DrawingExtractionId);
        }
    }
}

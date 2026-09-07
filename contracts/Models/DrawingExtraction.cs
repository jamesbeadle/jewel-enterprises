namespace Jewel.JPMS.Models;

/// <summary>Where a revision's extraction is. Queued and Running are transient (the worker owns
/// the move); Succeeded and Failed are terminal until someone re-extracts.</summary>
public enum DrawingExtractionStatus
{
    Queued = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3
}

/// <summary>
/// One drawing revision's extraction — pipeline status plus, once it succeeds, the shape of what
/// came out. The counts and title-block summary are denormalised here so a register can show
/// them without opening the blobs; the payloads themselves ride on DrawingExtractionView.
/// MarkupsNote says why Bluebeam markups are absent when they are (not connected, call failed)
/// — the structured read succeeds regardless, because it comes from the PDF itself.
/// </summary>
public sealed record DrawingExtraction(
    string DrawingExtractionId,
    string DrawingRevisionId,
    string DrawingId,
    string ProjectId,
    DrawingExtractionStatus Status,
    string QueuedBy,
    DateTimeOffset QueuedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int Attempts,
    string? ErrorMessage,
    int? PageCount,
    int? MarkupCount,
    string? MarkupsNote,
    int? DimensionCount,
    int? CalloutCount,
    int? ShapeCount,
    string? Scale,
    bool? ScaleVerified,
    string? DrawingNumber,
    string? RevisionLabel);

/// <summary>One markup from an extraction, as the data view renders it.</summary>
public sealed record DrawingMarkup(
    string DrawingMarkupId,
    string BluebeamMarkupId,
    int PageNumber,
    string MarkupType,
    string Subject,
    string Author,
    string Comment,
    string Colour,
    decimal? MeasurementValue,
    string? MeasurementUnit);

/// <summary>One page's embedded text layer (PdfPig's read of the PDF, not OCR — a scanned
/// drawing with no text layer legitimately comes back empty).</summary>
public sealed record DrawingTextPage(int Page, string Text);

/// <summary>The full data view for a revision: the extraction row, the structured read of the
/// sheet, its Bluebeam markups (if any were read), and the per-page text. Null structure and
/// empty lists simply mean the run hasn't succeeded yet.</summary>
public sealed record DrawingExtractionView(
    DrawingExtraction Extraction,
    DrawingStructure? Structure,
    IReadOnlyList<DrawingMarkup> Markups,
    IReadOnlyList<DrawingTextPage> TextPages);

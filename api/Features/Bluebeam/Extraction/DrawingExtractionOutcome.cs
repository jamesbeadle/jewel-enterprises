using Jewel.JPMS.Api.Features.Drawings.Geometry;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

/// <summary>
/// Everything one extraction run produced, handed from the runner to the result writer. The
/// first three come from the PDF itself and are always present on a successful run; the markups
/// only exist when Bluebeam was connected and answered — MarkupsNote says why when they don't.
/// </summary>
public sealed record DrawingExtractionOutcome(
    PdfTextLayerExtractor.TextLayer TextLayer,
    DrawingGeometry Geometry,
    DrawingStructure Structure,
    string? MarkupsRawJson,
    string? MarkupsNote);

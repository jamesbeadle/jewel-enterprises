namespace Jewel.JPMS.Models;

/// <summary>
/// The structured read of a drawing revision — what the portal derives from the PDF's OWN vector
/// geometry and positioned text (layer three of the extraction; the raw positioned layer sits
/// behind it as a blob). Nothing here comes from Bluebeam: markups are a separate, optional input
/// that only exists when a person has measured in Revu. Every coordinate is in real-world
/// millimetres from the sheet's bottom-left corner, already multiplied through the page's scale,
/// so a dimension's From/To can be compared with its Value directly.
/// </summary>
public sealed record DrawingStructure(
    DrawingTitleBlock TitleBlock,
    IReadOnlyList<DrawingRevisionNote> Revisions,
    IReadOnlyList<DrawingPageScale> Scales,
    IReadOnlyList<DrawingDimension> Dimensions,
    IReadOnlyList<DrawingCallout> Callouts,
    IReadOnlyList<DrawingShape> Shapes,
    IReadOnlyList<string> Warnings);

/// <summary>A point in real-world millimetres from the sheet's bottom-left corner.</summary>
public sealed record DrawingPoint(double X, double Y);

/// <summary>
/// Title-block fields read by label proximity ("DRAWING No." → the text beneath it). Any field the
/// sheet doesn't label the usual way is null; RawText is the whole title-block region so a reader
/// (person or model) can still find what the heuristics missed.
/// </summary>
public sealed record DrawingTitleBlock(
    string? DrawingNumber,
    string? Title,
    string? Revision,
    string? Scale,
    string? SheetSize,
    string? Date,
    string? DrawnBy,
    string? Job,
    string? Client,
    string RawText);

/// <summary>One row of the sheet's revision table ("N  23-02-2026  MVL  Updated …").</summary>
public sealed record DrawingRevisionNote(string Id, string? Date, string? By, string Comment);

/// <summary>
/// How a page's scale was established. Declared is the title block's own statement ("1:50");
/// MmPerPoint is the real-world millimetres one PDF point stands for at that scale. Verified means
/// the figured dimensions on the sheet matched drawn lines of that length — the sheet proves its
/// own scale — and the counts say how strong that proof is. Inferred is set when no declaration
/// was found and the scale was chosen by that same test.
/// </summary>
public sealed record DrawingPageScale(
    int Page,
    string? Declared,
    double? MmPerPoint,
    int LabelsChecked,
    int LabelsMatched,
    bool Verified,
    bool Inferred);

/// <summary>
/// A figured dimension paired with the line it measures. Axis is H (horizontal), V (vertical) or
/// D (diagonal). ValueMm is the figure as written; From/To are the drawn line's ends, LabelAt the
/// figure's position — so the figure, not the drawn length, is what a take-off should use (every
/// architect's sheet says figured dimensions take preference over scaling).
/// </summary>
public sealed record DrawingDimension(
    int Page,
    int ValueMm,
    string Axis,
    DrawingPoint From,
    DrawingPoint To,
    DrawingPoint LabelAt);

/// <summary>A block of text on the plan with where it sits — a specification callout, a note.</summary>
public sealed record DrawingCallout(int Page, DrawingPoint At, string Text);

/// <summary>
/// A closed vector shape — a pad, a slab outline, a chamber — with its size in real units.
/// Centre/Width/Height describe the bounding box; PerimeterMm and AreaSqM are the polygon's own.
/// HasCurves means the outline included curves that were read as straight chords, so the box is
/// right but the perimeter and area are approximate (a circle reads as its inscribed polygon).
/// </summary>
public sealed record DrawingShape(
    int Page,
    int PointCount,
    bool IsRectangle,
    bool HasCurves,
    DrawingPoint Centre,
    double WidthMm,
    double HeightMm,
    double PerimeterMm,
    double AreaSqM);

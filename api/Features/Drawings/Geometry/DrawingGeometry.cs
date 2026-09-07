namespace Jewel.JPMS.Api.Features.Drawings.Geometry;

/// <summary>
/// Layer two of a drawing extraction: everything positional the PDF itself holds, read
/// deterministically with PdfPig and stored verbatim as a blob under the revision. Coordinates are
/// PDF points in the page's own user space (origin bottom-left, y upward) — no scale applied, so
/// the structured read (layer three) can be rebuilt from this blob whenever its rules improve
/// without touching the PDF again. Text is normalised (ligature glyphs mapped back) but otherwise
/// exactly where the sheet placed it.
/// </summary>
public sealed record DrawingGeometry(IReadOnlyList<DrawingGeometryPage> Pages);

public sealed record DrawingGeometryPage(
    int Page,
    double WidthPoints,
    double HeightPoints,
    int Rotation,
    IReadOnlyList<GeometryWord> Words,
    IReadOnlyList<GeometrySegment> Segments,
    IReadOnlyList<GeometryPolygon> Polygons);

/// <summary>A word with its bounding box (X0 ≤ X1, Y0 ≤ Y1 always). Orientation is PdfPig's:
/// Horizontal, Rotate90, Rotate180, Rotate270 or Other — vertical dimension figures come through
/// as Rotate90/270, and Archicad's plan callouts as Other because they sit a fraction of a degree
/// off. AngleDegrees is the word's own baseline angle (0 = left to right, 90 = reading upward),
/// which is what actually decides whether words share a line.</summary>
public sealed record GeometryWord(string Text, double X0, double Y0, double X1, double Y1, string Orientation, double AngleDegrees);

/// <summary>One straight stroke. Curves are NOT segments — see GeometryPolygon.HasCurves.</summary>
public sealed record GeometrySegment(double X1, double Y1, double X2, double Y2);

/// <summary>
/// A closed subpath as its vertex list ([x, y] pairs). Bézier curves contribute only their end
/// points, so a circle drawn as four curves is a four-point polygon flagged HasCurves — its
/// bounding box is still right, its area is not.
/// </summary>
public sealed record GeometryPolygon(
    IReadOnlyList<double[]> Points,
    bool IsRectangle,
    bool HasCurves,
    bool IsFilled,
    bool IsStroked);

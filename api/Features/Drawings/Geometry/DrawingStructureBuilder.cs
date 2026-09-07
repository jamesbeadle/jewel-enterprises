namespace Jewel.JPMS.Api.Features.Drawings.Geometry;

/// <summary>
/// Layer three: turns the positioned geometry (and the plain text pages, for the revision table)
/// into the DrawingStructure a person or model reasons over — title block, revision rows, a
/// proven scale per page, every figured dimension paired with its drawn line, every text block
/// with its position, every closed shape with its real-world size. Pure: same blob in, same
/// structure out, so it can be re-run over stored geometry when these rules improve.
/// </summary>
public static class DrawingStructureBuilder
{
    private const int ShapeCap = 400;
    private const int CalloutCap = 600;

    public static DrawingStructure Build(DrawingGeometry geometry, IReadOnlyList<DrawingTextPage> textPages)
    {
        var warnings = new List<string>();
        var scales = new List<DrawingPageScale>();
        var dimensions = new List<DrawingDimension>();
        var callouts = new List<DrawingCallout>();
        var shapes = new List<DrawingShape>();
        DrawingTitleBlock? titleBlock = null;

        if (geometry.Pages.All(page => page.Words.Count == 0))
            warnings.Add("No embedded text on any page — a scanned or purely graphical sheet. Nothing can be measured from it; someone has to read the picture.");

        foreach (var page in geometry.Pages)
        {
            titleBlock ??= page.Words.Count == 0 ? null : DrawingTitleBlockReader.Read(page.Words);

            var calibration = DrawingScaleCalibrator.Calibrate(page);
            scales.Add(new DrawingPageScale(
                page.Page, calibration.Declared, calibration.MmPerPoint is { } mm ? Math.Round(mm, 6) : null,
                calibration.LabelsChecked, calibration.LabelsMatched, calibration.Verified, calibration.Inferred));
            if (calibration.Inferred)
                warnings.Add($"Page {page.Page}: no scale declared in a form the reader knows — inferred 1:{Math.Round(calibration.MmPerPoint!.Value * 72 / 25.4)} from {calibration.LabelsMatched} of {calibration.LabelsChecked} figured dimensions.");
            else if (!calibration.Verified && calibration.MmPerPoint is not null)
                warnings.Add($"Page {page.Page}: the declared scale {calibration.Declared} is unverified — only {calibration.LabelsMatched} of {calibration.LabelsChecked} figured dimensions matched a drawn line. Treat measurements from this page with care.");
            else if (calibration.MmPerPoint is null && page.Words.Count > 0)
                warnings.Add($"Page {page.Page}: no scale could be established, so no dimensions or shapes were measured on it.");

            callouts.AddRange(Callouts(page, calibration.MmPerPoint));
            if (calibration.MmPerPoint is not { } mmPerPoint) continue;

            foreach (var match in calibration.Matches)
                dimensions.Add(Dimension(page.Page, match, mmPerPoint));
            shapes.AddRange(Shapes(page, mmPerPoint, warnings));
        }

        if (callouts.Count > CalloutCap)
        {
            warnings.Add($"{callouts.Count} text blocks found; the {CalloutCap} longest are kept.");
            callouts = callouts.OrderByDescending(callout => callout.Text.Length).Take(CalloutCap).ToList();
        }

        return new DrawingStructure(
            titleBlock ?? new DrawingTitleBlock(null, null, null, null, null, null, null, null, null, ""),
            RevisionRows(textPages),
            scales, dimensions, callouts, shapes, warnings);
    }

    private static IReadOnlyList<DrawingRevisionNote> RevisionRows(IReadOnlyList<DrawingTextPage> textPages)
    {
        var rows = new List<DrawingRevisionNote>();
        foreach (var textPage in textPages) rows.AddRange(DrawingRevisionTableReader.Read(textPage.Text));
        return rows;
    }

    private static DrawingDimension Dimension(int page, DrawingScaleCalibrator.DimensionMatch match, double mmPerPoint)
    {
        var segment = match.Segment;
        var dx = Math.Abs(segment.X2 - segment.X1);
        var dy = Math.Abs(segment.Y2 - segment.Y1);
        var axis = dx > dy * 3 ? "H" : dy > dx * 3 ? "V" : "D";
        return new DrawingDimension(
            page, match.ValueMm, axis,
            Point(segment.X1, segment.Y1, mmPerPoint), Point(segment.X2, segment.Y2, mmPerPoint),
            Point((match.Label.X0 + match.Label.X1) / 2, (match.Label.Y0 + match.Label.Y1) / 2, mmPerPoint));
    }

    // Text blocks with something to say: three or more words, not a bare figure, not a revision
    // row (those are read separately), positioned by their centre. Without a scale the position
    // is still given, in sheet millimetres at 1:1 — the same convention, just unscaled, and the
    // page's scale row says so.
    private static IEnumerable<DrawingCallout> Callouts(DrawingGeometryPage page, double? mmPerPoint)
    {
        var scale = mmPerPoint ?? 25.4 / 72;
        foreach (var block in DrawingTextBlocks.Group(page.Words))
        {
            if (block.WordCount < 3 || block.Text.Length < 12) continue;
            if (!block.Text.Any(char.IsLetter)) continue;
            if (DrawingRevisionTableReader.IsRevisionRow(block.Text)) continue;
            yield return new DrawingCallout(
                page.Page,
                Point((block.Left + block.Right) / 2, (block.Bottom + block.Top) / 2, scale),
                block.Text.Length <= 1500 ? block.Text : block.Text[..1500]);
        }
    }

    // Closed shapes worth a take-off: at least 300 mm round and 0.01 m² in area, not the sheet
    // border or a frame (anything spanning most of the page), duplicates (fill + outline drawn
    // twice) collapsed, largest first.
    private static IEnumerable<DrawingShape> Shapes(DrawingGeometryPage page, double mmPerPoint, List<string> warnings)
    {
        var pageArea = page.WidthPoints * page.HeightPoints;
        var seen = new HashSet<string>();
        var shapes = new List<DrawingShape>();
        foreach (var polygon in page.Polygons)
        {
            var points = polygon.Points;
            var minX = points.Min(point => point[0]); var maxX = points.Max(point => point[0]);
            var minY = points.Min(point => point[1]); var maxY = points.Max(point => point[1]);
            if ((maxX - minX) * (maxY - minY) > pageArea * 0.6) continue;

            var perimeter = 0.0; var twiceArea = 0.0;
            for (var index = 0; index < points.Count; index++)
            {
                var a = points[index]; var b = points[(index + 1) % points.Count];
                perimeter += Math.Sqrt((b[0] - a[0]) * (b[0] - a[0]) + (b[1] - a[1]) * (b[1] - a[1]));
                twiceArea += a[0] * b[1] - b[0] * a[1];
            }
            var perimeterMm = perimeter * mmPerPoint;
            var areaSqM = Math.Abs(twiceArea) / 2 * mmPerPoint * mmPerPoint / 1_000_000;
            if (perimeterMm < 300 || areaSqM < 0.01) continue;

            var key = $"{Math.Round(minX)}:{Math.Round(minY)}:{Math.Round(maxX)}:{Math.Round(maxY)}:{points.Count}";
            if (!seen.Add(key)) continue;

            shapes.Add(new DrawingShape(
                page.Page, points.Count, polygon.IsRectangle, polygon.HasCurves,
                Point((minX + maxX) / 2, (minY + maxY) / 2, mmPerPoint),
                Math.Round((maxX - minX) * mmPerPoint), Math.Round((maxY - minY) * mmPerPoint),
                Math.Round(perimeterMm), Math.Round(areaSqM, 3)));
        }

        shapes.Sort((a, b) => b.AreaSqM.CompareTo(a.AreaSqM));
        if (shapes.Count > ShapeCap)
        {
            warnings.Add($"Page {page.Page}: {shapes.Count} closed shapes found; the {ShapeCap} largest are kept.");
            return shapes.Take(ShapeCap);
        }
        return shapes;
    }

    private static DrawingPoint Point(double x, double y, double mmPerPoint) =>
        new(Math.Round(x * mmPerPoint), Math.Round(y * mmPerPoint));
}

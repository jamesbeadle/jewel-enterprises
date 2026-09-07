using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace Jewel.JPMS.Api.Features.Drawings.Geometry;

/// <summary>
/// Reads a PDF's positioned text and vector geometry with PdfPig into a DrawingGeometry. Pure and
/// deterministic: no OCR, no scale, no interpretation — a scanned sheet comes back with empty
/// lists, and that emptiness is the fact the structured read reports. Rounded to 0.01pt so the
/// blob stays a few megabytes on an A0 sheet with twenty-thousand strokes.
/// </summary>
public static class PdfGeometryExtractor
{
    public static DrawingGeometry Read(byte[] pdfBytes)
    {
        PdfDocument document;
        try
        {
            document = PdfDocument.Open(pdfBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "The PDF could not be opened for geometry extraction — it may be password-protected or corrupted.", ex);
        }

        using (document)
        {
            var pages = new List<DrawingGeometryPage>();
            foreach (var page in document.GetPages())
                pages.Add(ReadPage(page));
            return new DrawingGeometry(pages);
        }
    }

    private static DrawingGeometryPage ReadPage(Page page)
    {
        var words = ReadWords(page);
        var segments = new List<GeometrySegment>();
        var polygons = new List<GeometryPolygon>();
        foreach (var path in ReadPaths(page))
        {
            if (path.IsClipping) continue;
            foreach (var subpath in path)
                ReadSubpath(subpath, path.IsFilled, path.IsStroked, segments, polygons);
        }
        return new DrawingGeometryPage(
            page.Number, Round(page.Width), Round(page.Height), (int)page.Rotation.Value,
            words, segments, polygons);
    }

    private static IReadOnlyList<GeometryWord> ReadWords(Page page)
    {
        IEnumerable<Word> words;
        try { words = page.GetWords(NearestNeighbourWordExtractor.Instance); }
        catch (Exception) { words = page.GetWords(); }

        var result = new List<GeometryWord>();
        foreach (var word in words)
        {
            var text = DrawingTextNormaliser.Normalise(word.Text).Trim();
            if (text.Length == 0) continue;
            // A rotated word's Left/Right/Bottom/Top follow the glyphs, not the page — order
            // them so X0 ≤ X1 and Y0 ≤ Y1 always hold and every reader can rely on it.
            var box = word.BoundingBox;
            result.Add(new GeometryWord(
                text,
                Round(Math.Min(box.Left, box.Right)), Round(Math.Min(box.Bottom, box.Top)),
                Round(Math.Max(box.Left, box.Right)), Round(Math.Max(box.Bottom, box.Top)),
                word.TextOrientation.ToString(), BaselineAngle(word)));
        }
        return result;
    }

    // The angle of the word's baseline from its first letter's start to its last letter's end.
    private static double BaselineAngle(Word word)
    {
        if (word.Letters.Count == 0) return 0;
        var start = word.Letters[0].StartBaseLine;
        var end = word.Letters[^1].EndBaseLine;
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        if (Math.Abs(dx) < 0.01 && Math.Abs(dy) < 0.01) return 0;
        return Math.Round(Math.Atan2(dy, dx) * 180 / Math.PI, 1);
    }

    private static IReadOnlyList<UglyToad.PdfPig.Graphics.PdfPath> ReadPaths(Page page)
    {
        try { return page.ExperimentalAccess.Paths; }
        catch (Exception) { return Array.Empty<UglyToad.PdfPig.Graphics.PdfPath>(); }
    }

    // Every straight stroke becomes a segment; a closed subpath with three or more distinct
    // vertices also becomes a polygon. Curves contribute their end points only.
    private static void ReadSubpath(
        PdfSubpath subpath, bool isFilled, bool isStroked,
        List<GeometrySegment> segments, List<GeometryPolygon> polygons)
    {
        var vertices = new List<double[]>();
        var hasCurves = false;
        PdfPoint? current = null;
        foreach (var command in subpath.Commands)
        {
            switch (command)
            {
                case PdfSubpath.Move move:
                    current = move.Location;
                    AddVertex(vertices, move.Location);
                    break;
                case PdfSubpath.Line line:
                    segments.Add(new GeometrySegment(
                        Round(line.From.X), Round(line.From.Y), Round(line.To.X), Round(line.To.Y)));
                    current = line.To;
                    AddVertex(vertices, line.To);
                    break;
                case PdfSubpath.BezierCurve curve:
                    hasCurves = true;
                    current = curve.EndPoint;
                    AddVertex(vertices, curve.EndPoint);
                    break;
                case PdfSubpath.Close:
                    break;
            }
        }
        _ = current;

        if (!subpath.IsClosed() || vertices.Count < 3) return;
        if (vertices.Count > 1 && SamePoint(vertices[0], vertices[^1])) vertices.RemoveAt(vertices.Count - 1);
        if (vertices.Count < 3) return;
        polygons.Add(new GeometryPolygon(
            vertices, subpath.IsDrawnAsRectangle || IsAxisAlignedQuad(vertices), hasCurves, isFilled, isStroked));
    }

    private static void AddVertex(List<double[]> vertices, PdfPoint point)
    {
        var vertex = new[] { Round(point.X), Round(point.Y) };
        if (vertices.Count > 0 && SamePoint(vertices[^1], vertex)) return;
        vertices.Add(vertex);
    }

    private static bool SamePoint(double[] a, double[] b) =>
        Math.Abs(a[0] - b[0]) < 0.05 && Math.Abs(a[1] - b[1]) < 0.05;

    private static bool IsAxisAlignedQuad(List<double[]> vertices)
    {
        if (vertices.Count != 4) return false;
        for (var index = 0; index < 4; index++)
        {
            var a = vertices[index];
            var b = vertices[(index + 1) % 4];
            var horizontal = Math.Abs(a[1] - b[1]) < 0.05;
            var vertical = Math.Abs(a[0] - b[0]) < 0.05;
            if (!horizontal && !vertical) return false;
        }
        return true;
    }

    private static double Round(double value) => Math.Round(value, 2);
}

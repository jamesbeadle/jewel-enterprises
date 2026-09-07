using System.Globalization;
using System.Text.RegularExpressions;

namespace Jewel.JPMS.Api.Features.Drawings.Geometry;

/// <summary>
/// Establishes a page's scale and makes the sheet prove it. The title block's "1:50 @ A0" is only
/// a claim — drainage falls ("1:60") look identical, and a sheet re-plotted to A3 keeps the old
/// label — so every candidate is tested the same way: each figured dimension on the sheet ("8,445")
/// is looked up as a drawn line of that length at that scale, near the figure. A scale that most
/// of the figures agree with is verified; when nothing is declared the candidate the figures
/// agree with best is inferred. Figures that don't match (radii, levels, callout numbers) are
/// simply not dimensions — they cost nothing.
/// </summary>
public static partial class DrawingScaleCalibrator
{
    private const double PointsPerMm = 72.0 / 25.4;
    private static readonly int[] StandardDenominators = { 1, 2, 5, 10, 20, 25, 50, 100, 200, 250, 500, 1000, 1250, 2500 };

    public sealed record Calibration(
        string? Declared, double? MmPerPoint, int LabelsChecked, int LabelsMatched, bool Verified, bool Inferred,
        IReadOnlyList<DimensionMatch> Matches);

    public sealed record DimensionMatch(GeometryWord Label, int ValueMm, GeometrySegment Segment);

    public static Calibration Calibrate(DrawingGeometryPage page)
    {
        var labels = DimensionLabels(page.Words);
        var declaredDenominators = DeclaredDenominators(page.Words);
        var declared = declaredDenominators.Count > 0 ? $"1:{declaredDenominators[0]}" : null;

        // The declaration is tried first; a verified declaration wins outright.
        foreach (var denominator in declaredDenominators)
        {
            var matches = Match(labels, page.Segments, denominator);
            if (IsVerified(labels.Count, matches.Count))
                return new Calibration($"1:{denominator}", MmPerPoint(denominator), labels.Count, matches.Count, true, false, matches);
        }

        // Otherwise let the figures choose among the usual scales.
        var best = default((int denominator, List<DimensionMatch> matches));
        foreach (var denominator in StandardDenominators)
        {
            if (declaredDenominators.Contains(denominator)) continue;
            var matches = Match(labels, page.Segments, denominator);
            if (matches.Count > (best.matches?.Count ?? 0)) best = (denominator, matches);
        }
        if (best.matches is not null && IsVerified(labels.Count, best.matches.Count))
            return new Calibration(declared, MmPerPoint(best.denominator), labels.Count, best.matches.Count, true, true, best.matches);

        // Nothing verified: keep the declaration (unproven) if there is one, else no scale at all.
        if (declaredDenominators.Count > 0)
        {
            var matches = Match(labels, page.Segments, declaredDenominators[0]);
            return new Calibration(declared, MmPerPoint(declaredDenominators[0]), labels.Count, matches.Count, false, false, matches);
        }
        return new Calibration(null, null, labels.Count, 0, false, false, Array.Empty<DimensionMatch>());
    }

    public static double MmPerPoint(int denominator) => denominator / PointsPerMm;

    // Five agreeing figures, and at least a quarter of them — a sheet with three dimensions can
    // still verify, a sheet whose figures mostly disagree cannot.
    private static bool IsVerified(int checkedCount, int matchedCount) =>
        matchedCount >= 5 && matchedCount * 4 >= checkedCount;

    /// <summary>Figured dimensions: "8,445", "550", "1,200" — 100 mm to 100 m, nothing smaller
    /// (a "15" is a thickness note, not a line worth finding).</summary>
    public static List<(GeometryWord Word, int ValueMm)> DimensionLabels(IReadOnlyList<GeometryWord> words)
    {
        var labels = new List<(GeometryWord, int)>();
        foreach (var word in words)
        {
            if (!DimensionFigure().IsMatch(word.Text)) continue;
            var value = int.Parse(word.Text.Replace(",", ""), CultureInfo.InvariantCulture);
            if (value is >= 100 and <= 100_000) labels.Add((word, value));
        }
        return labels;
    }

    private static List<int> DeclaredDenominators(IReadOnlyList<GeometryWord> words)
    {
        var joined = string.Join(" ", words.Select(word => word.Text));
        var result = new List<int>();
        // "1:50 @ A0" — the title block's own form — outranks any bare "1:N".
        foreach (Match match in ScaleWithSheet().Matches(joined)) Add(result, match.Groups[1].Value);
        foreach (Match match in BareScale().Matches(joined)) Add(result, match.Groups[1].Value);
        return result;

        static void Add(List<int> list, string text)
        {
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                && value is >= 1 and <= 10_000 && !list.Contains(value))
                list.Add(value);
        }
    }

    /// <summary>Pairs each figure with the nearest drawn line of that length (±1%) whose midpoint
    /// sits within 60pt of the figure — dimension text is set on or beside its line's middle.</summary>
    public static List<DimensionMatch> Match(
        List<(GeometryWord Word, int ValueMm)> labels, IReadOnlyList<GeometrySegment> segments, int denominator)
    {
        var mmPerPoint = MmPerPoint(denominator);
        var matches = new List<DimensionMatch>();
        foreach (var (word, value) in labels)
        {
            var wantedPoints = value / mmPerPoint;
            if (wantedPoints < 4) continue;
            var tolerance = wantedPoints * 0.01;
            var labelX = (word.X0 + word.X1) / 2;
            var labelY = (word.Y0 + word.Y1) / 2;
            GeometrySegment? best = null;
            var bestDistance = double.MaxValue;
            foreach (var segment in segments)
            {
                var dx = segment.X2 - segment.X1;
                var dy = segment.Y2 - segment.Y1;
                var length = Math.Sqrt(dx * dx + dy * dy);
                if (Math.Abs(length - wantedPoints) > tolerance) continue;
                var midX = (segment.X1 + segment.X2) / 2;
                var midY = (segment.Y1 + segment.Y2) / 2;
                var distance = Math.Sqrt((midX - labelX) * (midX - labelX) + (midY - labelY) * (midY - labelY));
                if (distance < 60 && distance < bestDistance) { best = segment; bestDistance = distance; }
            }
            if (best is not null) matches.Add(new DimensionMatch(word, value, best));
        }
        return matches;
    }

    [GeneratedRegex(@"^\d{1,3}(,\d{3})+$|^\d{3,6}$")]
    private static partial Regex DimensionFigure();

    [GeneratedRegex(@"\b1\s*:\s*(\d{1,4})\s*@\s*A\d", RegexOptions.IgnoreCase)]
    private static partial Regex ScaleWithSheet();

    [GeneratedRegex(@"\b1\s*:\s*(\d{1,4})\b")]
    private static partial Regex BareScale();
}

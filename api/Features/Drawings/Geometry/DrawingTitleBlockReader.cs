using System.Text.RegularExpressions;

namespace Jewel.JPMS.Api.Features.Drawings.Geometry;

/// <summary>
/// Reads the title block by caption proximity: architects print small upper-case captions
/// ("DRAWING No.", "SCALE", "DATE", "REVISION:") and set each value beneath its caption. The
/// captions that sit together in one corner ARE the title block — a stray "CLIENT" in the notes
/// column is ignored because it is far from the others. A value is the run of text lines that
/// start under a caption's column, stopping at the next caption down or the neighbouring caption
/// to the right. Anything the sheet captions differently comes back null; RawText, the whole
/// title-block region in reading order, is there so a reader can still find it. Never throws.
/// </summary>
public static partial class DrawingTitleBlockReader
{
    private static readonly string[] NumberCaptions = { "DRAWING No.", "DRAWING NO.", "DRAWING NO", "DRAWING NUMBER", "DWG No.", "DWG NO", "DRG No.", "DRG NO", "SHEET No." };
    private static readonly string[] TitleCaptions = { "DRAWING TITLE", "SHEET TITLE", "TITLE" };
    private static readonly string[] RevisionCaptions = { "REVISION:", "REVISION", "REV:", "REV" };
    private static readonly string[] ScaleCaptions = { "SCALE", "SCALE:" };
    private static readonly string[] DateCaptions = { "DATE", "DATE:" };
    private static readonly string[] DrawnCaptions = { "DRAWN BY:", "DRAWN BY", "DRAWN:", "DRAWN" };
    private static readonly string[] JobCaptions = { "JOB", "JOB:", "PROJECT", "PROJECT:" };
    private static readonly string[] ClientCaptions = { "CLIENT", "CLIENT:" };
    // Captions we read nothing from but must know about, because they end a neighbouring field.
    private static readonly string[] LimitCaptions = { "CHECKED BY:", "CHECKED BY", "CHECKED:", "CHECKED", "CHK:", "CHK", "APPROVED BY:", "APPROVED:", "APPROVED", "STATUS:", "STATUS", "PAPER SIZE", "SIZE:", "STAGE:", "STAGE", "PURPOSE OF ISSUE", "ISSUE:", "ISSUE" };
    private static readonly string[][] AllCaptions = { NumberCaptions, TitleCaptions, RevisionCaptions, ScaleCaptions, DateCaptions, DrawnCaptions, JobCaptions, ClientCaptions, LimitCaptions };

    private sealed record Caption(string Text, Box Box);

    public static DrawingTitleBlock Read(IReadOnlyList<GeometryWord> words)
    {
        var captions = TitleBlockCaptions(words);
        if (captions.Count == 0) return new DrawingTitleBlock(null, null, null, null, null, null, null, null, null, "");

        var region = Union(captions.Select(caption => caption.Box)).Expand(60);
        // Rotated text that happens to cross the corner (a note along the north arrow) is not
        // part of the title block, whatever its coordinates say.
        var lines = Lines(words).Where(line => !line.IsRotated).ToList();
        var rawText = string.Join("\n", lines
            .Where(line => region.ContainsMostly(line))
            .OrderByDescending(line => line.Top).ThenBy(line => line.Left)
            .Select(line => line.Text));

        var scaleText = ValueFor(lines, captions, ScaleCaptions, LooksLikeScale);
        var scaleMatch = scaleText is null ? null : ScaleAndSheet().Match(scaleText);
        return new DrawingTitleBlock(
            DrawingNumber: JoinNumber(ValueFor(lines, captions, NumberCaptions)),
            Title: ShortValue(ValueFor(lines, captions, TitleCaptions), 200),
            Revision: ShortValue(ValueFor(lines, captions, RevisionCaptions, LooksLikeRevision), 8),
            Scale: scaleMatch is { Success: true } ? $"1:{scaleMatch.Groups[1].Value}" : ShortValue(scaleText, 16),
            SheetSize: scaleMatch is { Success: true } && scaleMatch.Groups[2].Success ? scaleMatch.Groups[2].Value.ToUpperInvariant() : null,
            Date: ShortValue(ValueFor(lines, captions, DateCaptions, LooksLikeDate), 24),
            DrawnBy: ShortValue(ValueFor(lines, captions, DrawnCaptions), 24),
            Job: ShortValue(ValueFor(lines, captions, JobCaptions), 200),
            Client: ShortValue(ValueFor(lines, captions, ClientCaptions), 200),
            RawText: rawText);
    }

    // Every caption on the sheet, then only the cluster that holds the most different captions:
    // single-linkage within 220pt (a title block is a few inches across).
    private static List<Caption> TitleBlockCaptions(IReadOnlyList<GeometryWord> words)
    {
        var found = new List<Caption>();
        foreach (var group in AllCaptions)
            foreach (var caption in group)
                foreach (var box in FindCaption(words, caption)) found.Add(new Caption(caption, box));
        if (found.Count == 0) return found;

        var clusterOf = Enumerable.Range(0, found.Count).ToArray();
        for (var a = 0; a < found.Count; a++)
            for (var b = a + 1; b < found.Count; b++)
                if (found[a].Box.DistanceTo(found[b].Box) < 220) Merge(clusterOf, a, b);

        var best = Enumerable.Range(0, found.Count)
            .GroupBy(index => Root(clusterOf, index))
            .OrderByDescending(cluster => cluster.Select(index => found[index].Text).Distinct().Count())
            .ThenByDescending(cluster => cluster.Count())
            .First();
        return best.Select(index => found[index]).ToList();

        static int Root(int[] parents, int index) { while (parents[index] != index) index = parents[index]; return index; }
        static void Merge(int[] parents, int a, int b) { parents[Root(parents, a)] = Root(parents, b); }
    }

    // A caption may be one word ("SCALE") or several ("DRAWING No.") set side by side. Captions are
    // matched with their case — "CLIENT" is a caption, "Client" in a sentence is not.
    private static IEnumerable<Box> FindCaption(IReadOnlyList<GeometryWord> words, string caption)
    {
        var parts = caption.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var first in words)
        {
            if (!Same(first.Text, parts[0])) continue;
            var box = Box.Of(first);
            var cursor = first;
            var matched = true;
            for (var part = 1; part < parts.Length; part++)
            {
                var next = words.FirstOrDefault(word =>
                    Same(word.Text, parts[part]) && SameLine(cursor, word)
                    && word.X0 >= cursor.X1 - 1 && word.X0 - cursor.X1 < Height(cursor) * 2);
                if (next is null) { matched = false; break; }
                box = box.Union(Box.Of(next));
                cursor = next;
            }
            if (matched) yield return box;
        }
    }

    // A caption word can recur ("DATE" heads the revision table too), so a field with a
    // recognisable shape only accepts a value of that shape.
    private static string? ValueFor(List<Line> lines, List<Caption> captions, string[] wanted, Func<string, bool>? accept = null)
    {
        foreach (var caption in captions)
        {
            if (!wanted.Contains(caption.Text)) continue;
            var value = ValueBelow(lines, caption.Box, captions);
            if (value is { Length: > 0 } && (accept is null || accept(value))) return value;
        }
        return null;
    }

    // A value is set beside its caption on the same row ("REVISION: N", "JOB Nichols Nymet…") or
    // beneath it ("DATE" over "28/07/2023"), and a long value runs on below either way. So: the
    // lines that start just right of the caption on its row, then the lines that start in the
    // caption's column beneath it — bounded by the next caption to the right and the next one
    // down, which begin the neighbouring fields.
    private static string? ValueBelow(List<Line> lines, Box caption, List<Caption> captions)
    {
        var height = Math.Max(caption.Top - caption.Bottom, 4);
        // The neighbouring caption to the right shares this caption's row (their bands overlap);
        // the next caption down sits anywhere across this caption's column.
        var rightLimit = captions.Select(other => other.Box)
            .Where(box => box.Left > caption.Right && box.Bottom < caption.Top && box.Top > caption.Bottom)
            .Select(box => box.Left).DefaultIfEmpty(caption.Right + height * 40).Min();
        var floor = captions.Select(other => other.Box)
            .Where(box => box.Top < caption.Bottom - height * 0.2 && box.Right > caption.Left - height * 6 && box.Left < rightLimit)
            .Select(box => box.Top).DefaultIfEmpty(caption.Bottom - height * 5).Max();
        floor = Math.Max(floor, caption.Bottom - height * 5);

        bool IsCaption(Line line) => captions.Any(other => other.Box.ContainsMostly(line));
        var beside = lines.Where(line =>
                !IsCaption(line)
                && line.Left >= caption.Right - height * 0.5 && line.Left < rightLimit - height * 0.5
                && (line.Bottom + line.Top) / 2 > caption.Bottom - height * 0.8
                && (line.Bottom + line.Top) / 2 < caption.Top + height * 0.8)
            .OrderBy(line => line.Left).Select(line => line.Text).ToList();
        var below = lines.Where(line =>
                !IsCaption(line)
                && !beside.Contains(line.Text)
                && line.Top <= caption.Bottom + height * 0.4
                && line.Bottom >= floor - 0.5
                && line.Left >= caption.Left - height * 1.5
                && line.Left < rightLimit - height * 0.5)
            .OrderByDescending(line => line.Top).ThenBy(line => line.Left).Select(line => line.Text).ToList();

        var parts = new List<string>();
        if (beside.Count > 0) parts.Add(string.Join(" ", beside));
        parts.AddRange(below);
        return parts.Count == 0 ? null : string.Join("\n", parts).Trim();
    }

    private static bool LooksLikeDate(string value) => DateShape().IsMatch(value);
    private static bool LooksLikeRevision(string value) => value.Trim().Length <= 4 && !value.Contains(' ') && !value.Contains('\n');
    private static bool LooksLikeScale(string value) =>
        value.Contains("1:") || value.Contains("1 :")
        || value.Contains("N.T.S", StringComparison.OrdinalIgnoreCase) || value.Contains("NTS", StringComparison.OrdinalIgnoreCase);

    // "L/1730/" and "110" are one number set in two type sizes.
    private static string? JoinNumber(string? text)
    {
        if (text is null) return null;
        return ShortValue(TrailingSlashGap().Replace(text.Replace("\n", " "), "/"), 64);
    }

    private static string? ShortValue(string? text, int maxLength)
    {
        if (text is null) return null;
        var single = text.Replace("\n", " ").Trim();
        return single.Length <= maxLength ? single : single[..maxLength];
    }

    private static bool Same(string a, string b) => string.Equals(a.TrimEnd(':', '.'), b.TrimEnd(':', '.'), StringComparison.Ordinal);
    private static double Height(GeometryWord word) => Math.Max(word.Y1 - word.Y0, 4);
    private static bool SameLine(GeometryWord a, GeometryWord b) => Math.Abs((a.Y0 + a.Y1) / 2 - (b.Y0 + b.Y1) / 2) < Height(a) * 0.6;
    private static Box Union(IEnumerable<Box> boxes) => boxes.Aggregate((a, b) => a.Union(b));

    private sealed record Line(double Left, double Bottom, double Right, double Top, string Text, bool IsRotated);

    private static List<Line> Lines(IReadOnlyList<GeometryWord> words) =>
        DrawingTextBlocks.Lines(words).Select(line => new Line(line.Left, line.Bottom, line.Right, line.Top, line.Text, line.IsRotated)).ToList();

    private sealed record Box(double Left, double Bottom, double Right, double Top)
    {
        public static Box Of(GeometryWord word) => new(word.X0, word.Y0, word.X1, word.Y1);
        public Box Union(Box other) => new(Math.Min(Left, other.Left), Math.Min(Bottom, other.Bottom), Math.Max(Right, other.Right), Math.Max(Top, other.Top));
        public Box Expand(double by) => new(Left - by, Bottom - by, Right + by, Top + by);
        public bool ContainsMostly(Line line) =>
            (line.Left + line.Right) / 2 >= Left && (line.Left + line.Right) / 2 <= Right
            && (line.Bottom + line.Top) / 2 >= Bottom && (line.Bottom + line.Top) / 2 <= Top;
        public double DistanceTo(Box other)
        {
            var dx = Math.Max(0, Math.Max(other.Left - Right, Left - other.Right));
            var dy = Math.Max(0, Math.Max(other.Bottom - Top, Bottom - other.Top));
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }

    [GeneratedRegex(@"1\s*:\s*(\d{1,4})(?:\s*@\s*(A\d))?", RegexOptions.IgnoreCase)]
    private static partial Regex ScaleAndSheet();

    [GeneratedRegex(@"/\s+(?=\d)")]
    private static partial Regex TrailingSlashGap();

    [GeneratedRegex(@"\b\d{1,2}[./-]\d{1,2}[./-]\d{2,4}\b|\b\d{1,2}\s+[A-Za-z]{3,9}\s+\d{2,4}\b")]
    private static partial Regex DateShape();
}

namespace Jewel.JPMS.Api.Features.Drawings.Geometry;

/// <summary>
/// Groups positioned words into lines and lines into blocks, from the geometry blob alone (so the
/// structured read can be rebuilt without the PDF). A line is words on one baseline with ordinary
/// word gaps; a block is lines stacked at ordinary leading that share a left edge or overlap.
/// Rotated words each stand alone — a vertical dimension figure is its own block. Horizontal is
/// decided by the word's own baseline angle (within three degrees), not PdfPig's orientation
/// label: Archicad sets plan callouts a fraction of a degree off and labels them "Other".
/// Words of clearly different type sizes never share a line — that is what keeps a small caption
/// apart from the value set beside it.
/// </summary>
public static class DrawingTextBlocks
{
    public sealed record Block(double Left, double Bottom, double Right, double Top, string Text, int WordCount);

    public static IReadOnlyList<Block> Group(IReadOnlyList<GeometryWord> words)
    {
        var lines = Lines(words);
        var blocks = new List<List<Line>>();
        foreach (var line in lines.OrderByDescending(line => line.Top).ThenBy(line => line.Left))
        {
            List<Line>? home = null;
            foreach (var block in blocks)
            {
                var last = block[^1];
                var leading = Math.Max(last.Height, line.Height);
                var gap = last.Bottom - line.Top;
                var sameColumn = line.Left < last.Right && line.Right > last.Left
                    && (Math.Abs(line.Left - last.Left) < leading * 2 || line.Left < last.Right - leading);
                if (gap > -leading * 0.4 && gap < leading * 0.9 && sameColumn && Math.Abs(line.Height - last.Height) < leading * 0.6)
                {
                    home = block;
                    break;
                }
            }
            if (home is null) blocks.Add(new List<Line> { line }); else home.Add(line);
        }

        return blocks.Select(block => new Block(
            block.Min(line => line.Left), block.Min(line => line.Bottom),
            block.Max(line => line.Right), block.Max(line => line.Top),
            string.Join(" ", block.Select(line => line.Text)),
            block.Sum(line => line.WordCount))).ToList();
    }

    public sealed record Line(double Left, double Bottom, double Right, double Top, string Text, int WordCount, bool IsRotated)
    {
        public double Height => Math.Max(Top - Bottom, 1);
    }

    /// <summary>Words on one baseline at ordinary word gaps, as the sheet set them.</summary>
    public static List<Line> Lines(IReadOnlyList<GeometryWord> words)
    {
        var horizontal = words.Where(IsHorizontal).OrderBy(word => word.X0).ToList();
        var open = new List<List<GeometryWord>>();
        foreach (var word in horizontal)
        {
            var height = Math.Max(word.Y1 - word.Y0, 1);
            var line = open.FirstOrDefault(candidate =>
            {
                var last = candidate[^1];
                var lastHeight = Math.Max(last.Y1 - last.Y0, 1);
                var sameBaseline = Math.Abs((last.Y0 + last.Y1) / 2 - (word.Y0 + word.Y1) / 2) < Math.Min(height, lastHeight) * 0.6;
                var sameSize = Math.Abs(height - lastHeight) < Math.Max(height, lastHeight) * 0.45;
                var gap = word.X0 - last.X1;
                return sameBaseline && sameSize && gap > -height * 0.3 && gap < Math.Max(height, lastHeight) * 1.6;
            });
            if (line is null) open.Add(new List<GeometryWord> { word }); else line.Add(word);
        }

        var lines = open.Select(ToLine).ToList();
        foreach (var word in words.Where(word => !IsHorizontal(word)))
            lines.Add(new Line(word.X0, word.Y0, word.X1, word.Y1, word.Text, 1, true));
        return lines;
    }

    private static bool IsHorizontal(GeometryWord word) =>
        word.Orientation == "Horizontal" || Math.Abs(word.AngleDegrees) <= 3;

    private static Line ToLine(List<GeometryWord> words) => new(
        words.Min(word => word.X0), words.Min(word => word.Y0), words.Max(word => word.X1), words.Max(word => word.Y1),
        string.Join(" ", words.Select(word => word.Text)), words.Count, false);
}

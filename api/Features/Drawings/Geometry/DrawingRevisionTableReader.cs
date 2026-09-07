using System.Text.RegularExpressions;

namespace Jewel.JPMS.Api.Features.Drawings.Geometry;

/// <summary>
/// Reads the sheet's revision table out of the page text: rows of "N   23-02-2026   MVL   Updated
/// Electrical &amp; Mechanical layouts…", each comment running on for as many lines as it needs. The
/// text layer is used rather than positions because the table is set as running text and reading
/// order keeps a row's continuation lines together. Rows keep the sheet's own order.
/// </summary>
public static partial class DrawingRevisionTableReader
{
    public static IReadOnlyList<DrawingRevisionNote> Read(string pageText)
    {
        var rows = new List<DrawingRevisionNote>();
        if (string.IsNullOrWhiteSpace(pageText)) return rows;

        DrawingRevisionNote? open = null;
        var continuation = new List<string>();
        foreach (var rawLine in pageText.Split('\n'))
        {
            var line = DrawingTextNormaliser.Normalise(rawLine).Trim();
            var match = RevisionRow().Match(line);
            if (match.Success)
            {
                Close(rows, open, continuation);
                open = new DrawingRevisionNote(
                    match.Groups["id"].Value, match.Groups["date"].Value, match.Groups["by"].Value,
                    match.Groups["comment"].Value.Trim());
                continuation.Clear();
                continue;
            }
            if (open is null) continue;
            // A comment continues onto following lines until something that isn't prose — a bare
            // token, a caption, a blank — or another row starts.
            if (line.Length == 0 || LooksLikeCaption(line)) { Close(rows, open, continuation); open = null; continuation.Clear(); continue; }
            continuation.Add(line);
            if (continuation.Count > 8) { Close(rows, open, continuation); open = null; continuation.Clear(); }
        }
        Close(rows, open, continuation);
        return rows;
    }

    public static bool IsRevisionRow(string text) => RevisionRow().IsMatch(text.Trim());

    private static void Close(List<DrawingRevisionNote> rows, DrawingRevisionNote? open, List<string> continuation)
    {
        if (open is null) return;
        var comment = continuation.Count == 0 ? open.Comment : (open.Comment + " " + string.Join(" ", continuation)).Trim();
        rows.Add(open with { Comment = comment.Length <= 2000 ? comment : comment[..2000] });
    }

    private static bool LooksLikeCaption(string line) =>
        line.Length < 3
        || (line.Length <= 24 && line.All(character => char.IsUpper(character) || !char.IsLetter(character)) && line.Any(char.IsLetter))
        || FigureOnly().IsMatch(line);

    // id: a letter or two, a number, or P-numbered preliminaries; date in the usual UK forms;
    // by: initials or a short name; then the comment.
    [GeneratedRegex(@"^(?<id>[A-Z]{1,2}\d{0,2}|P\d{1,2}|\d{1,2})\s{1,}(?<date>\d{1,2}[-./]\d{1,2}[-./]\d{2,4})\s{1,}(?<by>[A-Za-z]{1,6})\s{1,}(?<comment>.+)$")]
    private static partial Regex RevisionRow();

    [GeneratedRegex(@"^[\d,.\s]+$")]
    private static partial Regex FigureOnly();
}

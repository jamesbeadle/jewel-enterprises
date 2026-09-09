using System.Text.Json.Serialization;

namespace Jewel.JPMS.Api.Features.Ai.Sources;

/// <summary>
/// One readable part of a source — a workbook's sheet, a PDF's page, a Word document's body, a
/// text file — as a list of units (rows, lines, paragraphs) the reader pages through and the
/// search reports hits against. Unit numbers are 1-based and, for a sheet, equal the sheet's own
/// row numbers (every row from 1 to the last used row is a unit, blanks included) so "row 12" in
/// a tool result is row 12 in Excel.
/// </summary>
internal sealed class AiSourcePart
{
    public AiSourcePart(string key, string label, string unitName, IReadOnlyList<string> units)
    {
        Key = key;
        Label = label;
        UnitName = unitName;
        Units = units;
        Chars = units.Sum(unit => unit.Length + 1);
    }

    /// <summary>What the model passes to read_source — a sheet's name, "p3", "body", "text".</summary>
    public string Key { get; }
    public string Label { get; }
    /// <summary>"row", "line" or "paragraph" — how the units are described to the model.</summary>
    public string UnitName { get; }
    public IReadOnlyList<string> Units { get; }
    public int Chars { get; }
}

/// <summary>
/// A source opened for reading: its kind, its parts, and — for an image — the bytes the model is
/// shown instead of text. Built by <see cref="AiSourceReader.Load"/> from a file's bytes, whatever
/// medium they came from (a chat upload, an email attachment); everything the tools do with a
/// source happens against this, so a workbook off an email reads exactly like one pasted into the
/// chat.
/// </summary>
internal sealed class AiSourceDocument
{
    public const string Workbook = "workbook";
    public const string Pdf = "pdf";
    public const string WordDocument = "document";
    public const string Text = "text";
    public const string Image = "image";

    /// <summary>Where the text came from: the file's own text layer, or OCR over a scan.</summary>
    public const string ExtractedText = "extracted";
    public const string OcrText = "ocr";

    public AiSourceDocument(string kind, IReadOnlyList<AiSourcePart> parts, string? imageMediaType = null, byte[]? imageBytes = null)
    {
        Kind = kind;
        Parts = parts;
        ImageMediaType = imageMediaType;
        ImageBytes = imageBytes;
    }

    public string Kind { get; }
    public IReadOnlyList<AiSourcePart> Parts { get; private set; }
    public string? ImageMediaType { get; }
    public byte[]? ImageBytes { get; }

    /// <summary>The PDF itself, kept only for a scan — no text layer — so its pages can be
    /// rendered as images and OCR'd (2026-09-09, the accountant's ask). Null for a PDF with text.</summary>
    public byte[]? ScanBytes { get; init; }
    public string TextSource { get; private set; } = ExtractedText;
    /// <summary>Mean word confidence across the OCR'd pages, 0–1; null until OCR has run.</summary>
    public double? OcrConfidence { get; private set; }

    public bool IsImage => Kind == Image;
    public bool IsScan => Kind == Pdf && ScanBytes is not null;
    public bool IsOcr => TextSource == OcrText;

    /// <summary>OCR's answer for a scan: the pages' lines become the parts' units, flagged as OCR.</summary>
    public void TakeOcr(IReadOnlyList<AiSourcePart> pages, double confidence)
    {
        Parts = pages;
        TextSource = OcrText;
        OcrConfidence = confidence;
    }

    public AiSourcePart? Part(string? key) =>
        string.IsNullOrWhiteSpace(key)
            ? null
            : Parts.FirstOrDefault(part => string.Equals(part.Key, key.Trim(), StringComparison.OrdinalIgnoreCase))
              ?? Parts.FirstOrDefault(part => string.Equals(part.Label, key.Trim(), StringComparison.OrdinalIgnoreCase));

    public AiSourceManifest Manifest() =>
        new(Kind,
            Parts.Select(part => new AiSourceManifestPart(part.Key, part.Label, part.UnitName, part.Units.Count, part.Chars)).ToList(),
            Parts.Sum(part => part.Chars),
            IsScan, TextSource, OcrConfidence);
}

/// <summary>The shape of a source without its contents — what is listed, stored on the attachment
/// row, and replayed to the model every hop. Small on purpose.</summary>
internal sealed record AiSourceManifest(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("parts")] IReadOnlyList<AiSourceManifestPart> Parts,
    [property: JsonPropertyName("totalChars")] int TotalChars,
    [property: JsonPropertyName("isScan")] bool IsScan = false,
    [property: JsonPropertyName("textSource")] string TextSource = AiSourceDocument.ExtractedText,
    [property: JsonPropertyName("ocrConfidence")] double? OcrConfidence = null)
{
    /// <summary>The one-line human summary — "3 sheets · 257 rows", "12 pages", "image".</summary>
    public string Summary()
    {
        switch (Kind)
        {
            case AiSourceDocument.Workbook:
            {
                var rows = Parts.Sum(part => part.Units);
                return $"{Parts.Count} sheet{(Parts.Count == 1 ? "" : "s")} · {rows:N0} row{(rows == 1 ? "" : "s")}";
            }
            case AiSourceDocument.Pdf:
                var pages = $"{Parts.Count} page{(Parts.Count == 1 ? "" : "s")}";
                if (!IsScan) return pages;
                return TextSource == AiSourceDocument.OcrText
                    ? $"{pages} · scan read by OCR ({OcrConfidence:P0} confidence)"
                    : $"{pages} · scan, no text — pages read as images";
            case AiSourceDocument.WordDocument:
            {
                var paragraphs = Parts.Sum(part => part.Units);
                return $"{paragraphs:N0} paragraph{(paragraphs == 1 ? "" : "s")}";
            }
            case AiSourceDocument.Image:
                return "image";
            default:
            {
                var lines = Parts.Sum(part => part.Units);
                return $"{lines:N0} line{(lines == 1 ? "" : "s")}";
            }
        }
    }

    /// <summary>The parts as one line for the turn context and the Context row — "Valuation
    /// No.14 · 217 rows, V01 - Levelling compound · 18 rows, …". Names are third-party strings,
    /// so they are «fenced» exactly like an email subject.</summary>
    public string PartsLine(int maxParts = 40)
    {
        if (Kind == AiSourceDocument.Image) return "an image";
        if (Kind == AiSourceDocument.Pdf) return $"{Parts.Count} pages";
        var shown = Parts.Take(maxParts)
            .Select(part => $"«{part.Label}» · {part.Units:N0} {part.UnitName}{(part.Units == 1 ? "" : "s")}");
        var line = string.Join(", ", shown);
        return Parts.Count > maxParts ? $"{line}, … and {Parts.Count - maxParts} more" : line;
    }
}

internal sealed record AiSourceManifestPart(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("unit")] string UnitName,
    [property: JsonPropertyName("units")] int Units,
    [property: JsonPropertyName("chars")] int Chars);

/// <summary>Where a read stopped and where the next one should start.</summary>
internal sealed record AiSourcePosition(string Part, int From);

internal sealed record AiSourceReadResult(
    string Text,
    /// <summary>The part the read started in.</summary>
    string PartKey,
    string PartLabel,
    int FromUnit,
    int ToUnit,
    /// <summary>True when the read reached the end of the last part it touched.</summary>
    bool ReachedEnd,
    /// <summary>Where to continue, or null when there is nothing after <see cref="ToUnit"/>.</summary>
    AiSourcePosition? Next);

internal sealed record AiSourceHit(string Part, string PartLabel, int Unit, string Text);

internal sealed record AiSourceSearchResult(
    IReadOnlyList<AiSourceHit> Hits,
    /// <summary>Parts whose NAME matches — a sheet called "V01 - Levelling compound" for "V01".</summary>
    IReadOnlyList<AiSourceManifestPart> PartsByName,
    int TotalHits);

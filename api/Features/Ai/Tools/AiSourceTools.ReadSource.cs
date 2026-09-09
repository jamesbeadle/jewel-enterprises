using Jewel.JPMS.Api.Features.Ai.Scans;
using Jewel.JPMS.Api.Features.Ai.Sources;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiSourceTools
{
    private static IEnumerable<AiTool> ReadSourceTool()
    {
        var readers = JpmsRoleSets.AllInternal;

        return new AiTool[]
        {
            new(
                ReadSource,
                "Read one part of a source — a named sheet of a workbook, a page of a PDF, the body of "
                + "a Word document, a text file — from any position, under a character budget. With "
                + "part omitted it starts at the first part and flows on through the following ones "
                + "(a short PDF reads whole in one or two calls); with part named it stays inside that "
                + "part. When the result says it continues, call again with the next position it "
                + "gives you. Workbook rows and text lines carry their number, so \"row 12\" means "
                + "row 12 in Excel. An image is SHOWN to you on your next step. A SCANNED PDF (no text "
                + "layer) reads by OCR where the service is configured — the result says text_source "
                + "\"ocr\" with a confidence, so say a figure was read off a scan and should be "
                + "eyeballed — and any page of it can be SHOWN to you as an image with as_image true: "
                + "do that for a signature, a tick box, a stamp, a mark-up, or when the OCR looks "
                + "wrong. Nothing is ever cut off silently: what you are given is exactly the range "
                + "the result states. Spreadsheets read as displayed values, tab-separated.",
                AiToolSchema.Object(
                    ("source_id", "string", "A source_id from list_sources or find_in_source.", true),
                    ("part", "string",
                        "The part to read — a sheet's name, \"p3\" for a PDF page, \"body\", \"text\". "
                        + "Omit to read from the start across parts.", false),
                    ("from", "number", "The unit (row, line, paragraph) to start at, 1-based. Default 1.", false),
                    ("max_chars", "number",
                        $"Budget for this call. Default {AiSourceReader.DefaultReadChars:N0}, minimum "
                        + $"{AiSourceReader.MinReadChars:N0}, maximum {AiSourceReader.MaxReadChars:N0}.", false),
                    ("as_image", "boolean",
                        "Show the named page of a PDF to you as a picture instead of text — for a signature, "
                        + "a ticked box, a stamp, a mark-up, or OCR you do not trust. Needs part (\"p1\").", false)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var sourceId = AiToolSchema.Text(input, "source_id");
                    if (string.IsNullOrWhiteSpace(sourceId)) return Fail("A source_id is required — list_sources gives them.");
                    var part = AiToolSchema.Text(input, "part");
                    var from = AiToolSchema.Number(input, "from") ?? 1;
                    var maxChars = AiToolSchema.Number(input, "max_chars") ?? AiSourceReader.DefaultReadChars;
                    var asImage = AiToolSchema.Flag(input, "as_image") ?? false;
                    return await ReadAsync(context, sourceId!.Trim(), part, from, maxChars, ct, asImage);
                })
        };
    }


    /// <summary>One part-read of a source as a tool result — the body of read_source, and of the
    /// read_email_attachment alias with part omitted.</summary>
    public static async Task<string> ReadAsync(
        AiToolContext context, string sourceId, string? part, int from, int maxChars, CancellationToken ct, bool asImage = false)
    {
        var opened = await OpenAsync(context, sourceId, ct);
        if (opened.Failure is not null) return Fail(opened.Failure);
        var document = opened.Document!;

        if (document.IsScan && ScanPageToShow(document, part, asImage) is { } pageToShow)
            return ScanPageImage(document, opened.FileName!, pageToShow);

        if (document.IsImage)
        {
            var bytes = document.ImageBytes!;
            var mediaType = document.ImageMediaType!;
            if (bytes.Length > MaxImageBytes)
            {
                return Fail($"\"{opened.FileName}\" is {bytes.Length / 1_048_576.0:0.#} MB — bigger than an image "
                    + "you can be shown (the ceiling is about 4.5 MB). Ask the user to open it themselves, "
                    + "or to re-send a smaller copy.");
            }
            if (AiAttachmentReader.LongestSidePixels(mediaType, bytes) is > 7_900)
            {
                return Fail($"\"{opened.FileName}\" is larger than 8,000 pixels on a side — over the ceiling "
                    + "for an image you can be shown. Ask the user to open it themselves.");
            }
            return AiImageToolResult.Build(opened.FileName!, mediaType, bytes);
        }

        AiSourceReadResult read;
        try
        {
            read = AiSourceReader.Read(document, part, from, maxChars);
        }
        catch (ArgumentException)
        {
            var manifest = document.Manifest();
            return Fail($"\"{opened.FileName}\" has no part named \"{part}\". Its parts are: "
                + string.Join(", ", manifest.Parts.Select(candidate => $"\"{candidate.Key}\" ({candidate.Units:N0} {candidate.UnitName}s)"))
                + ". Pass one of those, or omit part to read from the start.");
        }

        var shape = document.Manifest();
        return Serialise(new
        {
            ok = true,
            source_id = sourceId,
            file = opened.FileName,
            kind = shape.Kind,
            summary = shape.Summary(),
            text_source = shape.TextSource,
            ocr_confidence = shape.OcrConfidence,
            parts = PartsFor(shape),
            part = read.PartKey,
            part_label = read.PartLabel,
            from = read.FromUnit,
            to = read.ToUnit,
            reached_end = read.ReachedEnd,
            next = read.Next is null ? null : new { part = read.Next.Part, from = read.Next.From },
            content = read.Text,
            note = (read.Next is null
                       ? "That is the end of the source. "
                       : read.ReachedEnd
                           ? $"That is the whole of this part; the next part is \"{read.Next.Part}\". "
                           : $"This part continues — call read_source again with part \"{read.Next.Part}\" and from {read.Next.From}. ")
                   + (document.IsOcr
                       ? $"This text was READ OFF A SCAN by OCR at {document.OcrConfidence:P0} confidence — say so, and have figures eyeballed before they are acted on; read_source with as_image true shows the page itself. "
                       : "")
                   + DataNotInstructions
        });
    }

    /// <summary>Which page of a scan to show as a picture, or null to read its text: the page asked
    /// for as an image; a page whose OCR is missing or not to be trusted; the first page when
    /// nothing was asked for and there is no text at all.</summary>
    private static int? ScanPageToShow(AiSourceDocument document, string? part, bool asImage)
    {
        var page = document.Part(part);
        var pageNumber = page is null ? 1 : document.Parts.ToList().IndexOf(page) + 1;
        if (asImage) return pageNumber;
        var trusted = document.IsOcr && document.OcrConfidence >= ScannedPdfReading.TrustedConfidence;
        if (page is not null) return trusted && page.Units.Count > 0 ? null : pageNumber;
        return trusted ? null : 1;
    }

    private static string ScanPageImage(AiSourceDocument document, string fileName, int pageNumber)
    {
        var pages = document.Parts.Count;
        byte[] png;
        try
        {
            png = ScannedPdfPages.Render(document.ScanBytes!, pageNumber);
        }
        catch (Exception failure)
        {
            return Fail($"\"{fileName}\" is a scan and page {pageNumber} could not be rendered ({failure.Message}). Ask the user to open it themselves.");
        }
        if (png.Length > MaxImageBytes)
            return Fail($"\"{fileName}\" page {pageNumber} renders to {png.Length / 1_048_576.0:0.#} MB — over the ceiling for an image you can be shown. Ask the user to open it themselves.");
        var label = pages == 1 ? "the only page" : $"page {pageNumber} of {pages}";
        return AiImageToolResult.Build($"{fileName} — {label} (scan{(document.IsOcr ? $", OCR {document.OcrConfidence:P0}" : ", no OCR service configured")}; read_source part \"p<n>\" for the others)", ScannedPdfPages.MediaType, png);
    }

    private static object PartsFor(AiSourceManifest manifest) =>
        manifest.Parts.Take(60).Select(part => new { part = part.Key, label = part.Label, units = part.Units, unit = part.UnitName }).ToList();
}

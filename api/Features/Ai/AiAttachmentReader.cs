namespace Jewel.JPMS.Api.Features.Ai;

/// <summary>
/// The attachment helpers shared by the chat's uploads and the email readers: which formats are
/// supported, image validation (magic bytes, dimensions, media types) and a flat
/// <see cref="Extract"/> for callers that want one read from the start. The parsing itself —
/// spreadsheets sheet by sheet, PDFs page by page, Word documents, text — lives in
/// <see cref="Sources.AiSourceReader"/>, which is also where a file is read PART by part
/// (docs/ai/06-context-retrieval.md). What genuinely cannot be read — a password-protected PDF, a
/// scan with no text layer, a legacy .doc/.xls — is refused with a plain sentence rather than
/// half-read (ADR-007: declared, not hidden).
/// </summary>
internal static class AiAttachmentReader
{
    /// <summary>Raw upload ceiling. A tracker workbook is tens of KB; this is generous headroom
    /// while staying far inside the gateway's request budget once base64-inflated.</summary>
    public const int MaxBytes = 10 * 1024 * 1024;

    /// <summary>Ceiling on a flat <see cref="Extract"/>. Part-by-part reads through
    /// AiSourceReader are budgeted per call instead and never lose a later sheet or page.</summary>
    public const int MaxChars = 25_000;

    private static readonly string[] SupportedExtensions =
        { ".xlsx", ".csv", ".tsv", ".txt", ".pdf", ".docx" };

    /// <summary>
    /// Image formats reach the model as IMAGE blocks, not extracted text — the one attachment kind
    /// where the bytes themselves are what gets persisted (base64, on the Context row) and replayed.
    /// The list is exactly what the Anthropic Messages API accepts as image media types.
    /// </summary>
    private static readonly Dictionary<string, string> ImageMediaTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp"
        };

    public static bool IsSupported(string fileName) =>
        SupportedExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant())
        || IsImage(fileName);

    /// <summary>The media type a chat upload is stored under — the image's own, the Office /
    /// PDF type for documents, text for the rest.</summary>
    public static string StoredContentType(string fileName) =>
        IsImage(fileName) ? ImageMediaType(fileName) : Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".csv" => "text/csv",
            ".tsv" => "text/tab-separated-values",
            _ => "text/plain"
        };

    public static bool IsImage(string fileName) =>
        ImageMediaTypes.ContainsKey(Path.GetExtension(fileName));

    public static string ImageMediaType(string fileName) =>
        ImageMediaTypes[Path.GetExtension(fileName)];

    public static string SupportedList =>
        "xlsx, csv, tsv, txt, pdf, docx — or an image (png, jpg, gif, webp)";

    /// <summary>
    /// Checks an image upload actually IS the image its name claims (magic bytes, not extension —
    /// a renamed .exe must not ride into the transcript as a "png") and returns the one-line human
    /// summary for the pill. Throws <see cref="InvalidDataException"/> when the bytes don't match.
    /// </summary>
    public static string ValidateImage(string fileName, byte[] content)
    {
        var mediaType = ImageMediaType(fileName);
        if (!LooksLike(mediaType, content))
            throw new InvalidDataException(
                $"\"{fileName}\" doesn't look like a real {Path.GetExtension(fileName).TrimStart('.')} image — re-save it and attach again.");

        return content.Length >= 1_048_576
            ? $"image · {content.Length / 1_048_576.0:0.#} MB"
            : $"image · {Math.Max(1, content.Length / 1024)} KB";
    }

    /// <summary>The magic-byte check itself, shared with read_email_attachment — an email image
    /// is validated by its bytes exactly like a pasted one.</summary>
    public static bool LooksLike(string mediaType, byte[] content) => mediaType switch
    {
        "image/png" => StartsWith(content, 0x89, 0x50, 0x4E, 0x47),
        "image/jpeg" => StartsWith(content, 0xFF, 0xD8, 0xFF),
        "image/gif" => StartsWith(content, 0x47, 0x49, 0x46, 0x38),
        "image/webp" => StartsWith(content, 0x52, 0x49, 0x46, 0x46)
                        && content.Length > 11
                        && content[8] == 0x57 && content[9] == 0x45
                        && content[10] == 0x42 && content[11] == 0x50,
        _ => false
    };

    /// <summary>The media type for an EMAIL attachment: the wire content type when it is one the
    /// model can be shown, else the file name's extension; null means "not a showable image"
    /// (svg included — it is markup, and falls through to the text path).</summary>
    public static string? EmailImageMediaType(string name, string? contentType)
    {
        var type = contentType?.ToLowerInvariant().Trim();
        if (type is "image/png" or "image/jpeg" or "image/gif" or "image/webp") return type;
        if (type is "image/jpg" or "image/pjpeg") return "image/jpeg";
        return ImageMediaTypes.TryGetValue(Path.GetExtension(name), out var byName) ? byName : null;
    }

    /// <summary>
    /// The image's longest side in pixels, read straight from the header (png, jpeg) — the
    /// Anthropic API rejects anything over 8,000px, and finding that out mid-turn wastes the
    /// hop. Null when the format's header is not sniffed (gif, webp) or unreadable.
    /// </summary>
    public static int? LongestSidePixels(string mediaType, byte[] content)
    {
        try
        {
            return mediaType switch
            {
                "image/png" when content.Length >= 24 =>
                    Math.Max(ReadBigEndian(content, 16), ReadBigEndian(content, 20)),
                "image/jpeg" => JpegLongestSide(content),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static int ReadBigEndian(byte[] content, int offset) =>
        (content[offset] << 24) | (content[offset + 1] << 16) | (content[offset + 2] << 8) | content[offset + 3];

    /// <summary>Walks the JPEG marker chain to the first frame header (SOFn) and reads its
    /// dimensions. Null when no frame header is found before the data ends.</summary>
    private static int? JpegLongestSide(byte[] content)
    {
        var index = 2;
        while (index + 9 < content.Length)
        {
            if (content[index] != 0xFF) { index++; continue; }
            var marker = content[index + 1];
            if (marker is 0xD8 or 0x01 or (>= 0xD0 and <= 0xD7)) { index += 2; continue; }
            var length = (content[index + 2] << 8) | content[index + 3];
            if (length < 2) return null;
            if (marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC)
            {
                var height = (content[index + 5] << 8) | content[index + 6];
                var width = (content[index + 7] << 8) | content[index + 8];
                return Math.Max(height, width);
            }
            index += 2 + length;
        }
        return null;
    }

    private static bool StartsWith(byte[] content, params byte[] signature)
    {
        if (content.Length < signature.Length) return false;
        for (var i = 0; i < signature.Length; i++)
            if (content[i] != signature[i]) return false;
        return true;
    }

    /// <summary>
    /// Extracts <paramref name="content"/> to prompt-ready text plus a one-line human summary —
    /// the whole file from the start, under <see cref="MaxChars"/>. Kept for callers that want
    /// one flat read; it delegates to <see cref="Sources.AiSourceReader"/>, which is where the
    /// parsing lives and where part-by-part reading (a named sheet, a page range) happens.
    /// Throws <see cref="NotSupportedException"/> for a format outside the list and
    /// <see cref="InvalidDataException"/> when the file cannot be read as what it claims to be.
    /// </summary>
    public static (string Text, string Summary) Extract(string fileName, byte[] content)
    {
        var document = Sources.AiSourceReader.Load(fileName, null, content);
        if (document.IsImage)
        {
            // A flat text read has nothing to say about a picture; callers that can SHOW an
            // image (read_source) never come through here, and the tender extractor lists it
            // as unreadable exactly as it did before.
            throw new NotSupportedException($"\"{fileName}\" is an image — it has no text to extract.");
        }
        if (document.IsScan)
        {
            throw new InvalidDataException(
                $"\"{fileName}\" is a scan with no text layer. The assistant's read_source and "
                + "read_email_attachment read scans (OCR where configured, else page by page as images); a "
                + "flat text extract cannot.");
        }
        var manifest = document.Manifest();
        var read = Sources.AiSourceReader.Read(document, null, 1, MaxChars);
        var truncated = read.Next is not null;
        var text = truncated
            ? read.Text + $"\n[… the file was larger than {MaxChars:N0} characters and has been cut here.]"
            : read.Text;
        return (text, manifest.Summary() + (truncated ? " (truncated)" : ""));
    }
}

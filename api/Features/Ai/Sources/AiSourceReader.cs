namespace Jewel.JPMS.Api.Features.Ai.Sources;

/// <summary>
/// The ONE place a file becomes something the assistant can read — whatever medium it came from.
/// <see cref="Load"/> opens the bytes into an <see cref="AiSourceDocument"/> of parts and units;
/// <see cref="Read"/> pages through it from any position under a character budget; <see cref="Search"/>
/// finds where a reference appears. Spreadsheets become one part per sheet (displayed values,
/// tab-separated, every row from 1 to the last used row so unit numbers ARE row numbers), PDFs one
/// part per page (reading order, PdfPig), Word documents one body part of paragraphs and table
/// rows (the docx zip read directly), text files one part of lines (BOM-aware; HTML flattened),
/// images one part carrying the bytes the model is shown. What genuinely cannot be read — a
/// password-protected PDF, a scan with no text layer, a legacy .doc/.xls — throws the honest
/// sentence rather than returning half a document.
///
/// <para>Replaces the extract-once-and-cap approach (AiAttachmentReader.Extract, which now
/// delegates here): a 25,000-character cap over a whole workbook meant the first sheet ate the
/// budget and every later tab was silently never extracted — the V01 failure of 2026-08-25. Now
/// no file is unreadable, only long, and the model reads the part it needs.</para>
/// </summary>
internal static partial class AiSourceReader
{
    /// <summary>Per-call ceiling on a read: the result replays in the transcript (latest copy
    /// only) and one part is normally far smaller than this.</summary>
    public const int DefaultReadChars = 20_000;
    public const int MinReadChars = 2_000;
    public const int MaxReadChars = 50_000;

    /// <summary>How much of the first part rides on the Context row as a preview — enough to see
    /// what a file is, small enough to replay every hop for nothing.</summary>
    public const int PreviewChars = 2_000;

    private static readonly string[] TextExtensions =
        { ".txt", ".csv", ".tsv", ".md", ".json", ".xml", ".htm", ".html", ".eml", ".log" };

    private static readonly string[] SpreadsheetExtensions = { ".xlsx", ".xlsm" };

    public static bool IsSupported(string fileName, string? contentType = null) =>
        AiAttachmentReader.IsImage(fileName)
        || IsSpreadsheet(fileName, contentType)
        || IsTextLike(fileName, contentType)
        || Path.GetExtension(fileName).ToLowerInvariant() is ".pdf" or ".docx";

    /// <summary>Opens the bytes as the document they are, routed on extension with the wire
    /// content type as a second opinion (an email's "spreadsheetml" attachment with an odd name
    /// still opens as a workbook). Throws <see cref="NotSupportedException"/> for a format outside
    /// the list and <see cref="InvalidDataException"/> when the file cannot be read as what it
    /// claims to be.</summary>
    public static AiSourceDocument Load(string fileName, string? contentType, byte[] content)
    {
        if (AiAttachmentReader.EmailImageMediaType(fileName, contentType) is { } imageMediaType)
        {
            if (!AiAttachmentReader.LooksLike(imageMediaType, content))
                throw new InvalidDataException($"\"{fileName}\" does not look like a real {imageMediaType} image.");
            return new AiSourceDocument(AiSourceDocument.Image,
                new[] { new AiSourcePart("image", Path.GetFileName(fileName), "image", Array.Empty<string>()) },
                imageMediaType, content);
        }

        if (IsSpreadsheet(fileName, contentType)) return LoadWorkbook(content);

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension == ".pdf" || contentType?.Contains("pdf", StringComparison.OrdinalIgnoreCase) == true)
            return LoadPdf(content);
        if (extension == ".docx" || contentType?.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase) == true)
            return LoadDocx(content);
        if (IsTextLike(fileName, contentType))
            return LoadText(content, LooksLikeHtml(fileName, contentType));

        throw new NotSupportedException(RefusalFor(fileName, extension));
    }

    /// <summary>Name the format, say why, name the route that works (2026-09-09, the accountant's
    /// item C) — never a bare "unsupported".</summary>
    private static string RefusalFor(string fileName, string extension) => extension switch
    {
        ".xls" => $"\"{fileName}\" is a legacy binary Excel file (.xls), which the reader cannot open — it reads "
                  + ".xlsx/.xlsm. Ask the user to open it in Excel, Save As .xlsx and re-attach it, or to upload it into the chat.",
        ".doc" => $"\"{fileName}\" is a legacy binary Word file (.doc), which the reader cannot open — it reads .docx. "
                  + "Ask the user to open it in Word, Save As .docx and re-attach it, or to upload it into the chat.",
        ".msg" => $"\"{fileName}\" is an Outlook message file (.msg) — open the email it wraps in the mailbox instead.",
        ".zip" => $"\"{fileName}\" is a zip archive — ask the user which file inside it holds the answer and to attach that file.",
        _ => $"\"{fileName}\" ({(extension.Length == 0 ? "no extension" : extension)}) is not a format the reader opens — it reads {AiAttachmentReader.SupportedList}. "
             + "Ask the user to re-save it as one of those, or to upload it into the chat."
    };

    public static bool IsSpreadsheet(string name, string? contentType)
    {
        if (contentType?.Contains("spreadsheetml", StringComparison.OrdinalIgnoreCase) == true) return true;
        return SpreadsheetExtensions.Contains(Path.GetExtension(name).ToLowerInvariant());
    }

    public static bool IsTextLike(string name, string? contentType)
    {
        if (contentType is not null)
        {
            var type = contentType.ToLowerInvariant();
            if (type.StartsWith("text/", StringComparison.Ordinal)) return true;
            if (type.Contains("json") || type.Contains("xml") || type.Contains("csv")) return true;
        }
        return TextExtensions.Contains(Path.GetExtension(name).ToLowerInvariant());
    }

    private static bool LooksLikeHtml(string name, string? contentType)
    {
        if (contentType?.Contains("html", StringComparison.OrdinalIgnoreCase) == true) return true;
        return Path.GetExtension(name).ToLowerInvariant() is ".htm" or ".html";
    }
}

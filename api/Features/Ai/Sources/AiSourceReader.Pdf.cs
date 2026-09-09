using System.Text;

namespace Jewel.JPMS.Api.Features.Ai.Sources;

internal static partial class AiSourceReader
{
    private static AiSourceDocument LoadPdf(byte[] content)
    {
        UglyToad.PdfPig.PdfDocument document;
        try
        {
            document = UglyToad.PdfPig.PdfDocument.Open(content);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException(
                "That PDF could not be opened — it may be password-protected or corrupted.", ex);
        }

        using (document)
        {
            var parts = new List<AiSourcePart>();
            var anyText = false;
            foreach (var page in document.GetPages())
            {
                string pageText;
                try
                {
                    // Reading order beats raw content-stream order — a two-column spec sheet
                    // read stream-wise interleaves the columns into nonsense.
                    pageText = UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor
                        .ContentOrderTextExtractor.GetText(page);
                }
                catch (Exception)
                {
                    pageText = page.Text;
                }

                var lines = SplitLines(pageText);
                if (lines.Count > 0) anyText = true;
                parts.Add(new AiSourcePart($"p{page.Number}", $"Page {page.Number}", "line", lines));
            }

            // A scan (2026-09-09, the accountant's ask): no text layer is not a dead end. The
            // document keeps its bytes so each page can be rendered and shown as an image, and
            // OCR'd where Azure AI Vision is configured (ScannedPdfReading fills the parts).
            if (!anyText)
                return new AiSourceDocument(AiSourceDocument.Pdf, parts) { ScanBytes = content };

            return new AiSourceDocument(AiSourceDocument.Pdf, parts);
        }
    }
}

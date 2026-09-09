using Jewel.JPMS.Api.Features.Ai.Scans;
using Jewel.JPMS.Api.Features.Ai.Sources;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// Reading scans (2026-09-09, the accountant's ask): a PDF with no text layer is kept, not
/// refused; its pages render to real PNGs; and the refusals that remain name the format and the
/// route that works.
/// </summary>
public sealed class ScannedPdfReadingTests
{
    // A one-page PDF with no text at all — the shape of a scan, without the picture.
    private static readonly byte[] BlankPagePdf = System.Text.Encoding.ASCII.GetBytes(
        "%PDF-1.4\n" +
        "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n" +
        "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj\n" +
        "3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 200 100] >> endobj\n" +
        "xref\n0 4\n0000000000 65535 f \n0000000009 00000 n \n0000000058 00000 n \n0000000115 00000 n \n" +
        "trailer << /Size 4 /Root 1 0 R >>\nstartxref\n185\n%%EOF\n");

    [Fact]
    public void APdfWithNoTextLayerIsKeptAsAScanNotRefused()
    {
        var document = AiSourceReader.Load("certificate.pdf", "application/pdf", BlankPagePdf);

        Assert.True(document.IsScan);
        Assert.False(document.IsOcr);
        Assert.Equal(AiSourceDocument.Pdf, document.Kind);
        Assert.Single(document.Parts);
        Assert.Contains("scan, no text", document.Manifest().Summary());
    }

    [Fact]
    public void AScannedPageRendersToAPng()
    {
        var png = ScannedPdfPages.Render(BlankPagePdf, 1);

        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, png.Take(4));
        Assert.Equal(1, ScannedPdfPages.PageCount(BlankPagePdf));
    }

    [Fact]
    public void OcrLinesBecomeThePagesUnitsFlaggedAsOcr()
    {
        var document = AiSourceReader.Load("certificate.pdf", "application/pdf", BlankPagePdf);

        document.TakeOcr(new[] { new AiSourcePart("p1", "Page 1", "line", new[] { "Net for payment £13,703.94" }) }, 0.97);

        Assert.True(document.IsOcr);
        Assert.Equal(0.97, document.OcrConfidence);
        Assert.Equal("Net for payment £13,703.94", AiSourceReader.Read(document, "p1", 1, 2000).Text.Split('\n').Last());
        Assert.Contains("read by OCR (97% confidence)", document.Manifest().Summary());
    }

    [Theory]
    [InlineData("Windyridge Valuation 09 - Final Claim.xls", "legacy binary Excel", "Save As .xlsx")]
    [InlineData("old-contract.doc", "legacy binary Word", "Save As .docx")]
    public void ARefusalNamesTheFormatAndTheRouteThatWorks(string fileName, string format, string route)
    {
        var refusal = Assert.Throws<NotSupportedException>(() => AiSourceReader.Load(fileName, null, new byte[] { 1, 2, 3 }));

        Assert.Contains(format, refusal.Message);
        Assert.Contains(route, refusal.Message);
    }
}

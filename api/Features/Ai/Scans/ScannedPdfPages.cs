using Docnet.Core;
using Docnet.Core.Models;

namespace Jewel.JPMS.Api.Features.Ai.Scans;

/// <summary>
/// Renders one page of a PDF to a PNG the assistant can be shown — the route through a scan that
/// has no text layer (2026-09-09, the accountant's ask). pdfium through Docnet; 150 dpi, which
/// reads a typed certificate cleanly and keeps an A4 page well inside the image ceiling.
/// </summary>
public static class ScannedPdfPages
{
    /// <summary>pdfium's 72 dpi base scaled to ~150 dpi.</summary>
    private const double Scale = 150.0 / 72.0;

    public const string MediaType = "image/png";

    public static int PageCount(byte[] pdf)
    {
        using var reader = DocLib.Instance.GetDocReader(pdf, new PageDimensions(Scale));
        return reader.GetPageCount();
    }

    /// <summary>The page as a PNG; pages are 1-based as the reader labels them ("p3").</summary>
    public static byte[] Render(byte[] pdf, int pageNumber)
    {
        using var reader = DocLib.Instance.GetDocReader(pdf, new PageDimensions(Scale));
        if (pageNumber < 1 || pageNumber > reader.GetPageCount())
            throw new ArgumentOutOfRangeException(nameof(pageNumber), $"The PDF has {reader.GetPageCount()} pages.");
        using var page = reader.GetPageReader(pageNumber - 1);
        return PngEncoder.FromBgra(page.GetImage(), page.GetPageWidth(), page.GetPageHeight());
    }
}

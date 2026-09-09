namespace Jewel.JPMS.Api.Features.Ai.Scans;

/// <summary>One page as OCR read it: its lines in reading order and the mean word confidence, 0–1.</summary>
public sealed record OcrPage(IReadOnlyList<string> Lines, double Confidence);

/// <summary>
/// Reads the text off one page image. The reader behind a scanned PDF (2026-09-09, the
/// accountant's ask): typed forms — certificates, contracts — read cleanly; the assistant is told
/// the text came from OCR and at what confidence, and can ask for the page image instead.
/// </summary>
public interface IDocumentOcr
{
    bool IsConfigured { get; }
    string Provider { get; }
    Task<OcrPage> ReadAsync(byte[] pngImage, CancellationToken cancellationToken);
}

/// <summary>The stand-in when no OCR service is configured: scans are read as page images only.</summary>
public sealed class NullDocumentOcr : IDocumentOcr
{
    public bool IsConfigured => false;
    public string Provider => "none";
    public Task<OcrPage> ReadAsync(byte[] pngImage, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("No OCR service is configured — set DocumentOcr__Endpoint and DocumentOcr__ApiKey.");
}

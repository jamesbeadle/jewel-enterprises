using System.Security.Cryptography;
using System.Text.Json;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Ai.Sources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Ai.Scans;

/// <summary>
/// Gives a scanned PDF its text (2026-09-09, the accountant's ask): every page rendered and
/// OCR'd, the lines becoming the document's parts flagged as OCR with their confidence, and the
/// result cached by the file's hash so the same certificate is read once. With no OCR service
/// configured the document is left as it is — pages still show as images through read_source.
/// An OCR failure is logged and leaves the document readable as images too; it never fails the
/// tool call.
/// </summary>
internal static class ScannedPdfReading
{
    /// <summary>Below this, the assistant is shown the page rather than trusted with the text.</summary>
    public const double TrustedConfidence = 0.6;

    private static readonly JsonSerializerOptions Json = new();

    public static async Task FillAsync(AiSourceDocument document, IServiceProvider services, CancellationToken cancellationToken)
    {
        if (!document.IsScan) return;
        var ocr = services.GetRequiredService<IDocumentOcr>();
        if (!ocr.IsConfigured) return;

        var context = services.GetRequiredService<JpmsContext>();
        var hash = Sha256Of(document.ScanBytes!);
        var cached = await context.DocumentOcrResults.FindAsync(new object[] { hash }, cancellationToken);
        if (cached is not null)
        {
            document.TakeOcr(PartsOf(JsonSerializer.Deserialize<List<List<string>>>(cached.PagesJson, Json) ?? new()), cached.Confidence);
            return;
        }

        try
        {
            var pages = await ReadEveryPageAsync(document.ScanBytes!, ocr, cancellationToken);
            var confidence = pages.Count == 0 ? 0 : pages.Average(page => page.Confidence);
            document.TakeOcr(PartsOf(pages.Select(page => page.Lines.ToList()).ToList()), confidence);
            await RememberAsync(context, hash, pages, confidence, ocr.Provider, cancellationToken);
        }
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(ScannedPdfReading))
                .LogWarning(failure, "OCR of a scanned PDF failed; it stays readable as page images.");
        }
    }

    private static async Task<List<OcrPage>> ReadEveryPageAsync(byte[] pdf, IDocumentOcr ocr, CancellationToken cancellationToken)
    {
        var pages = new List<OcrPage>();
        var count = ScannedPdfPages.PageCount(pdf);
        for (var page = 1; page <= count; page++)
            pages.Add(await ocr.ReadAsync(ScannedPdfPages.Render(pdf, page), cancellationToken));
        return pages;
    }

    private static IReadOnlyList<AiSourcePart> PartsOf(List<List<string>> pages) =>
        pages.Select((lines, index) => new AiSourcePart($"p{index + 1}", $"Page {index + 1}", "line", lines)).ToList();

    private static async Task RememberAsync(JpmsContext context, string hash, List<OcrPage> pages, double confidence, string provider, CancellationToken cancellationToken)
    {
        context.DocumentOcrResults.Add(new DocumentOcrResultEntity
        {
            ContentSha256 = hash,
            PageCount = pages.Count,
            PagesJson = JsonSerializer.Serialize(pages.Select(page => page.Lines).ToList(), Json),
            Confidence = confidence,
            Provider = provider,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    private static string Sha256Of(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}

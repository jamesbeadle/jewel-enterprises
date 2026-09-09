using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Ai.Scans;

/// <summary>Where the OCR service is: <c>DocumentOcr__Endpoint</c> (https://{resource}.cognitiveservices.azure.com)
/// and <c>DocumentOcr__ApiKey</c>. Unset means scans are read as page images only.</summary>
public sealed class DocumentOcrOptions
{
    public const string SectionName = "DocumentOcr";
    public string? Endpoint { get; init; }
    public string? ApiKey { get; init; }
    public string ApiVersion { get; init; } = "2024-02-01";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(ApiKey);

    public static DocumentOcrOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        return new DocumentOcrOptions
        {
            Endpoint = section["Endpoint"]?.TrimEnd('/'),
            ApiKey = section["ApiKey"],
            ApiVersion = string.IsNullOrWhiteSpace(section["ApiVersion"]) ? "2024-02-01" : section["ApiVersion"]!
        };
    }
}

/// <summary>
/// Azure AI Vision's Read (Image Analysis 4.0): one POST per page image, the lines back in
/// reading order with a confidence per word. A typed certificate reads at 0.95+; a signature
/// block or a stamp scores low, which is the assistant's cue to look at the page instead.
/// </summary>
public sealed class AzureVisionOcr : IDocumentOcr
{
    private readonly HttpClient http;
    private readonly DocumentOcrOptions options;

    public AzureVisionOcr(HttpClient http, DocumentOcrOptions options) { this.http = http; this.options = options; }

    public bool IsConfigured => options.IsConfigured;
    public string Provider => "azure-ai-vision-read";

    public async Task<OcrPage> ReadAsync(byte[] pngImage, CancellationToken cancellationToken)
    {
        var url = $"{options.Endpoint}/computervision/imageanalysis:analyze?api-version={options.ApiVersion}&features=read";
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new ByteArrayContent(pngImage) };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        request.Headers.Add("Ocp-Apim-Subscription-Key", options.ApiKey);
        using var response = await http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"The OCR service answered HTTP {(int)response.StatusCode}: {Truncate(body)}");
        return Parse(body);
    }

    private static OcrPage Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var lines = new List<string>();
        var confidences = new List<double>();
        if (document.RootElement.TryGetProperty("readResult", out var readResult)
            && readResult.TryGetProperty("blocks", out var blocks))
            foreach (var block in blocks.EnumerateArray())
                if (block.TryGetProperty("lines", out var lineElements))
                    foreach (var line in lineElements.EnumerateArray())
                        TakeLine(line, lines, confidences);
        return new OcrPage(lines, confidences.Count == 0 ? 0 : confidences.Average());
    }

    private static void TakeLine(JsonElement line, List<string> lines, List<double> confidences)
    {
        var text = line.TryGetProperty("text", out var textElement) ? textElement.GetString() : null;
        if (string.IsNullOrWhiteSpace(text)) return;
        lines.Add(text);
        if (!line.TryGetProperty("words", out var words)) return;
        foreach (var word in words.EnumerateArray())
            if (word.TryGetProperty("confidence", out var confidence) && confidence.TryGetDouble(out var value))
                confidences.Add(value);
    }

    private static string Truncate(string body) => body.Length <= 300 ? body : body[..300] + "…";
}

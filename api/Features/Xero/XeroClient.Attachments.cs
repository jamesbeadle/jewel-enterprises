using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{
    public async Task<IReadOnlyList<XeroInvoiceAttachment>> ListAttachmentsAsync(
        string invoiceId, bool isCreditNote, CancellationToken ct)
    {
        var token = await GetAccessTokenAsync(ct);
        var baseUrl = isCreditNote ? CreditNotesUrl : InvoicesUrl;

        JsonDocument doc;
        try
        {
            doc = await GetJsonAsync(token, $"{baseUrl}/{invoiceId}/Attachments", "attachments", ct);
        }
        catch (XeroCallFailedException failure) when (failure.Message.Contains("HTTP 403"))
        {
            throw new XeroCallFailedException(
                "Couldn't read the invoice's attachments — the Xero custom connection needs the "
                + "accounting.attachments scope ticked in the Xero developer portal. " + failure.Message);
        }

        using (doc)
        {
            if (!doc.RootElement.TryGetProperty("Attachments", out var items)
                || items.ValueKind != JsonValueKind.Array)
                return Array.Empty<XeroInvoiceAttachment>();

            return items.EnumerateArray()
                .Select(item => new XeroInvoiceAttachment(
                    AttachmentId: StringOf(item, "AttachmentID") ?? "",
                    FileName: StringOf(item, "FileName") ?? "attachment",
                    MimeType: StringOf(item, "MimeType") ?? "application/octet-stream",
                    ContentLength: item.TryGetProperty("ContentLength", out var length)
                        && length.ValueKind == JsonValueKind.Number ? length.GetInt64() : 0))
                .Where(attachment => attachment.AttachmentId.Length > 0)
                .ToList();
        }
    }

    public async Task<XeroAttachmentContent?> GetAttachmentAsync(
        string invoiceId, bool isCreditNote, string fileName, CancellationToken ct)
    {
        // The list gives the attachment's real MimeType — Xero's content endpoint wants it
        // in the Accept header — and confirms the file actually belongs to this invoice.
        var attachments = await ListAttachmentsAsync(invoiceId, isCreditNote, ct);
        var attachment = attachments.FirstOrDefault(candidate =>
            candidate.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        if (attachment is null) return null;

        var token = await GetAccessTokenAsync(ct);
        var baseUrl = isCreditNote ? CreditNotesUrl : InvoicesUrl;
        var url = $"{baseUrl}/{invoiceId}/Attachments/{Uri.EscapeDataString(attachment.FileName)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(attachment.MimeType));
        if (!string.IsNullOrWhiteSpace(_options.TenantId))
            request.Headers.Add("xero-tenant-id", _options.TenantId);

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Xero attachment call failed: {Status} {Body}.", (int)response.StatusCode, Truncate(body));
            throw new XeroCallFailedException(
                $"Xero rejected the attachment request with HTTP {(int)response.StatusCode}. {Truncate(body)}");
        }

        var content = await response.Content.ReadAsByteArrayAsync(ct);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? attachment.MimeType;
        return new XeroAttachmentContent(content, contentType, attachment.FileName);
    }

    public async Task<XeroApprovalResult> AttachToInvoiceAsync(
        string invoiceId, string fileName, string contentType, byte[] content, CancellationToken ct)
    {
        if (!_options.IsConfigured) return XeroApprovalResult.Failed(NotConnected);
        var token = await GetAccessTokenAsync(ct);
        var url = $"{InvoicesUrl}/{invoiceId}/Attachments/{Uri.EscapeDataString(fileName)}";
        using var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = new ByteArrayContent(content) };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(_options.TenantId))
            request.Headers.Add("xero-tenant-id", _options.TenantId);

        using var response = await _http.SendAsync(request, ct);
        if (response.IsSuccessStatusCode) return XeroApprovalResult.Ok("ATTACHED");
        var body = await response.Content.ReadAsStringAsync(ct);
        _logger.LogWarning("Xero attach call failed: {Status} {Body}.", (int)response.StatusCode, Truncate(body));
        var reason = (int)response.StatusCode == 403
            ? "the Xero custom connection needs the accounting.attachments scope ticked in the Xero developer portal"
            : ExtractXeroErrors(body);
        return XeroApprovalResult.Failed($"Xero rejected the attachment with HTTP {(int)response.StatusCode} — {reason}");
    }

    /// <summary>
    /// Rebuilds the invoice's full line list for the update: untouched lines pass through
    /// as-is (keyed by LineItemID so Xero updates in place), single-centre lines get their
    /// tracking replaced, and split lines are replaced by one new line per cost centre with
    /// pro-rated amounts. Returns null (with an error) when the invoice no longer matches
    /// the stored allocation — edited amounts or removed lines — because silently approving
    /// figures nobody allocated would corrupt the accounts.
    /// </summary>
}

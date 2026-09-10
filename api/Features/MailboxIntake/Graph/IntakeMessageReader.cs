using System.Net;
using System.Net.Http.Headers;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Graph;

/// <summary>Full on-demand content of one mailbox message: the (raw, unsanitised) HTML or text
/// body plus its real, non-inline attachments. Sanitisation happens in the handler, not here.
/// The envelope fields (From/To/Cc/ReplyTo/Subject) feed the composer's reply prefill; all optional
/// so existing callers are unchanged.</summary>
public sealed record IntakeMessageContent(
    string Body,
    bool IsHtml,
    IReadOnlyList<IntakeMessageAttachment> Attachments,
    string? FromEmail = null,
    string? FromName = null,
    IReadOnlyList<string>? To = null,
    IReadOnlyList<string>? Cc = null,
    string? ReplyTo = null,
    string? Subject = null,
    // Images embedded in the HTML body (a pasted screenshot travels as an isInline attachment the
    // body references by <img src="cid:{ContentId}">). Never shown as files — InboundEmailBodyBuilder
    // uses these to put the pictures back into the rendered body. Null from older callers/caches.
    IReadOnlyList<IntakeInlineImage>? InlineImages = null,
    // Every Outlook category on the message, unfiltered, read in the same direct GET as the body —
    // so a caller that opens an email sees its tags as they are NOW, not as the list page had them.
    // Null from readers that don't select categories.
    IReadOnlyList<string>? Categories = null,
    // Graph fills bccRecipients only on the mailbox's OWN sent copies (a received message never
    // reveals its Bcc), so this is how a sent invite says who it went to. Null from readers that
    // don't select it; empty on an inbound message.
    IReadOnlyList<string>? Bcc = null);

// Id is the Graph attachment id, used to download the attachment's bytes on demand (e.g. saving a
// drawing out of a triaged email). Optional so existing metadata-only callers are unchanged.
public sealed record IntakeMessageAttachment(string Name, long Size, string? ContentType, string Id = "");

/// <summary>One inline body image, known at list time only by its Graph attachment id — the cid the
/// HTML references it by lives on the fileAttachment subtype, which the metadata read cannot select,
/// so it is learned from the full attachment fetch. Size is Graph's reported size, used to respect
/// the embed caps before downloading.</summary>
public sealed record IntakeInlineImage(string AttachmentId, string? ContentType, long Size);

/// <summary>One downloaded attachment: its bytes plus the metadata needed to store it elsewhere.
/// ContentId is set for inline images — the cid the email's HTML references the picture by.</summary>
public sealed record IntakeAttachmentContent(string Name, string ContentType, byte[] Content, string? ContentId = null);

/// <summary>
/// Reads a single mailbox message's full body and attachment metadata from Microsoft Graph, on
/// demand, when a triager opens an email. Deliberately read-only and narrow — the producer/webhook
/// API has no other Graph reach; the background sweep/move/draft paths live in the worker.
/// </summary>
public interface IIntakeMessageReader
{
    /// <summary>Fetch body + attachments for a Graph message id, or null if it can't be retrieved.</summary>
    Task<IntakeMessageContent?> GetAsync(string graphMessageId, CancellationToken ct);

    /// <summary>Download one attachment's bytes: a Graph fileAttachment as-is, or an itemAttachment
    /// (an email attached to the email) as its raw MIME named <c>{subject}.eml</c>. Null if it can't
    /// be retrieved or has no bytes (reference attachments are links, not files).</summary>
    Task<IntakeAttachmentContent?> GetAttachmentAsync(string graphMessageId, string attachmentId, CancellationToken ct);

    /// <summary>The real (non-inline) attachments on a message — names, sizes and ids only, no
    /// body, no bytes. Cheaper than <see cref="GetAsync"/> when several messages of a thread are
    /// being surveyed for files. Null if the message can't be read.</summary>
    Task<IReadOnlyList<IntakeMessageAttachment>?> ListAttachmentsAsync(string graphMessageId, CancellationToken ct);
}

/// <summary>No-op reader used when Graph credentials aren't configured for the API. Returns null so
/// callers fall back to the stored preview rather than failing.</summary>
public sealed class NullIntakeMessageReader : IIntakeMessageReader
{
    public Task<IntakeMessageContent?> GetAsync(string graphMessageId, CancellationToken ct) =>
        Task.FromResult<IntakeMessageContent?>(null);
    public Task<IntakeAttachmentContent?> GetAttachmentAsync(string graphMessageId, string attachmentId, CancellationToken ct) =>
        Task.FromResult<IntakeAttachmentContent?>(null);
    public Task<IReadOnlyList<IntakeMessageAttachment>?> ListAttachmentsAsync(string graphMessageId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<IntakeMessageAttachment>?>(null);
}

/// <summary>Graph REST implementation (HttpClient + app-only token), matching the worker's style.</summary>
public sealed class GraphIntakeMessageReader : IIntakeMessageReader
{
    private const string GraphBase = "https://graph.microsoft.com/v1.0";

    private readonly HttpClient _http;
    private readonly GraphTokenProvider _tokens;
    private readonly MailboxIntakeOptions _options;
    private readonly ILogger<GraphIntakeMessageReader> _logger;

    public GraphIntakeMessageReader(
        HttpClient http, GraphTokenProvider tokens, MailboxIntakeOptions options, ILogger<GraphIntakeMessageReader> logger)
    {
        _http = http;
        _tokens = tokens;
        _options = options;
        _logger = logger;
    }

    private string Mailbox => Uri.EscapeDataString(_options.Mailbox);

    public async Task<IntakeMessageContent?> GetAsync(string graphMessageId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(graphMessageId))
            return null;

        // Pull the full body, the envelope (for the composer's reply prefill) and non-inline
        // attachment metadata in a single round trip.
        var url = $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(graphMessageId)}"
            + "?$select=body,hasAttachments,subject,from,toRecipients,ccRecipients,bccRecipients,replyTo,categories"
            // NB: contentId cannot join this $select — it lives on the fileAttachment subtype and
            // Graph rejects derived-type properties here. The full per-attachment fetch carries it.
            + "&$expand=attachments($select=id,name,size,contentType,isInline)";

        try
        {
            var token = await _tokens.GetTokenAsync(ct);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _http.SendAsync(request, ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Intake message {GraphId} not found in mailbox.", graphMessageId);
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Graph message read failed: {Status}.", (int)response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;

            string body = "";
            bool isHtml = true;
            if (root.TryGetProperty("body", out var bodyEl) && bodyEl.ValueKind == JsonValueKind.Object)
            {
                body = bodyEl.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
                var contentType = bodyEl.TryGetProperty("contentType", out var ctEl) ? ctEl.GetString() : "html";
                isHtml = !string.Equals(contentType, "text", StringComparison.OrdinalIgnoreCase);
            }

            var attachments = new List<IntakeMessageAttachment>();
            var inlineImages = new List<IntakeInlineImage>();
            if (root.TryGetProperty("attachments", out var atts) && atts.ValueKind == JsonValueKind.Array)
            {
                foreach (var att in atts.EnumerateArray())
                {
                    var name = att.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    if (string.IsNullOrWhiteSpace(name)) name = "(unnamed attachment)";
                    long size = att.TryGetProperty("size", out var s) && s.TryGetInt64(out var sz) ? sz : 0;
                    string? type = att.TryGetProperty("contentType", out var t) ? t.GetString() : null;
                    var attachmentId = att.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";

                    // An inline attachment (embedded image etc.) is part of the body, not a file:
                    // it is kept out of the attachment list, but its cid → attachment-id mapping is
                    // carried so the body builder can embed the picture where the sender put it.
                    var isInline = att.TryGetProperty("isInline", out var inline) && inline.ValueKind == JsonValueKind.True;
                    if (isInline)
                    {
                        if (!string.IsNullOrEmpty(attachmentId))
                            inlineImages.Add(new IntakeInlineImage(attachmentId, type, size));
                        continue;
                    }

                    attachments.Add(new IntakeMessageAttachment(name, size, type, attachmentId));
                }
            }

            string? fromEmail = null, fromName = null;
            if (root.TryGetProperty("from", out var from) && from.ValueKind == JsonValueKind.Object
                && from.TryGetProperty("emailAddress", out var fromAddr) && fromAddr.ValueKind == JsonValueKind.Object)
            {
                fromEmail = fromAddr.TryGetProperty("address", out var a) ? a.GetString() : null;
                fromName = fromAddr.TryGetProperty("name", out var nm) ? nm.GetString() : null;
            }

            static List<string> Addresses(JsonElement parent, string property)
            {
                var result = new List<string>();
                if (parent.TryGetProperty(property, out var recipients) && recipients.ValueKind == JsonValueKind.Array)
                    foreach (var r in recipients.EnumerateArray())
                        if (r.TryGetProperty("emailAddress", out var addr) && addr.ValueKind == JsonValueKind.Object
                            && addr.TryGetProperty("address", out var a) && a.GetString() is { Length: > 0 } email)
                            result.Add(email);
                return result;
            }

            var to = Addresses(root, "toRecipients");
            var cc = Addresses(root, "ccRecipients");
            var bcc = Addresses(root, "bccRecipients");
            var replyTo = Addresses(root, "replyTo").FirstOrDefault();
            var subject = root.TryGetProperty("subject", out var subj) ? subj.GetString() : null;

            var categories = new List<string>();
            if (root.TryGetProperty("categories", out var categoryElements) && categoryElements.ValueKind == JsonValueKind.Array)
                foreach (var categoryElement in categoryElements.EnumerateArray())
                    if (categoryElement.GetString() is { Length: > 0 } category)
                        categories.Add(category);

            return new IntakeMessageContent(body, isHtml, attachments, fromEmail, fromName, to, cc, replyTo, subject, inlineImages, categories, bcc);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Intake message read errored for {GraphId}.", graphMessageId);
            return null;
        }
    }

    public async Task<IReadOnlyList<IntakeMessageAttachment>?> ListAttachmentsAsync(string graphMessageId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(graphMessageId))
            return null;

        // Metadata only — the attachments collection without contentBytes (never selected, so the
        // bytes never travel). Same select as GetAsync's $expand, minus the body.
        var url = $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(graphMessageId)}"
            + "/attachments?$select=id,name,size,contentType,isInline";

        try
        {
            var token = await _tokens.GetTokenAsync(ct);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Graph attachment list failed for {GraphId}: {Status}.", graphMessageId, (int)response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;

            var attachments = new List<IntakeMessageAttachment>();
            if (root.TryGetProperty("value", out var atts) && atts.ValueKind == JsonValueKind.Array)
            {
                foreach (var att in atts.EnumerateArray())
                {
                    // Inline images are part of the body, not files anyone would attach onward.
                    if (att.TryGetProperty("isInline", out var inline) && inline.ValueKind == JsonValueKind.True)
                        continue;
                    var name = att.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    if (string.IsNullOrWhiteSpace(name)) name = "(unnamed attachment)";
                    long size = att.TryGetProperty("size", out var s) && s.TryGetInt64(out var sz) ? sz : 0;
                    string? type = att.TryGetProperty("contentType", out var t) ? t.GetString() : null;
                    var attachmentId = att.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
                    attachments.Add(new IntakeMessageAttachment(name, size, type, attachmentId));
                }
            }
            return attachments;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Attachment list errored for {GraphId}.", graphMessageId);
            return null;
        }
    }

    public async Task<IntakeAttachmentContent?> GetAttachmentAsync(string graphMessageId, string attachmentId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(graphMessageId) || string.IsNullOrEmpty(attachmentId))
            return null;

        // A fileAttachment carries its bytes as base64 contentBytes in the attachment resource.
        var url = $"{GraphBase}/users/{Mailbox}/messages/{Uri.EscapeDataString(graphMessageId)}"
            + $"/attachments/{Uri.EscapeDataString(attachmentId)}";

        try
        {
            var token = await _tokens.GetTokenAsync(ct);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Graph attachment read failed: {Status}.", (int)response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;

            var name = root.TryGetProperty("name", out var n) ? n.GetString() ?? "attachment" : "attachment";

            // Only fileAttachment has contentBytes. An itemAttachment is an Outlook item the sender
            // dragged into the email — nearly always a forwarded *message* ("please see Re: …") — and
            // Graph serves its raw MIME from /$value, which downloads as a .eml that Outlook opens.
            // Reference attachments (OneDrive links) have no bytes at all and stay undownloadable.
            if (!root.TryGetProperty("contentBytes", out var bytesEl) || bytesEl.ValueKind != JsonValueKind.String)
            {
                var odataType = root.TryGetProperty("@odata.type", out var typeEl) ? typeEl.GetString() : null;
                if (string.Equals(odataType, "#microsoft.graph.itemAttachment", StringComparison.OrdinalIgnoreCase))
                    return await GetItemAttachmentMimeAsync(url, name, token, ct);

                _logger.LogWarning("Attachment {AttachmentId} is not a file attachment ({Type}).", attachmentId, odataType);
                return null;
            }
            var contentType = root.TryGetProperty("contentType", out var t) ? t.GetString() ?? "application/octet-stream" : "application/octet-stream";
            var content = Convert.FromBase64String(bytesEl.GetString() ?? "");
            var contentId = root.TryGetProperty("contentId", out var cid) ? cid.GetString() : null;
            return new IntakeAttachmentContent(name, contentType, content, contentId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Attachment read errored for {GraphId}/{AttachmentId}.", graphMessageId, attachmentId);
            return null;
        }
    }

    /// <summary>The raw MIME of an attached message, as Graph returns it from
    /// <c>…/attachments/{id}/$value</c>. Named <c>{subject}.eml</c> so the browser hands it to the mail
    /// client rather than showing bytes. Null (logged) if Graph won't serve it — e.g. the attached
    /// item is a contact or calendar entry rather than a message.</summary>
    private async Task<IntakeAttachmentContent?> GetItemAttachmentMimeAsync(
        string attachmentUrl, string name, string token, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, attachmentUrl + "/$value");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Graph item attachment $value read failed: {Status}.", (int)response.StatusCode);
            return null;
        }

        var content = await response.Content.ReadAsByteArrayAsync(ct);
        if (content.Length == 0) return null;

        // Outlook names an attached message by its subject, with no extension; strip characters a
        // filename can't carry and give it the extension mail clients associate with raw messages.
        var fileName = new string(name.Trim().Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
        if (string.IsNullOrWhiteSpace(fileName)) fileName = "attached message";
        if (!fileName.EndsWith(".eml", StringComparison.OrdinalIgnoreCase)) fileName += ".eml";

        return new IntakeAttachmentContent(fileName, "message/rfc822", content);
    }
}

using Ganss.Xss;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Contracts.RecordLinks;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiRecordTools
{
    private static IEnumerable<AiTool> CorrespondenceTools()
    {
        var readers = JpmsRoleSets.AllInternal;

        return new AiTool[]
        {
            new(
                "list_request_correspondence",
                "The emails and attachments on a request, as a list of headlines — sender, subject, date, "
                + "and the names of anything attached. Cheap. Call this FIRST when you need to know what "
                + "exists, then call get_request_context only if you actually need the wording.",
                AiToolSchema.Object(("requestId", "string", "The request's id, from list_requests or find_by_reference.", true)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var requestId = AiToolSchema.Text(input, "requestId");
                    if (string.IsNullOrWhiteSpace(requestId)) return Fail("A requestId is required.");

                    var request = await context.Db.Requests.AsNoTracking()
                        .FirstOrDefaultAsync(row => row.RequestId == requestId, ct);
                    if (request is null) return Fail($"No request found with id {requestId}.");

                    // Files and drawing links held against the record itself.
                    var attachments = await context.Db.RequestAttachments.AsNoTracking()
                        .Where(row => row.RequestId == requestId)
                        .Select(row => new
                        {
                            row.RequestAttachmentId,
                            kind = row.Kind == (int)RequestAttachmentKind.Drawing ? "drawing" : "file",
                            name = row.Kind == (int)RequestAttachmentKind.Drawing
                                ? (row.DrawingCode ?? "") + " " + (row.RevisionLabel ?? "")
                                : (row.FileName ?? ""),
                            row.ContentType,
                            row.Caption
                        })
                        .ToListAsync(ct);

                    // The live mailbox thread. Headlines only — HasAttachments is a flag, not names,
                    // because names cost a per-message Graph round trip.
                    var emails = Array.Empty<object>() as IReadOnlyList<object>;
                    try
                    {
                        var reader = context.Services.GetRequiredService<RequestEmailReader>();
                        var messages = await reader.ForRequestAsync(requestId, ct);
                        emails = messages
                            .Select(message => (object)new
                            {
                                from = string.IsNullOrWhiteSpace(message.FromName) ? message.FromEmail : message.FromName,
                                message.Subject,
                                received = message.ReceivedAt,
                                message.HasAttachments,
                                preview = message.BodyPreview
                            })
                            .ToList();
                    }
                    catch (Exception ex)
                    {
                        return Serialise(new
                        {
                            ok = true,
                            request = request.Reference,
                            attachments,
                            emails = Array.Empty<object>(),
                            note = $"The mailbox could not be read ({ex.Message}). The attachments listed are the "
                                   + "ones held on the record itself."
                        });
                    }

                    return Serialise(new
                    {
                        ok = true,
                        request = request.Reference,
                        request.Title,
                        attachments,
                        emails,
                        note = "Previews are truncated. Call get_request_context for the full wording."
                    });
                }),

            new(
                "read_record_emails",
                "Every email tagged to a record — what its page shows as Communications or Tender "
                + "responses & related emails — with FULL bodies flattened to text, plus each "
                + "attachment's name and id. Works for ANY record type: bid packages, variations, "
                + "requests, work orders, defects, to-dos. This is the tool when the user says \"read "
                + "the emails\": tender line items, what a subcontractor quoted, who said what — it "
                + "all lives here. Each email carries its envelope (to, cc, bcc), so a SENT invite "
                + "shows who received it (bcc). Defaults to the record on the page in view; nothing else needs "
                + "calling first. Not for requests you are drafting a variation from — "
                + "get_request_context is richer there.",
                AiToolSchema.Object(
                    ("recordType", "string",
                        "One of: request, bid_package, variation, variation_quote, work_order, defect, "
                        + "todo, lad, cost_centre, scheduling, subcontractor_comms, valuation_snapshot, "
                        + "valuation_claim. "
                        + "Defaults to the record in view.", false),
                    ("recordId", "string", "The record's id. Defaults to the record in view.", false),
                    ("maxChars", "number",
                        "Total body budget. Default 25000, ceiling 50000. Newest emails keep their "
                        + "bodies first when it runs out; older ones fall back to headlines.", false)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var typeText = AiToolSchema.Text(input, "recordType") ?? context.Scope?.RecordType;
                    var recordId = AiToolSchema.Text(input, "recordId") ?? context.Scope?.RecordId;
                    if (string.IsNullOrWhiteSpace(typeText) || string.IsNullOrWhiteSpace(recordId))
                        return Fail("Say which record: pass recordType and recordId, or have the user open the record's page.");

                    if (!TryMapRecordType(typeText, out var recordType))
                    {
                        return Fail($"Emails cannot be read for \"{typeText}\" — tagged mail exists for: request, "
                            + "bid_package, variation, variation_quote, work_order, defect, todo, lad, "
                            + "cost_centre, scheduling, subcontractor_comms, valuation_snapshot, valuation_claim.");
                    }

                    IReadOnlyList<MailboxMessage> messages;
                    try
                    {
                        var emailReader = context.Services.GetRequiredService<RecordEmailReader>();
                        messages = await emailReader.ForRecordAsync(recordType, recordId!, ct);
                    }
                    catch (Exception ex)
                    {
                        return Fail($"The mailbox could not be read ({ex.Message}).");
                    }

                    if (messages.Count == 0)
                    {
                        return Serialise(new
                        {
                            ok = true,
                            emails = Array.Empty<object>(),
                            note = "No emails are tagged to this record — or the record was not found, "
                                   + "or the mailbox is not configured on this environment."
                        });
                    }

                    // Full bodies, newest emails first when the budget bites: on a tender the latest
                    // reply is usually the one carrying the prices. Older emails degrade to their
                    // headline + preview rather than disappearing.
                    var budget = Math.Clamp(AiToolSchema.Number(input, "maxChars") ?? 25_000, 1_000, 50_000);
                    var detailReader = context.Services.GetRequiredService<IIntakeMessageReader>();
                    var bodies = new Dictionary<string, object>(StringComparer.Ordinal);

                    // A wall clock over the whole per-message Graph fan-out. These fetches are
                    // sequential and each carries the shared HttpClient's ~100s default; a hop is
                    // one Claude call (up to 36s) PLUS this, under one ~45s gateway, so an
                    // unbounded loop on a slow mailbox took the whole turn past the ceiling and
                    // cost the user a 502. Expiring here costs the OLDER emails their bodies
                    // (they fall back to headline + preview below) — the same trade
                    // RequestContextAssembler makes.
                    var fetchClock = System.Diagnostics.Stopwatch.StartNew();
                    var fetchDeadline = TimeSpan.FromSeconds(8);

                    foreach (var message in messages.OrderByDescending(m => m.ReceivedAt))
                    {
                        if (budget <= 0 || fetchClock.Elapsed > fetchDeadline) break;
                        IntakeMessageContent? content;
                        try
                        {
                            content = await detailReader.GetAsync(message.Id, ct);
                        }
                        catch (Exception) when (!ct.IsCancellationRequested)
                        {
                            continue; // One unreadable email must not lose the rest.
                        }
                        if (content is null) continue;

                        var text = content.IsHtml
                            ? RequestContextAssembler.HtmlToText(new HtmlSanitizer().Sanitize(content.Body))
                            : content.Body ?? "";
                        text = text.Trim();

                        var take = Math.Min(text.Length, Math.Min(budget, 8_000));
                        var clipped = take < text.Length;
                        budget -= take;

                        bodies[message.Id] = new
                        {
                            body = clipped
                                ? text[..take] + "\n[… this email was longer and has been cut here.]"
                                : text,
                            // The envelope, so an OUTBOUND copy (the sent invite, the PO email)
                            // says who received it. Graph only fills bcc on the mailbox's own
                            // sent copies — which is exactly where a mass invite's recipients live.
                            to = content.To ?? Array.Empty<string>(),
                            cc = content.Cc ?? Array.Empty<string>(),
                            bcc = content.Bcc ?? Array.Empty<string>(),
                            attachments = content.Attachments
                                .Select(file => new { file.Id, file.Name, file.Size, file.ContentType })
                                .ToList()
                        };
                    }

                    // The replies the record's page is blind to (its amber "newer replies aren't
                    // filed yet" banner) — the SAME read the banner makes, so a connector session
                    // sees the gap without needing the user's mailbox connected, and can offer
                    // the file_unfiled_replies action. Best-effort: a throttled Graph read must
                    // not cost the tagged list above.
                    IReadOnlyList<MailboxMessage> unfiled = Array.Empty<MailboxMessage>();
                    try
                    {
                        var unfiledReader = context.Services
                            .GetRequiredService<IQueryHandler<ListUnfiledReplies, IReadOnlyList<MailboxMessage>>>();
                        unfiled = await unfiledReader.HandleAsync(new ListUnfiledReplies(recordType, recordId!), ct);
                    }
                    catch (Exception) when (!ct.IsCancellationRequested) { /* best-effort aside */ }

                    return Serialise(new
                    {
                        ok = true,
                        emails = messages.Select(message => new
                        {
                            messageId = message.Id,
                            from = string.IsNullOrWhiteSpace(message.FromName) ? message.FromEmail : message.FromName,
                            fromEmail = message.FromEmail,
                            message.Subject,
                            received = message.ReceivedAt,
                            detail = bodies.TryGetValue(message.Id, out var detail)
                                ? detail
                                : (object)new
                                {
                                    body = $"[body omitted — over the {AiToolSchema.Number(input, "maxChars") ?? 25_000}-character budget; "
                                           + $"preview: {message.BodyPreview}]",
                                    attachments = (object)(message.HasAttachments
                                        ? "has attachments — re-call with a higher maxChars to see their names"
                                        : "none")
                                }
                        }).ToList(),
                        unfiledReplies = unfiled.Count == 0
                            ? null
                            : unfiled.Select(reply => new
                            {
                                messageId = reply.Id,
                                from = string.IsNullOrWhiteSpace(reply.FromName) ? reply.FromEmail : reply.FromName,
                                fromEmail = reply.FromEmail,
                                reply.Subject,
                                received = reply.ReceivedAt,
                                preview = reply.BodyPreview
                            }).ToList<object>(),
                        note = "Oldest first. Each email's detail carries its envelope (to, cc, bcc — bcc is "
                               + "filled only on the mailbox's own sent copies, so a sent tender invite lists "
                               + "who it went to there). Attachment ids feed read_email_attachment. Nothing here "
                               + "extracts figures for you — read the bodies and quote only what they say."
                               + (unfiled.Count == 0
                                   ? ""
                                   : $" unfiledReplies lists {unfiled.Count} newer thread "
                                     + "member(s) NOT yet filed to this record (the page's amber banner) "
                                     + "— tell the user, and offer the file_unfiled_replies action to "
                                     + "file them all.")
                    });
                }),

            new(
                "read_email_attachment",
                "One attachment from an email you have read, by the messageId and attachment id "
                + "read_record_emails or get_mailbox_message returned — the same as read_source with "
                + "source_id mail:<messageId>|<attachmentId>, reading from the start. Prefer "
                + "read_source: it reads a NAMED sheet or page and pages through a long file, and "
                + "find_in_source finds where a reference appears first. Every standard format opens "
                + "(spreadsheets as displayed values, PDFs by page, Word documents, text files) and an "
                + "IMAGE is SHOWN to you on your next step. A SCANNED PDF reads by OCR where configured "
                + "(the result says text_source \"ocr\" and a confidence — say a figure came off a scan) "
                + "and otherwise its first page is SHOWN to you as a picture, the rest through read_source. "
                + "What genuinely cannot be read is refused naming the format and the route that works "
                + "— relay it rather than guessing.",
                AiToolSchema.Object(
                    ("messageId", "string",
                        "The email's messageId from read_record_emails or get_mailbox_message.", true),
                    ("attachmentId", "string", "The attachment's id from the same tool result.", true),
                    ("maxChars", "number",
                        "How much extracted text to return. Default 20000, minimum 2000, maximum "
                        + "50000. The result says where it stopped; continue with read_source.", false)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var messageId = AiToolSchema.Text(input, "messageId");
                    var attachmentId = AiToolSchema.Text(input, "attachmentId");
                    if (string.IsNullOrWhiteSpace(messageId) || string.IsNullOrWhiteSpace(attachmentId))
                        return Fail("Both messageId and attachmentId are required — read_record_emails returns them.");

                    var limit = Math.Clamp(AiToolSchema.Number(input, "maxChars") ?? Sources.AiSourceReader.DefaultReadChars,
                        Sources.AiSourceReader.MinReadChars, Sources.AiSourceReader.MaxReadChars);
                    return await AiSourceTools.ReadAsync(
                        context, AiSourceTools.MailSourceId(messageId!, attachmentId!), null, 1, limit, ct);
                }),

        };
    }
}

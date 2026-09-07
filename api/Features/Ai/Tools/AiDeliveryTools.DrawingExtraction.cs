using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiDeliveryTools
{
    private const int DimensionRows = 400;
    private const int CalloutRows = 400;
    private const int ShapeRows = 200;

    /// <summary>The structured read of a drawing revision — the same view the Documents page's
    /// "Extracted data" panel renders, through the same query handler.</summary>
    private static AiTool GetDocumentExtraction()
    {
        return new(
            "get_document_extraction",
            "What the portal read from a drawing revision's PDF — its title block (drawing number, "
            + "title, revision, scale, date, drawn by, job, client), the revision table, the scale "
            + "each page PROVED (figured dimensions matched to drawn lines at that scale — trust "
            + "scaleVerified before measuring anything), every figured dimension paired with the "
            + "line it measures (value in mm, axis H/V/D, from/to/label positions in real-world mm "
            + "from the sheet's bottom-left corner), every note and callout with its position, and "
            + "every closed shape with its real width, height, perimeter and area — plus any Revu "
            + "markups if Bluebeam read some, and the warnings the reader raised. Use it for "
            + "take-offs and quantities: the figured dimension is the number to use (sheets say "
            + "figured dimensions take preference over scaling); the drawn geometry is the "
            + "cross-check and the way to find which callout a dimension belongs to (nearest "
            + "position). Pass revisionId, or drawingId for that document's newest extracted "
            + "revision. status tells you whether the read has run; if it hasn't, the user can "
            + "queue it with Extract data on the document page.",
            AiToolSchema.Object(
                ("revisionId", "string", "The revision to read (list_documents with drawingId gives revision ids).", false),
                ("drawingId", "string", "Instead of revisionId: the document whose newest extracted revision to read.", false)),
            AiToolKind.Read,
            DrawingReaders,
            GetDocumentExtractionAsync);
    }

    private static async Task<string> GetDocumentExtractionAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var revisionId = AiToolSchema.Text(input, "revisionId")?.Trim();
        var drawingId = AiToolSchema.Text(input, "drawingId")?.Trim();
        if (string.IsNullOrWhiteSpace(revisionId) && string.IsNullOrWhiteSpace(drawingId))
            return Fail("Pass revisionId or drawingId.");

        DrawingExtractionView? view = null;
        if (!string.IsNullOrWhiteSpace(revisionId))
        {
            view = await Query<GetDrawingExtraction, DrawingExtractionView?>(context, new GetDrawingExtraction(revisionId), ct);
            if (view is null) return Serialise(new { ok = true, revisionId, status = "NotExtracted", note = "Nothing has been extracted from this revision yet — Extract data on the document page queues it." });
        }
        else
        {
            var revisions = await Query<ListRevisionsForDrawing, IReadOnlyList<DrawingRevision>>(
                context, new ListRevisionsForDrawing(drawingId!), ct);
            foreach (var revision in revisions.OrderByDescending(row => row.ReceivedAt).Take(8))
            {
                var candidate = await Query<GetDrawingExtraction, DrawingExtractionView?>(
                    context, new GetDrawingExtraction(revision.DrawingRevisionId), ct);
                if (candidate?.Extraction.Status == DrawingExtractionStatus.Succeeded) { view = candidate; break; }
                view ??= candidate;
            }
            if (view is null) return Serialise(new { ok = true, drawingId, status = "NotExtracted", note = "None of this document's revisions has been extracted yet — Extract data on the document page queues one." });
        }

        var extraction = view.Extraction;
        if (extraction.Status != DrawingExtractionStatus.Succeeded)
            return Serialise(new { ok = true, extraction.DrawingRevisionId, status = extraction.Status.ToString(), extraction.QueuedBy, extraction.QueuedAt, extraction.ErrorMessage });

        var structure = view.Structure;
        return Serialise(new
        {
            ok = true,
            extraction.DrawingRevisionId,
            extraction.DrawingId,
            status = "Succeeded",
            extraction.CompletedAt,
            extraction.PageCount,
            units = "Every position and size is in real-world millimetres (areas in m²), measured from the sheet's bottom-left corner at the page's proven scale. Prefer a dimension's value over the distance between its from/to points.",
            summary = new
            {
                extraction.DrawingNumber,
                extraction.RevisionLabel,
                extraction.Scale,
                extraction.ScaleVerified,
                extraction.DimensionCount,
                extraction.CalloutCount,
                extraction.ShapeCount,
                extraction.MarkupCount,
                extraction.MarkupsNote
            },
            titleBlock = structure?.TitleBlock,
            revisions = structure?.Revisions,
            scales = structure?.Scales,
            warnings = structure?.Warnings,
            dimensions = Capped(structure?.Dimensions, DimensionRows),
            callouts = Capped(structure?.Callouts.Select(callout => new { callout.Page, callout.At, Text = callout.Text.Length <= 400 ? callout.Text : callout.Text[..400] }).ToList(), CalloutRows),
            shapes = Capped(structure?.Shapes, ShapeRows),
            markups = view.Markups.Take(200),
            structureNote = structure is null
                ? "This revision was extracted before the portal read drawings itself — only the text layer is held. Extract data again on the document page to get the structured read."
                : null
        });
    }

    private static object? Capped<T>(IReadOnlyList<T>? rows, int cap)
    {
        if (rows is null) return null;
        if (rows.Count <= cap) return new { count = rows.Count, rows };
        return new { count = rows.Count, rows = (IReadOnlyList<T>)rows.Take(cap).ToList(), note = $"{rows.Count} in total; the first {cap} are listed." };
    }
}

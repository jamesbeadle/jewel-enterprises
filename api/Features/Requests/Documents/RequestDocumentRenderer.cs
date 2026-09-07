using System.Globalization;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Requests.Documents;

/// <summary>
/// Renders a <see cref="RequestDocumentModel"/> into a branded, Procore-style one-page request sheet
/// (PDF bytes) using PDFsharp/MigraDoc. Pure function of the model: no I/O, no database — the same
/// bytes come out for the same input (bar the generated-at footer), which keeps regeneration on
/// download/resend idempotent. Shared by the api (download) and worker (send) projects.
/// </summary>
public static partial class RequestDocumentRenderer
{



    public static byte[] Render(RequestDocumentModel model)
    {
        EnsureFonts();

        var document = new Document();
        document.Info.Title = $"{model.DisplayNumber} {model.TypeShort}".Trim();
        document.Info.Author = "Jewel Bespoke Build";
        document.Info.Subject = model.Title;

        var normal = document.Styles["Normal"]!;
        normal.Font.Name = FontFamily;
        normal.Font.Size = 9;
        normal.Font.Color = Ink;

        var section = A4Page(document);

        AddHeaderBand(section, model);
        AddTitleBlock(section, model);
        AddPartiesGrid(section, model);
        AddReferences(section, model);
        AddQuestionSection(section, model);
        AddItemisedQueries(section, model);
        AddResponseActionRequired(section, model);
        AddResponseSection(section, model);
        AddRecipients(section, model);
        AddFooter(section, model);

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, closeStream: false);
        return stream.ToArray();
    }
}

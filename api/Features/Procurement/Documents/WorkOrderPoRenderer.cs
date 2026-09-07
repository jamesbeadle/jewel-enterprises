using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Procurement.Documents;

/// <summary>
/// Renders a <see cref="WorkOrderPoDocumentModel"/> into the branded purchase-order PDF using
/// PDFsharp/MigraDoc — the server-side twin of the portal's PurchaseOrderSheet component (the
/// browser-printed sheet), section for section: header and meta, Sub/Vendor + Job parties, the
/// order summary, the standard Scope of Work text (special instructions, insurances/RAMS,
/// works-order info, programme, invoice and payment requirements, deposit), the priced lines with
/// paid-to-date, the acceptance wording and the signature blocks. Pure function of the model, so
/// an emailed attachment and any future download render identically. Palette and helpers follow
/// <see cref="Subcontractors.Documents.SubcontractorStatementRenderer"/>.
/// </summary>
public static partial class WorkOrderPoRenderer
{
    public static byte[] Render(WorkOrderPoDocumentModel model)
    {
        EnsureFonts();
        var document = NewDocument(model);
        var section = AddA4Section(document);

        AddHeaderBand(section, model);
        AddDetailsGrid(section, model);
        AddParties(section, model);
        AddSummaryTable(section, model);
        AddScopeOfWork(section, model);
        AddLinesTable(section, model);
        AddAcceptanceWording(section);
        AddSignatures(section, model);
        AddFooter(section);

        return ToPdfBytes(document);
    }

    private static Document NewDocument(WorkOrderPoDocumentModel model)
    {
        var document = new Document();
        document.Info.Title = $"Purchase Order {model.Order.Reference}".Trim();
        document.Info.Author = "Jewel Bespoke Build";
        document.Info.Subject = "Purchase order";

        var normal = document.Styles["Normal"]!;
        normal.Font.Name = FontFamily;
        normal.Font.Size = 9;
        normal.Font.Color = Ink;
        return document;
    }

    private static Section AddA4Section(Document document) => A4Page(document);

    private static byte[] ToPdfBytes(Document document)
    {
        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, closeStream: false);
        return stream.ToArray();
    }
}

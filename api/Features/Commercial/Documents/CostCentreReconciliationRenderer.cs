using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

/// <summary>
/// Renders one cost centre's reconciliation into a branded PDF using PDFsharp/MigraDoc — the
/// delivery position of a centre for the accountant to brief the managing director: the sales
/// lines grouped under the centre, the work orders (drafts included, marked) and Xero costs
/// against it, then gross profit, procurement gain / loss and margin. Pure function of the
/// document model, so the endpoint and any future email attachment render identically.
/// Follows the JewelBB palette established by ProgressReportRenderer.
/// </summary>
public static partial class CostCentreReconciliationRenderer
{
    private static readonly Color Negative = new(0xB4, 0x23, 0x18);

    public static byte[] Render(CostCentreReconciliationDocument document)
    {
        EnsureFonts();
        var pdf = NewDocument(document);
        var section = AddA4Section(pdf);

        AddHeaderBand(section, document);
        AddDetailsGrid(section, document);
        AddSalesLines(section, document);
        AddWorkOrders(section, document);
        AddXeroCosts(section, document);
        AddSummary(section, document);
        AddClosingNote(section, document);
        AddFooter(section, document);

        return ToPdfBytes(pdf);
    }

    private static Document NewDocument(CostCentreReconciliationDocument document)
    {
        var pdf = new Document();
        pdf.Info.Title = $"{document.ProjectName} — {document.Heading} reconciliation".Trim();
        pdf.Info.Author = "Jewel Bespoke Build";
        pdf.Info.Subject = "Cost centre reconciliation";

        var normal = pdf.Styles["Normal"]!;
        normal.Font.Name = FontFamily;
        normal.Font.Size = 9;
        normal.Font.Color = Ink;
        return pdf;
    }

    private static Section AddA4Section(Document pdf) => A4Page(pdf);

    private static byte[] ToPdfBytes(Document pdf)
    {
        var renderer = new PdfDocumentRenderer { Document = pdf };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, closeStream: false);
        return stream.ToArray();
    }
}

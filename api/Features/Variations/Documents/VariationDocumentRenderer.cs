using Jewel.JPMS.Api.Features.Requests.Documents;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Variations.Documents;

/// <summary>
/// Renders a <see cref="VariationDocumentModel"/> into the branded variation order sheet (PDF
/// bytes) using PDFsharp/MigraDoc — the same house style as the request (RFI) document. Pure
/// function of the model: no I/O, no database, so regeneration on download/attach/resend is
/// idempotent (two renders of an unchanged order differ only by the generated-at footer).
/// </summary>
public static class VariationDocumentRenderer
{
    public static byte[] Render(VariationDocumentModel model)
    {
        EnsureFonts();

        var document = new Document();
        document.Info.Title = $"{model.DisplayNumber} Variation Order".Trim();
        document.Info.Author = "Jewel Bespoke Build";
        document.Info.Subject = model.Title;

        var normal = document.Styles["Normal"]!;
        normal.Font.Name = FontFamily;
        normal.Font.Size = 9;
        normal.Font.Color = Ink;

        var section = A4Page(document);

        AddHeaderBand(section, model);
        VariationDocumentSections.AddTitleBlock(section, model);
        VariationDocumentSections.AddDetailsGrid(section, model);
        VariationDocumentSections.AddScopeOfWorks(section, model);
        VariationDocumentSections.AddNarrative(section, "Commercial basis", model.CommercialBasis);
        VariationDocumentCostBreakdown.Add(section, model);
        VariationDocumentSections.AddNarrative(section, "Programme impact", model.ProgrammeImpact);
        VariationDocumentSections.AddNarrative(section, "Exclusions", model.Exclusions);
        VariationDocumentSections.AddFooter(section, model);

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    private static void AddHeaderBand(Section section, VariationDocumentModel model)
    {
        var table = section.AddTable();
        table.Borders.Width = 0;
        var left = table.AddColumn(Unit.FromCentimeter(11.3));
        var right = table.AddColumn(Unit.FromCentimeter(6.5));
        right.Format.Alignment = ParagraphAlignment.Right;

        var row = table.AddRow();
        row.Shading.Color = Navy;
        row.TopPadding = Unit.FromMillimeter(4);
        row.BottomPadding = Unit.FromMillimeter(4);
        row.Cells[0].Format.LeftIndent = Unit.FromMillimeter(4);
        row.Cells[1].Format.RightIndent = Unit.FromMillimeter(4);
        row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
        row.Cells[1].VerticalAlignment = VerticalAlignment.Center;

        // Left: official logo, white document name, gold reference line.
        DocumentBranding.AddLogo(row.Cells[0], Unit.FromCentimeter(3.4), Unit.FromMillimeter(1.5));

        var heading = row.Cells[0].AddParagraph("VARIATION ORDER");
        heading.Format.Font.Size = 17;
        heading.Format.Font.Bold = true;
        heading.Format.Font.Color = White;
        SpaceAfter(heading, 1);

        var referenceLine = model.DisplayNumber.Length > 0
            ? $"{model.DisplayNumber}  ·  {model.Reference}"
            : model.Reference;
        var sub = row.Cells[0].AddParagraph(referenceLine);
        sub.Format.Font.Size = 9.5;
        sub.Format.Font.Bold = true;
        sub.Format.Font.Color = Gold;

        // Right: status + the dates the correspondent cares about.
        var status = row.Cells[1].AddParagraph(model.StatusLabel.ToUpperInvariant());
        status.Format.Font.Size = 10;
        status.Format.Font.Bold = true;
        status.Format.Font.Color = White;
        SpaceAfter(status, 2);

        var issued = row.Cells[1].AddParagraph($"Issued  {Date(model.IssuedDisplayDate)}");
        issued.Format.Font.Size = 8;
        issued.Format.Font.Color = White;

        if (model.ApprovedAt is { } approvedAt)
        {
            SpaceAfter(issued, 0.5);
            var approved = row.Cells[1].AddParagraph($"Approved  {Date(approvedAt)}");
            approved.Format.Font.Size = 8;
            approved.Format.Font.Color = Gold;
        }

        // Orange hairline directly beneath the band.
        Hairline(section);
    }
}

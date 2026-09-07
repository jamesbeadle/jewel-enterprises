using System.Globalization;
using Jewel.JPMS.Api.Features.Requests.Documents;
using Jewel.JPMS.Contracts.Commercial;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

/// <summary>
/// Everything the snapshot PDF needs beyond the frozen detail itself: the project identity for
/// the header, and whether this is a frozen snapshot or a working copy of the live report
/// (draft exports render the same statement with working-copy stamps instead of the immutable
/// -record wording). Assembled by <see cref="ValuationReportSnapshotPdfBuilder"/>.
/// </summary>
public sealed record ValuationReportSnapshotDocument(
    string ProjectReference,
    string ProjectName,
    string ClientName,
    ValuationReportSnapshotDetail Detail,
    bool IsDraft = false,
    // Cost code → master name, for the bill's area sub-headings when a line carries no
    // estimate section (ValuationReportAreas rule). Null renders codes rather than names.
    IReadOnlyDictionary<string, string>? CostCentreNames = null);

/// <summary>
/// Renders one frozen valuation-report snapshot into a branded PDF using PDFsharp/MigraDoc: the
/// same section + summary-footer layout as the on-screen snapshot viewer (Contract Works,
/// Provisional Sums, Contingency Sums, Variations), fed entirely from the snapshot's copied lines
/// — live report edits never show here, exactly as on screen. Contract, PC and contingency
/// work prints every line under its area heading (the accountant reconciles item by item);
/// only variations consolidate, to one row per order. Each row carries the movement
/// story the accountant traces a claim by: Previous / This period / Claimed, with lines that
/// moved this period shaded gold. Pure function of the document model, so the download endpoint
/// and the email attachment render identically.
/// Follows the JewelBB palette established by <see cref="Progress.Documents.ProgressReportRenderer"/>.
/// </summary>
public static partial class ValuationReportSnapshotRenderer
{
    private static readonly Color Negative = new(0xB4, 0x23, 0x18);
    // Warm gold tint behind lines that moved this period — light enough to print.
    private static readonly Color Highlight = new(0xFB, 0xF2, 0xE2);



    public static byte[] Render(ValuationReportSnapshotDocument document)
    {
        EnsureFonts();

        var snapshot = document.Detail.Snapshot;

        var pdf = new Document();
        pdf.Info.Title = $"{document.ProjectName} Valuation Report — {snapshot.Label}".Trim();
        pdf.Info.Author = "Jewel Bespoke Build";
        pdf.Info.Subject = document.IsDraft ? "Valuation report (working copy)" : "Valuation report";

        var normal = pdf.Styles["Normal"]!;
        normal.Font.Name = FontFamily;
        normal.Font.Size = 9;
        normal.Font.Color = Ink;

        var section = A4Page(pdf);

        AddHeaderBand(section, document);
        AddDetailsGrid(section, document);
        AddMovementLegend(section, document);

        // One column layout for the whole statement: the client-reference column appears in
        // every bill section or none, decided by the document's lines as a whole.
        var columns = ValuationReportBillColumns.For(document.Detail.Lines);
        AddElementGroup(section, document, columns, "Contract Works", ValuationElementType.ContractWorks);
        AddElementGroup(section, document, columns, "Provisional Sums", ValuationElementType.PcSum);
        AddElementGroup(section, document, columns, "Contingency Sums", ValuationElementType.Contingency);
        AddElementGroup(section, document, columns, "Variations", ValuationElementType.Variation);

        AddSummary(section, document.Detail);
        AddClosingNote(section, document);
        AddFooter(section, document);

        var renderer = new PdfDocumentRenderer { Document = pdf };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, closeStream: false);
        return stream.ToArray();
    }
}

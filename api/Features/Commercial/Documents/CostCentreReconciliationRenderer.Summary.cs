using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

public static partial class CostCentreReconciliationRenderer
{
    private static void AddSummary(Section section, CostCentreReconciliationDocument document)
    {
        SectionHeading(section, "Reconciliation");

        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        table.AddColumn(Unit.FromCentimeter(11.3));
        var value = table.AddColumn(Unit.FromCentimeter(6.5));
        value.Format.Alignment = ParagraphAlignment.Right;

        AddSummaryRow(table, "Sales value", Money(document.SalesValue));
        AddSummaryRow(table, "Costs — work orders committed", Money(document.WoCommitted));
        AddSummaryRow(table, "Costs — Xero & other", Money(document.NonWoCost));
        AddSummaryRow(table, "Total forecast cost of sales", Money(document.TotalCosts));
        AddSummaryRow(table, "Gross profit", Money(document.GrossProfit), strong: true, negative: document.GrossProfit < 0m);
        AddSummaryRow(table, "Target cost (sales less assumed markup)", Money(document.TargetCost));
        AddSummaryRow(table, "Procurement gain / loss (target less costs)", Money(document.ProcurementGainLoss),
            negative: document.ProcurementGainLoss < 0m);
        AddSummaryRow(table, "Margin",
            document.MarginPercent is { } margin ? Pct(margin) : "—",
            strong: true, negative: document.GrossProfit < 0m);

        SpaceAfterTable(section);
    }

    private static void AddSummaryRow(Table table, string label, string amountText, bool strong = false, bool negative = false)
    {
        var row = table.AddRow();
        if (strong) row.Shading.Color = Panel;
        row.TopPadding = Unit.FromMillimeter(1.2);
        row.BottomPadding = Unit.FromMillimeter(1.2);
        var p = row.Cells[0].AddParagraph(label);
        p.Format.LeftIndent = CellIndent;
        p.Format.Font.Size = strong ? 9 : 8.5;
        p.Format.Font.Bold = strong;
        p.Format.Font.Color = strong ? Navy : Muted;
        var v = row.Cells[1].AddParagraph(amountText);
        v.Format.RightIndent = CellIndent;
        v.Format.Font.Size = strong ? 9 : 8.5;
        v.Format.Font.Bold = strong;
        v.Format.Font.Color = negative ? Negative : strong ? Navy : Ink;
    }

    private static void AddClosingNote(Section section, CostCentreReconciliationDocument document)
    {
        var note = section.AddParagraph(
            "All figures are net of VAT. Work-order costs are committed values (draft orders are included "
            + "and marked; rejected drafts are not). Xero figures reflect the ledger as last synced into "
            + "JPMS. Figures are gross of reconciliation-package netting — this is the centre's full "
            + $"position. Generated on {Date(document.GeneratedAt)} from the live JPMS figures.");
        note.Format.Font.Size = 8;
        note.Format.Font.Color = Muted;
        SpaceBefore(note, 2);
    }

    private static void AddFooter(Section section, CostCentreReconciliationDocument document) =>
        HouseFooter(section, $"Generated {DateAndTime(document.GeneratedAt)} · live figures from the JPMS Financials tab");
}

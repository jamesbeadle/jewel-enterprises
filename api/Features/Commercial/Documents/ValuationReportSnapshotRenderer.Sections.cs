using System.Globalization;
using Jewel.JPMS.Api.Features.Requests.Documents;
using Jewel.JPMS.Contracts.Commercial;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

public static partial class ValuationReportSnapshotRenderer
{
    // ---- Sections -----------------------------------------------------------------------------

    private static void AddHeaderBand(Section section, ValuationReportSnapshotDocument document)
    {
        var snapshot = document.Detail.Snapshot;

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

        // The official Jewel Bespoke Build logo leads the band — the gold/orange registered
        // artwork reads directly on the navy ground (embedded once in DocumentBranding).
        DocumentBranding.AddLogo(row.Cells[0], Unit.FromCentimeter(3.4), Unit.FromMillimeter(1.5));

        var heading = row.Cells[0].AddParagraph("VALUATION REPORT");
        heading.Format.Font.Size = 17;
        heading.Format.Font.Bold = true;
        heading.Format.Font.Color = White;
        SpaceAfter(heading, 1);

        var sub = row.Cells[0].AddParagraph(string.IsNullOrWhiteSpace(document.ProjectReference)
            ? document.ProjectName
            : $"{document.ProjectReference} — {document.ProjectName}");
        sub.Format.Font.Size = 9.5;
        sub.Format.Font.Bold = true;
        sub.Format.Font.Color = Gold;

        var stamp = row.Cells[1].AddParagraph(snapshot.Label.ToUpperInvariant());
        stamp.Format.Font.Size = 10;
        stamp.Format.Font.Bold = true;
        stamp.Format.Font.Color = White;
        SpaceAfter(stamp, 2);

        var date = row.Cells[1].AddParagraph(document.IsDraft
            ? $"Prepared  {DateAndTime(snapshot.TakenAt)}"
            : $"Snapshot taken  {DateAndTime(snapshot.TakenAt)}");
        date.Format.Font.Size = 8;
        date.Format.Font.Color = Gold;

        if (document.IsDraft)
        {
            var draft = row.Cells[1].AddParagraph("WORKING COPY OF THE LIVE REPORT");
            draft.Format.Font.Size = 8;
            draft.Format.Font.Bold = true;
            draft.Format.Font.Color = Orange;
            SpaceBefore(draft, 1.5);
        }

        Hairline(section);
    }

    private static void AddDetailsGrid(Section section, ValuationReportSnapshotDocument document)
    {
        var snapshot = document.Detail.Snapshot;

        var spacer = section.AddParagraph();
        spacer.Format.SpaceAfter = Unit.FromMillimeter(1.5);
        spacer.Format.Font.Size = 2;

        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        var labelW = Unit.FromCentimeter(3.3);
        var valueW = Unit.FromCentimeter(5.6);
        table.AddColumn(labelW);
        table.AddColumn(valueW);
        table.AddColumn(labelW);
        table.AddColumn(valueW);

        AddGridRow(table,
            "Project", document.ProjectName,
            "Client", document.ClientName);
        AddGridRow(table,
            "Statement", snapshot.Label,
            document.IsDraft ? "Prepared" : "Snapshot taken", DateAndTime(snapshot.TakenAt));
        AddGridRow(table,
            "Revised contract sum", Money(snapshot.RevisedContractSum),
            "Total works complete", Money(snapshot.TotalWorksComplete));
        AddGridRow(table,
            "Certified to date", Money(snapshot.CertifiedToDate),
            "Payment due (ex VAT)", Money(snapshot.PaymentDueExVat));

        SpaceAfterTable(section);
    }

    /// <summary>
    /// One line telling the reader how to trace the claim: what the movement columns mean and
    /// what the gold shading marks. Sits between the details grid and the first bill section.
    /// </summary>
    private static void AddMovementLegend(Section section, ValuationReportSnapshotDocument document)
    {
        var legend = section.AddParagraph();
        legend.Format.Font.Size = 7.5;
        legend.Format.Font.Color = Muted;
        legend.AddFormattedText("◆ ", new Font { Color = Orange, Size = 7.5 });
        legend.AddText("Rows shaded gold moved this period. ");
        legend.AddFormattedText("Previous", new Font { Bold = true, Size = 7.5, Color = Muted });
        legend.AddText(" is the cumulative value claimed on the statement before this one, ");
        legend.AddFormattedText("This period", new Font { Bold = true, Size = 7.5, Color = Muted });
        legend.AddText(" the movement now being claimed, ");
        legend.AddFormattedText("Claimed", new Font { Bold = true, Size = 7.5, Color = Muted });
        legend.AddText(" the cumulative total to date. All figures net of VAT.");
        SpaceAfter(legend, 1.5);
    }

    private static void AddElementGroup(
        Section section, ValuationReportSnapshotDocument document, ValuationReportBillColumns columns,
        string title, ValuationElementType elementType)
    {
        // Contract, PC and contingency work prints every line under its area heading — the
        // accountant reconciles item by item; only variations consolidate, to one row per
        // variation order (ValuationReportBillRows).
        var groups = ValuationReportBillRows.GroupsFor(document.Detail.Lines, elementType,
            code => document.CostCentreNames is { } names && names.TryGetValue(code, out var name) ? name : null);
        if (groups.Count == 0)
            return;

        SectionHeading(section, title);
        var table = BillTable(section, columns);
        AddBillHeader(table, columns);

        foreach (var group in groups)
        {
            if (!string.IsNullOrWhiteSpace(group.AreaTitle))
                AddAreaHeadingRow(table, columns, group.AreaTitle);
            foreach (var row in group.Rows)
            {
                AddBillRow(table, columns, row);
            }
        }

        AddBillTotals(table, columns, title, groups.SelectMany(group => group.Rows).ToList());
        SpaceAfterTable(section);
    }

    // An area sub-heading inside the bill — the panel band the screen and workbook use, spanning
    // the full row so the itemised lines beneath read as one run ("Electrics", "Plumbing & Heating").
    private static void AddAreaHeadingRow(Table table, ValuationReportBillColumns columns, string title)
    {
        var row = table.AddRow();
        row.Shading.Color = Panel;
        row.TopPadding = Unit.FromMillimeter(1.0);
        row.BottomPadding = Unit.FromMillimeter(1.0);
        row.Cells[0].MergeRight = columns.Last;
        var heading = row.Cells[0].AddParagraph(title);
        heading.Format.Font.Size = 8.5;
        heading.Format.Font.Bold = true;
        heading.Format.Font.Color = Navy;
    }

    private static Table BillTable(Section section, ValuationReportBillColumns columns)
    {
        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        table.AddColumn(Unit.FromCentimeter(columns.CodeWidthCentimetres));
        if (columns.HasClientReference)
            table.AddColumn(Unit.FromCentimeter(columns.ClientReferenceWidthCentimetres));
        table.AddColumn(Unit.FromCentimeter(columns.DescriptionWidthCentimetres));
        var qty = table.AddColumn(Unit.FromCentimeter(columns.QuantityWidthCentimetres));
        var rate = table.AddColumn(Unit.FromCentimeter(columns.RateWidthCentimetres));
        var amount = table.AddColumn(Unit.FromCentimeter(columns.AmountWidthCentimetres));
        var percent = table.AddColumn(Unit.FromCentimeter(columns.PercentWidthCentimetres));
        var previous = table.AddColumn(Unit.FromCentimeter(columns.PreviousWidthCentimetres));
        var period = table.AddColumn(Unit.FromCentimeter(columns.PeriodWidthCentimetres));
        var claimed = table.AddColumn(Unit.FromCentimeter(columns.ClaimedWidthCentimetres));
        qty.Format.Alignment = ParagraphAlignment.Right;
        rate.Format.Alignment = ParagraphAlignment.Right;
        amount.Format.Alignment = ParagraphAlignment.Right;
        percent.Format.Alignment = ParagraphAlignment.Right;
        previous.Format.Alignment = ParagraphAlignment.Right;
        period.Format.Alignment = ParagraphAlignment.Right;
        claimed.Format.Alignment = ParagraphAlignment.Right;
        return table;
    }

    private static void AddBillHeader(Table table, ValuationReportBillColumns columns)
    {
        var header = table.AddRow();
        header.Shading.Color = Panel;
        header.TopPadding = Unit.FromMillimeter(1.2);
        header.BottomPadding = Unit.FromMillimeter(1.2);
        header.HeadingFormat = true;
        HeaderCell(header.Cells[columns.Code], "Code");
        if (columns.HasClientReference)
            HeaderCell(header.Cells[columns.ClientReference], "Client ref");
        HeaderCell(header.Cells[columns.Description], "Description");
        HeaderCell(header.Cells[columns.Quantity], "Qty");
        HeaderCell(header.Cells[columns.Rate], "Rate");
        HeaderCell(header.Cells[columns.Amount], "Amount");
        HeaderCell(header.Cells[columns.Percent], "%");
        HeaderCell(header.Cells[columns.Previous], "Previous");
        HeaderCell(header.Cells[columns.Period], "This period");
        HeaderCell(header.Cells[columns.Claimed], "Claimed");
    }

    private static void AddBillRow(Table table, ValuationReportBillColumns columns, ValuationReportBillRow line)
    {
        var moved = line.MovedThisPeriod;
        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(1.2);
        row.BottomPadding = Unit.FromMillimeter(1.2);
        // The gold tint is the accountant's scan line: only rows that moved carry it.
        if (moved) row.Shading.Color = Highlight;

        // The code is provenance, not the story — small, quiet, never competing with the money.
        var code = row.Cells[columns.Code].AddParagraph(line.Code);
        code.Format.Font.Size = 7;
        code.Format.Font.Color = Muted;

        // The client's own schedule-of-works item number — the figure they reconcile by.
        if (columns.HasClientReference)
        {
            var clientReference = row.Cells[columns.ClientReference].AddParagraph(line.ClientReference);
            clientReference.Format.Font.Size = 8;
            clientReference.Format.Font.Color = line.CountsTowardTotals ? Ink : Muted;
        }

        AddDescription(row.Cells[columns.Description], line);
        NumberCell(row.Cells[columns.Quantity], line.Quantity);
        NumberCell(row.Cells[columns.Rate], line.Rate);
        MoneyCell(row.Cells[columns.Amount], line.Amount,
            colour: line.Amount < 0 ? Negative : line.CountsTowardTotals ? null : Muted);

        var pct = row.Cells[columns.Percent].AddParagraph(line.CountsTowardTotals ? Pct(line.PercentComplete) : "—");
        pct.Format.Font.Size = 8;
        pct.Format.Font.Color = Muted;

        if (!line.CountsTowardTotals)
        {
            DashCell(row.Cells[columns.Previous]);
            DashCell(row.Cells[columns.Period]);
            DashCell(row.Cells[columns.Claimed]);
            return;
        }
        MoneyCell(row.Cells[columns.Previous], line.PreviousClaimed, colour: Muted);
        // The figure this statement exists to show — bold on the rows that moved.
        MoneyCell(row.Cells[columns.Period], line.PeriodIncrement, bold: moved,
            colour: line.PeriodIncrement < 0 ? Negative : moved ? Navy : Muted);
        MoneyCell(row.Cells[columns.Claimed], line.CumulativeClaimed);
    }

    private static void AddDescription(Cell cell, ValuationReportBillRow line)
    {
        var title = cell.AddParagraph(line.Title);
        title.Format.Font.Size = 8.5;
        title.Format.Font.Color = line.CountsTowardTotals ? Ink : Muted;
        if (!string.IsNullOrWhiteSpace(line.Comments))
        {
            var comment = cell.AddParagraph(line.Comments);
            comment.Format.Font.Size = 7.5;
            comment.Format.Font.Color = Muted;
        }
        if (string.IsNullOrWhiteSpace(line.KindLabel))
            return;
        var kind = cell.AddParagraph(line.KindLabel.ToUpperInvariant());
        kind.Format.Font.Size = 7;
        kind.Format.Font.Color = Muted;
    }

    private static void AddBillTotals(Table table, ValuationReportBillColumns columns, string title, IReadOnlyList<ValuationReportBillRow> rows)
    {
        var counting = rows.Where(row => row.CountsTowardTotals).ToList();
        var totals = table.AddRow();
        totals.Shading.Color = Panel;
        totals.TopPadding = Unit.FromMillimeter(1.4);
        totals.BottomPadding = Unit.FromMillimeter(1.4);
        var label = totals.Cells[columns.Description].AddParagraph($"{title} total");
        label.Format.Font.Size = 8.5;
        label.Format.Font.Bold = true;
        label.Format.Font.Color = Navy;
        MoneyCell(totals.Cells[columns.Amount], counting.Sum(row => row.Amount), bold: true);
        MoneyCell(totals.Cells[columns.Previous], counting.Sum(row => row.PreviousClaimed), bold: true);
        MoneyCell(totals.Cells[columns.Period], counting.Sum(row => row.PeriodIncrement), bold: true);
        MoneyCell(totals.Cells[columns.Claimed], counting.Sum(row => row.CumulativeClaimed), bold: true);
    }

    private static void AddSummary(Section section, ValuationReportSnapshotDetail detail)
    {
        var snapshot = detail.Snapshot;

        SectionHeading(section, "Valuation summary");

        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        table.AddColumn(Unit.FromCentimeter(11.3));
        var value = table.AddColumn(Unit.FromCentimeter(6.5));
        value.Format.Alignment = ParagraphAlignment.Right;

        void SummaryRow(string label, decimal amount, bool strong = false)
        {
            var row = table.AddRow();
            if (strong) row.Shading.Color = Panel;
            row.TopPadding = Unit.FromMillimeter(1.2);
            row.BottomPadding = Unit.FromMillimeter(1.2);
            var p = row.Cells[0].AddParagraph(label);
            p.Format.LeftIndent = Unit.FromMillimeter(1.5);
            p.Format.Font.Size = strong ? 9 : 8.5;
            p.Format.Font.Bold = strong;
            p.Format.Font.Color = strong ? Navy : Muted;
            MoneyCell(row.Cells[1], amount, bold: strong,
                colour: amount < 0 ? Negative : null);
        }

        // The movement being claimed on this statement — the same total as the per-section
        // "This period" columns, so the bill and the summary reconcile by inspection.
        var periodTotal = detail.Lines
            .Where(line => line.CountsTowardTotals)
            .Sum(line => line.PeriodIncrement);

        SummaryRow("Original contract sum", snapshot.ContractSum);
        SummaryRow("Net variations", snapshot.NetVariations);
        SummaryRow("Revised contract sum", snapshot.RevisedContractSum, strong: true);
        SummaryRow("Total works complete", snapshot.TotalWorksComplete);
        SummaryRow("Works claimed this period", periodTotal);
        SummaryRow($"Retention held ({Pct(snapshot.RetentionPercent)})", snapshot.RetentionHeld);
        SummaryRow($"Retention released ({Pct(snapshot.RetentionReleasePercent)})", snapshot.RetentionReleased);
        SummaryRow("Certified to date", snapshot.CertifiedToDate);
        // Only projects with a cash-up-front deposit show the deduction block — everyone
        // else's summary keeps its familiar By France shape. Mirrors the workbook: total
        // payable, less the deposit released, equals the amount actually invoiced.
        if (snapshot.DepositPercent > 0m || snapshot.DepositReleased != 0m)
        {
            SummaryRow("Payment due before deposit (ex VAT)", snapshot.PaymentDueExVat + snapshot.DepositReleased);
            SummaryRow($"Less deposit released ({Pct(snapshot.DepositPercent)})", snapshot.DepositReleased);
        }
        SummaryRow("Payment due (ex VAT)", snapshot.PaymentDueExVat, strong: true);

        SpaceAfterTable(section);
    }

    private static void AddClosingNote(Section section, ValuationReportSnapshotDocument document)
    {
        var snapshot = document.Detail.Snapshot;
        // The working-copy note must be true at every stage of the claim — draft, locked, invoiced
        // or confirmed — so it says what this print IS and where the issued statement lives, and
        // never claims the figures are still moving or that nothing has gone out.
        var note = section.AddParagraph(document.IsDraft
            ? "All figures are net of VAT. This is a WORKING COPY of the live valuation report as it stood "
              + $"when prepared on {Date(snapshot.TakenAt)}: an internal review print of the claim as the report "
              + "currently shows it, not a statement issued to anyone. The statement issued for a claim is the "
              + "frozen snapshot behind its valuation invoice, held in the Report Snapshots register. Contract, "
              + "provisional and contingency items are shown individually under their area headings; each "
              + "approved variation shows as one consolidated row per order, and orders not yet approved are "
              + "not listed. Declined and to-be-confirmed items are not priced into any total."
            : "All figures are net of VAT. This statement is a frozen record of the valuation report exactly "
              + $"as it stood when the snapshot was taken on {Date(snapshot.TakenAt)}; work recorded since is "
              + "not reflected here. Contract, provisional and contingency items are shown individually under "
              + "their area headings; each approved variation shows as one consolidated row per order, and "
              + "orders not yet approved are not listed. Declined and to-be-confirmed items are not priced "
              + "into any total. If anything on this statement doesn't "
              + "match your records, please get "
              + "in touch so we can reconcile it together.");
        note.Format.Font.Size = 8;
        note.Format.Font.Color = Muted;
        SpaceBefore(note, 2);
    }

    private static void AddFooter(Section section, ValuationReportSnapshotDocument document)
    {
        var snapshot = document.Detail.Snapshot;
        HouseFooter(section, document.IsDraft
            ? $"Prepared {DateAndTime(snapshot.TakenAt)} · working copy of the live report"
            : $"Snapshot taken {DateAndTime(snapshot.TakenAt)} · immutable record from the JPMS register");
    }
}

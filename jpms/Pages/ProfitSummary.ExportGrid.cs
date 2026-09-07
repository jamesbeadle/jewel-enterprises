using Jewel.JPMS.Features.Cvr;
using static Jewel.JPMS.Features.Cvr.ProfitDisplay;

namespace Jewel.JPMS.Pages;

public partial class ProfitSummary
{
    // ---- The running-profit grid in the workbook ----------------------------
    // Two tabs. "Running profit" is the grid as the page shows it: a presentation sheet, one
    // project per PAIR of rows — the running % to date on the first (a real percentage, so it
    // can be summed against nothing and checked against everything), the small print on the
    // second ("77.5% ▼ · +£31k", or the £ alone, greyed, under the floor) — with every month
    // cell filled on the ONE colour rule, the sign of the month's £. "Running profit data" is
    // the same months as plain rows (invoiced, profit, month margin, running %, movement) so the
    // margin can be checked against the Xero site P&L line by line.

    private static readonly ExcelCellStyle GridBand = new(Fill: ExcelFill.Navy);
    private static readonly ExcelCellStyle GridBandTitle = new(Font: ExcelFont.Title, Fill: ExcelFill.Navy);
    private static readonly ExcelCellStyle GridBandText = new(Font: ExcelFont.BandText, Fill: ExcelFill.Navy);
    private static readonly ExcelCellStyle GridLegend = new(Font: ExcelFont.SmallMuted, WrapText: true);
    private static readonly ExcelCellStyle GridColumnHead = new(Font: ExcelFont.Muted, Fill: ExcelFill.Panel, Border: ExcelBorder.Hairline);
    private static readonly ExcelCellStyle GridColumnHeadCentre = GridColumnHead with { Align = ExcelAlign.Center };
    private static readonly ExcelCellStyle GridName = new(Font: ExcelFont.Bold);
    private static readonly ExcelCellStyle GridReference = new(Font: ExcelFont.SmallMuted, Border: ExcelBorder.Hairline);
    private static readonly ExcelCellStyle GridTotalName = new(Font: ExcelFont.NavyBold, Fill: ExcelFill.Panel);
    private static readonly ExcelCellStyle GridTotalNote = new(Font: ExcelFont.SmallMuted, Fill: ExcelFill.Panel, Border: ExcelBorder.Hairline);
    private static readonly ExcelCellStyle GridPoints = new(Format: ExcelFormat.Number, Align: ExcelAlign.Center);
    private static readonly ExcelCellStyle GridPointsNegative = GridPoints with { Font = ExcelFont.Negative };
    private static readonly ExcelCellStyle GridPosition = new(Format: ExcelFormat.Percent, Font: ExcelFont.NavyBold, Align: ExcelAlign.Center);
    private static readonly ExcelCellStyle GridPositionNegative = GridPosition with { Font = ExcelFont.Negative };
    private static readonly ExcelCellStyle GridPositionMoney = new(Format: ExcelFormat.Currency, Font: ExcelFont.Muted, Border: ExcelBorder.Hairline, Align: ExcelAlign.Center);
    private static readonly ExcelCellStyle GridDash = new(Font: ExcelFont.SmallMuted, Align: ExcelAlign.Center);
    private static readonly ExcelCellStyle GridDashUnder = GridDash with { Border = ExcelBorder.Hairline };

    private const string RunningProfitSheetName = "Running profit";
    private const string RunningProfitDataSheetName = "Running profit data";
    private const double GridLabelColumnWidth = 30;
    private const double GridMonthColumnWidth = 16;

    private void AddRunningProfitSheets(ExcelWorkbook workbook, MovementModel movement)
    {
        AddRunningProfitGridSheet(workbook, movement);
        AddRunningProfitDataSheet(workbook, movement);
    }

    private void AddRunningProfitGridSheet(ExcelWorkbook workbook, MovementModel movement)
    {
        var columns = new List<ExcelColumn> { new("Project", Width: GridLabelColumnWidth) };
        columns.AddRange(movement.Months.Select(month => new ExcelColumn(month.ToString("MMM yy"), Width: GridMonthColumnWidth)));
        columns.Add(new ExcelColumn("6-mo Δ", Width: GridMonthColumnWidth));
        columns.Add(new ExcelColumn("Position now", Width: GridMonthColumnWidth));
        var columnCount = columns.Count;

        var sheet = workbook.AddSheet(RunningProfitSheetName, columns.ToArray());
        sheet.ShowHeaderRow = false;
        sheet.AutoFilter = false;
        sheet.FreezeHeaderRow = false;
        sheet.ShowGridLines = false;
        sheet.PrintLandscapeFitToWidth = true;
        sheet.FrozenColumns = 1;

        object?[] Band(ExcelStyledCell first)
        {
            var cells = Enumerable.Repeat<object?>(new ExcelStyledCell(null, GridBand), columnCount).ToArray();
            cells[0] = first;
            return cells;
        }
        void AddMergedRow(object?[] cells)
        {
            sheet.AddRow(cells);
            sheet.MergedRanges.Add($"A{sheet.Rows.Count}:{ColumnLetter(columnCount)}{sheet.Rows.Count}");
        }

        AddMergedRow(Band(new ExcelStyledCell("Running profit by month", GridBandTitle)));
        AddMergedRow(Band(new ExcelStyledCell(
            $"Xero site P&L, {BasisLabel} · exported {DateTime.Now:dd MMM yyyy HH:mm} · month % floor £{movement.MonthPercentFloor:N0}",
            GridBandText)));
        AddMergedRow(new object?[]
        {
            new ExcelStyledCell(
                "Main figure = the whole job to that month end (cumulative profit over cumulative invoicing, the Running % Profit row). " +
                "Small print = that month on its own: the month's profit over the month's invoicing, then the month's £; ▲/▼ = above or below the running % at the end of the month before. " +
                "Colour = whether the month made (green) or lost (red) money — nothing else drives it. " +
                $"A month invoicing under £{movement.MonthPercentFloor:N0} shows its £ only. The data tab has every month's invoicing, profit and margin to check against Xero.",
                GridLegend)
        });
        sheet.AddRow();

        var heads = new object?[columnCount];
        heads[0] = new ExcelStyledCell("Project", GridColumnHead);
        for (var index = 1; index < columnCount; index++)
            heads[index] = new ExcelStyledCell(columns[index].Header, GridColumnHeadCentre);
        sheet.AddRow(heads);
        sheet.FrozenRows = sheet.Rows.Count;

        foreach (var row in movement.Rows)
        {
            AddGridRowPair(sheet, movement, row.Project.Name, row.Project.Reference, isTotal: false,
                row.MonthCells, row.WindowDelta, row.RunningPercent, row.PositionMoney);
        }
        if (movement.Rows.Count > 1)
        {
            AddGridRowPair(sheet, movement, "All jobs with Xero data", "combined margin — total profit over total invoiced", isTotal: true,
                movement.ColumnTotals, movement.TotalWindowDelta, movement.TotalRunningPercent, movement.TotalPositionMoney);
        }
    }

    // One project: the running-% row, then the small-print row beneath it. Both rows' month
    // cells wear the month's colour, so the pair reads as one cell of the grid.
    private static void AddGridRowPair(
        ExcelSheet sheet, MovementModel movement, string name, string note, bool isTotal,
        IReadOnlyList<RunningCell> cells, decimal? windowDelta, decimal? runningPercent, decimal positionMoney)
    {
        var floor = movement.MonthPercentFloor;
        var top = new List<object?> { new ExcelStyledCell(name, isTotal ? GridTotalName : GridName) };
        var under = new List<object?> { new ExcelStyledCell(note, isTotal ? GridTotalNote : GridReference) };
        foreach (var cell in cells)
        {
            var fill = MonthFill(cell.Own);
            if (cell.Empty)
            {
                top.Add(new ExcelStyledCell("—", GridDash with { Fill = fill }));
                under.Add(new ExcelStyledCell(null, GridDashUnder with { Fill = fill }));
                continue;
            }
            top.Add(cell.Running is decimal running
                ? new ExcelStyledCell(running / 100m, new ExcelCellStyle(
                    Format: ExcelFormat.Percent,
                    Font: running < 0m ? ExcelFont.Negative : ExcelFont.Bold,
                    Fill: fill,
                    Align: ExcelAlign.Center))
                : new ExcelStyledCell("n/a", GridDash with { Fill = fill }));
            // Under the floor (or nothing ever invoiced) the small print is the £ alone, greyed —
            // the same SmallPrint the screen prints, so the two can never disagree.
            var greyed = cell.MonthPercent(floor) is null;
            under.Add(new ExcelStyledCell(
                cell.Running is null && cell.Own.Empty ? null : SmallPrint(cell, floor),
                new ExcelCellStyle(
                    Font: greyed ? ExcelFont.SmallMuted : ExcelFont.Muted,
                    Fill: fill,
                    Border: ExcelBorder.Hairline,
                    Align: ExcelAlign.Center)));
        }
        top.Add(windowDelta is decimal delta
            ? new ExcelStyledCell(Math.Round(delta, 1), delta < 0m ? GridPointsNegative : GridPoints)
            : new ExcelStyledCell("—", GridDash));
        under.Add(new ExcelStyledCell(null, GridDashUnder));
        top.Add(runningPercent is decimal position
            ? new ExcelStyledCell(position / 100m, position < 0m ? GridPositionNegative : GridPosition)
            : new ExcelStyledCell("—", GridDash));
        under.Add(new ExcelStyledCell(positionMoney, GridPositionMoney));
        sheet.AddRow(top.ToArray());
        sheet.AddRow(under.ToArray());
    }

    /// <summary>The grid's one colour rule, in Excel: the sign of the month's own £.</summary>
    private static ExcelFill MonthFill(MonthCell own) => own.MoneySign switch
    {
        > 0 => ExcelFill.Positive,
        < 0 => ExcelFill.Negative,
        _ => ExcelFill.None,
    };

    // The plain rows behind the grid: one per project-month, the figures the margin is made of.
    private static void AddRunningProfitDataSheet(ExcelWorkbook workbook, MovementModel movement)
    {
        var sheet = workbook.AddSheet(RunningProfitDataSheetName,
            new ExcelColumn("Project"),
            new ExcelColumn("Reference"),
            new ExcelColumn("Month", ExcelFormat.Date),
            new ExcelColumn("Invoiced in month", ExcelFormat.Currency),
            new ExcelColumn("Profit in month", ExcelFormat.Currency),
            new ExcelColumn("Month margin", ExcelFormat.Percent),
            new ExcelColumn("Month vs job so far"),
            new ExcelColumn("Invoiced to date", ExcelFormat.Currency),
            new ExcelColumn("Profit to date", ExcelFormat.Currency),
            new ExcelColumn("Running % to date", ExcelFormat.Percent),
            new ExcelColumn("Movement in running % (pts)", ExcelFormat.Number),
            new ExcelColumn("Made or lost money"));

        void AddRows(string name, string reference, IReadOnlyList<RunningCell> cells)
        {
            for (var index = 0; index < cells.Count; index++)
            {
                var cell = cells[index];
                if (cell.Empty) continue;
                sheet.AddRow(
                    name,
                    reference,
                    movement.Months[index],
                    cell.Own.Income,
                    cell.Own.Profit,
                    cell.MonthPercent(movement.MonthPercentFloor) is decimal monthPct ? monthPct / 100m : null,
                    cell.MonthDirection(movement.MonthPercentFloor) switch { > 0 => "above", < 0 => "below", _ => "" },
                    cell.CumIncome,
                    cell.CumProfit,
                    cell.Running is decimal running ? running / 100m : null,
                    cell.MovementPp is decimal move ? Math.Round(move, 1) : null,
                    cell.Own.MoneySign switch { > 0 => "made", < 0 => "lost", _ => "nil" });
            }
        }

        foreach (var row in movement.Rows)
            AddRows(row.Project.Name, row.Project.Reference, row.MonthCells);
        if (movement.Rows.Count > 1)
            AddRows("All jobs with Xero data", "", movement.ColumnTotals);
    }

    /// <summary>The Excel letter of a 1-based column number — the grid is never wider than a few columns, but the rule is general.</summary>
    private static string ColumnLetter(int columnNumber)
    {
        var letters = "";
        while (columnNumber > 0)
        {
            var remainder = (columnNumber - 1) % 26;
            letters = (char)('A' + remainder) + letters;
            columnNumber = (columnNumber - 1) / 26;
        }
        return letters;
    }
}

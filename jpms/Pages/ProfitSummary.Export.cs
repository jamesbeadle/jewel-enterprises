using Jewel.JPMS.Features.Cvr;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Projects;
using Jewel.JPMS.Features.Xero;

namespace Jewel.JPMS.Pages;

public partial class ProfitSummary
{
    // Same rows the table renders — memo columns broken out so the workbook reconciles without
    // the on-screen memo lines — plus the total row when there's more than one project.
    private ExcelWorkbook? BuildExportWorkbook(bool _)
    {
        var rows = LoadedRows();
        if (rows.Count == 0) return null;

        var workbook = new ExcelWorkbook();
        var sheet = workbook.AddSheet("Profit summary",
            new ExcelColumn("Project"),
            new ExcelColumn("Stage"),
            new ExcelColumn("% complete", ExcelFormat.Percent),
            new ExcelColumn("Budgeted profit", ExcelFormat.Currency),
            new ExcelColumn("Budgeted margin", ExcelFormat.Percent),
            new ExcelColumn("Certified to date", ExcelFormat.Currency),
            new ExcelColumn("Retention held", ExcelFormat.Currency),
            new ExcelColumn("Cost to date", ExcelFormat.Currency),
            new ExcelColumn("Current profit", ExcelFormat.Currency),
            new ExcelColumn("Current margin", ExcelFormat.Percent),
            new ExcelColumn("Left to certify", ExcelFormat.Currency),
            new ExcelColumn("Cost to complete", ExcelFormat.Currency),
            new ExcelColumn("Profit to finish", ExcelFormat.Currency),
            new ExcelColumn("To-finish margin", ExcelFormat.Percent),
            new ExcelColumn("Final sales", ExcelFormat.Currency),
            new ExcelColumn("Net variations", ExcelFormat.Currency),
            new ExcelColumn("Final cost", ExcelFormat.Currency),
            new ExcelColumn("Forecast profit", ExcelFormat.Currency),
            new ExcelColumn("Forecast margin", ExcelFormat.Percent));

        // The margins and % complete go out as fractions (ExcelFormat.Percent's contract) and
        // as nulls when there is no base — a blank cell, matching the table's absent lines.
        void AddRow(string name, string stage, ProfitRow row, decimal? percentComplete) => sheet.AddRow(
            name,
            stage,
            percentComplete,
            row.BudgetedProfit,
            row.BudgetedMargin,
            row.CertifiedToDate,
            row.RetentionOutstanding,
            row.ActualCostOfSales,
            row.CurrentProfit,
            row.CurrentMargin,
            row.LeftToCertify,
            row.CostToComplete,
            row.ToFinishProfit,
            row.ToFinishMargin,
            row.ContractValue,
            row.NetVariations,
            row.ForecastCostOfSales,
            row.ForecastedProfit,
            row.ForecastedMargin);

        foreach (var (project, row) in rows)
            AddRow(project.Name, project.Stage.DisplayName(), row, row.PercentComplete);

        if (rows.Count > 1)
            AddRow("All projects", "", ProfitRow.TotalOf(rows), null);

        // The running-profit grid travels with the table (Jeremy, 2026-09-07: "the Excel export
        // of this grid carries the same small print and the same colour rule") — the same model
        // the panel renders, in the same row order, on the same basis and floor.
        if (MovementFor(GridProjects) is { } movement)
            AddRunningProfitSheets(workbook, movement);

        return workbook;
    }
}

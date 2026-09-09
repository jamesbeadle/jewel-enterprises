namespace Jewel.JPMS.Pages;

public partial class ComplianceRegister
{
    private ExcelWorkbook? BuildExportWorkbook(bool ignoreFilters)
    {
        var rows = ignoreFilters ? allRows : FilteredRows;
        if (rows.Count == 0) return null;

        var workbook = new ExcelWorkbook();
        var sheet = workbook.AddSheet("Compliance register",
            new ExcelColumn("Company"),
            new ExcelColumn("Trade"),
            new ExcelColumn("Document"),
            new ExcelColumn("Expires", ExcelFormat.Date),
            new ExcelColumn("Compliance"));

        foreach (var row in rows)
        {
            sheet.AddRow(
                row.Company.CompanyName,
                row.Company.TradesLabel,
                row.DocumentLabel,
                row.ExpiresAt,
                row.Status.DisplayName());
        }
        return workbook;
    }
}

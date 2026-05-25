using System.Globalization;
using System.Text;

namespace Jewel.JPMS.Services;

public static class CsvBuilder
{
    public static string ToDataUrl(string csvContent)
    {
        var bytes = Encoding.UTF8.GetBytes(csvContent);
        return $"data:text/csv;charset=utf-8;base64,{Convert.ToBase64String(bytes)}";
    }

    public static string BuildRows(IEnumerable<string> headers, IEnumerable<IEnumerable<object?>> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", headers.Select(EscapeCell)));
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", row.Select(FormatCell)));
        }
        return builder.ToString();
    }

    private static string FormatCell(object? value) => value switch
    {
        null               => "",
        DateTimeOffset dto => EscapeCell(dto.ToString("yyyy-MM-dd")),
        DateTime dt        => EscapeCell(dt.ToString("yyyy-MM-dd")),
        decimal money      => money.ToString("0.00", CultureInfo.InvariantCulture),
        _                  => EscapeCell(value.ToString() ?? "")
    };

    private static string EscapeCell(string cell)
    {
        if (cell.Contains(',') || cell.Contains('"') || cell.Contains('\n'))
        {
            return $"\"{cell.Replace("\"", "\"\"")}\"";
        }
        return cell;
    }
}

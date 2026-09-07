using System.Globalization;
using System.Text.RegularExpressions;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed partial class RunXeroCodingHandler
{
    private static readonly Regex MonthNameYear = new(
        @"\b(jan|feb|mar|apr|may|jun|jul|aug|sep|sept|oct|nov|dec)[a-z]*\.?\s*[-/']?\s*(20\d\d|\d\d)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex NumericMonthYear = new(
        @"\b(0?[1-9]|1[0-2])[/\-](20\d\d)\b|\b(20\d\d)[/\-](0?[1-9]|1[0-2])\b",
        RegexOptions.CultureInvariant);
    private static readonly string[] MonthAbbreviations =
        { "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec" };

    /// <summary>The month a bill number/reference states, when it states one.</summary>
    internal static (int Year, int Month)? StatedMonth(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var named = MonthNameYear.Match(text);
        if (named.Success)
        {
            var abbreviation = named.Groups[1].Value[..3].ToLowerInvariant();
            var month = Array.IndexOf(MonthAbbreviations, abbreviation) + 1;
            var year = int.Parse(named.Groups[2].Value, CultureInfo.InvariantCulture);
            if (year < 100) year += 2000;
            if (month > 0) return (year, month);
        }
        var numeric = NumericMonthYear.Match(text);
        if (!numeric.Success) return null;
        return numeric.Groups[1].Success
            ? (Year(numeric.Groups[2].Value), Year(numeric.Groups[1].Value))
            : (Year(numeric.Groups[3].Value), Year(numeric.Groups[4].Value));
    }

    private static int Year(string digits) => int.Parse(digits, CultureInfo.InvariantCulture);
}

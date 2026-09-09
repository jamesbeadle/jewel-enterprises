using System.Text.RegularExpressions;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

/// <summary>
/// Finds the work-order number a supplier wrote on a bill. The purchase order prints
/// "WO-0026"; suppliers copy it as "WO-0026", "WO 26", "wo26" or — on orders migrated from
/// Buildertrend, whose PO number the order kept — "PO-26". Fields are read in order of trust:
/// the bill's Reference (where Dext puts the PO number), then the line descriptions, then the
/// invoice number itself; the first field that names any order decides, and naming two
/// different orders in it is ambiguous, not a choice.
/// </summary>
public static partial class WorkOrderBillReference
{
    [GeneratedRegex(@"\b(?:WO|PO)[\s\-#:]*0*(\d{1,6})\b", RegexOptions.IgnoreCase)]
    private static partial Regex NumberPattern();

    /// <summary>The distinct order numbers named in the first field that names any; empty when none does.</summary>
    public static IReadOnlyList<int> NumbersOn(string? reference, IEnumerable<string?> descriptions, string? invoiceNumber)
    {
        var fields = new List<string?> { reference };
        fields.AddRange(descriptions);
        fields.Add(invoiceNumber);
        foreach (var field in fields)
        {
            var numbers = NumbersIn(field);
            if (numbers.Count > 0) return numbers;
        }
        return Array.Empty<int>();
    }

    /// <summary>The distinct order numbers named in one field; empty when it names none.</summary>
    public static IReadOnlyList<int> NumbersIn(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<int>();
        return NumberPattern().Matches(text)
            .Select(match => int.Parse(match.Groups[1].Value))
            .Distinct()
            .ToList();
    }
}

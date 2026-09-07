using System.Globalization;

namespace Jewel.JPMS.Features.Cvr;

/// <summary>How the profit figures read on screen — shared by the Profit Summary page, its
/// panels and the running-profit grid, defined once so none of them can round differently.</summary>
public static class ProfitDisplay
{
    public static string ProfitClass(decimal value) =>
        value == 0m ? "text-content-muted" : value > 0m ? "text-positive" : "text-negative";

    /// <summary>A signed whole-pound figure ("+£205,958", "-£12,400"); zero reads as a dash.</summary>
    public static string SignedMoney(decimal value) =>
        value == 0m ? "—" : value > 0m ? $"+£{value:N0}" : $"-£{Math.Abs(value):N0}";

    /// <summary>A margin fraction read as a percentage with one decimal ("9.1%").</summary>
    public static string Pct(decimal fraction) => $"{fraction * 100m:0.0}%";

    /// <summary>An inline-style percentage, culture-invariant — a comma decimal separator would silently break the CSS.</summary>
    public static string Pc(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>Signed £k with one decimal — the trajectory's six-month headline ("+4.7", "−12.0").</summary>
    public static string DeltaK(decimal value) =>
        value >= 0m ? $"+{value / 1000m:0.0}" : $"−{Math.Abs(value) / 1000m:0.0}";

    /// <summary>The cumulative chart's line pair — the accountant's mock colours, validated for
    /// CVD separation and contrast on the card surface.</summary>
    public const string InvoicedLineColor = "#3987e5";
    public const string CostLineColor = "#d95926";

    public static string MoneyCompact(decimal value)
    {
        var sign = value < 0m ? "-" : "";
        var abs = Math.Abs(value);
        return abs >= 1_000_000m ? $"{sign}£{abs / 1_000_000m:0.00}m"
            : abs >= 10_000m ? $"{sign}£{abs / 1_000m:0}k"
            : abs >= 1_000m ? $"{sign}£{abs / 1_000m:0.0}k"
            : $"{sign}£{abs:N0}";
    }

    /// <summary>Signed compact £ for the grid's small print and hovers ("+£8.1k", "−£4.8k").</summary>
    public static string SignedMoneyCompact(decimal value) =>
        value >= 0m ? $"+{MoneyCompact(value)}" : MoneyCompact(value);

    /// <summary>The grid's unit: a percentage with one decimal ("26.5%", "−39.0%"), the accountant's rounding.</summary>
    public static string PctCell(decimal value) =>
        value >= 0m ? $"{value:0.0}%" : $"−{Math.Abs(value):0.0}%";

    /// <summary>A movement in percentage points, one decimal, always signed ("+15.3", "−11.8").</summary>
    public static string SignedPp(decimal value) =>
        value >= 0m ? $"+{value:0.0}" : $"−{Math.Abs(value):0.0}";

    /// <summary>The movement in points as the hover words it: "+0.8 pts" / "−3.8 pts", "no movement" inside a twentieth of a point, or why there is none.</summary>
    public static string MovementWords(decimal? movementPp) =>
        movementPp is decimal move
            ? Math.Abs(move) >= 0.05m ? $"{SignedPp(move)} pts" : "no movement"
            : "no prior % to move from";

    /// <summary>The ▲/▼ after the month margin: above or below the running % at the end of the previous month. Empty when there is nothing to compare.</summary>
    public static string DirectionMarker(int direction) => direction > 0 ? "▲" : direction < 0 ? "▼" : "";

    /// <summary>The small print's month margin with its marker ("77.5% ▼"), or null when the month's invoicing is under the floor.</summary>
    public static string? MonthMarginPrint(RunningCell cell, decimal floor)
    {
        if (cell.MonthPercent(floor) is not decimal monthPct) return null;
        var marker = DirectionMarker(cell.MonthDirection(floor));
        return marker.Length == 0 ? PctCell(monthPct) : $"{PctCell(monthPct)} {marker}";
    }

    /// <summary>The whole small-print line — "77.5% ▼ · +£31k", or just the £ where the month % is suppressed, or "—" for a month where nothing happened.</summary>
    public static string SmallPrint(RunningCell cell, decimal floor)
    {
        if (cell.Own.Empty) return "—";
        var money = SignedMoneyCompact(cell.Own.Profit);
        return MonthMarginPrint(cell, floor) is { } margin ? $"{margin} · {money}" : money;
    }

    // The cell colours: ONE rule — the sign of the month's own £. Green made money, red lost
    // money, the neutral canvas where the month is nil or nothing happened. A flat tint, not a
    // scale: intensity would be a second signal, and the colour is meant to answer one question.
    private const string NeutralCellStyle = "background:#12151c";
    private const string PositiveCellStyle = "background:rgba(46,160,101,0.32)";
    private const string NegativeCellStyle = "background:rgba(194,85,85,0.32)";

    /// <summary>The grid's cell shading from the month's own £: green profit, red loss, neutral nil.</summary>
    public static string MonthCellStyle(MonthCell own) => own.MoneySign switch
    {
        > 0 => PositiveCellStyle,
        < 0 => NegativeCellStyle,
        _ => NeutralCellStyle,
    };

    /// <summary>The running cell's hover: the month's own figures (invoicing, so the margin can
    /// be checked), what the month did to the running % in points, then the position to date.</summary>
    public static string RunningCellHover(DateTime month, RunningCell cell, decimal floor)
    {
        var own = cell.Own.Income == 0m
            ? $"this month: nothing invoiced · profit {SignedMoneyCompact(cell.Own.Profit)}"
            : cell.MonthPercent(floor) is decimal monthPct
                ? $"this month: invoiced {MoneyCompact(cell.Own.Income)} · profit {SignedMoneyCompact(cell.Own.Profit)} ({PctCell(monthPct)})"
                : $"this month: invoiced {MoneyCompact(cell.Own.Income)} · profit {SignedMoneyCompact(cell.Own.Profit)} (month % not shown — invoicing under the £{floor:N0} floor)";
        var movement = cell.PriorRunning is decimal prior && cell.Running is decimal now
            ? $"running % {MovementWords(cell.MovementPp)} ({PctCell(prior)} → {PctCell(now)})"
            : $"running % {MovementWords(cell.MovementPp)}";
        return $"{month:MMM yy} — {own} · {movement} · to date: invoiced {MoneyCompact(cell.CumIncome)} · profit {SignedMoneyCompact(cell.CumProfit)}";
    }
}

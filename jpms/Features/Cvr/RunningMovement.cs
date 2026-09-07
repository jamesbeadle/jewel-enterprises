namespace Jewel.JPMS.Features.Cvr;

// Jeremy's running-balance format (2026-08-13): each cell's MAIN figure is the running % to
// date at that month end. The SMALL PRINT is the month's OWN margin (the month's profit over
// the month's invoicing) with the month's profit £ beside it, and the cell's COLOUR is the sign
// of that £ — green made money, red lost money (2026-09-07: the small print used to be the
// month's movement in the running %, and the colour followed it; on a job running at 84% a
// month that made £31k at 77% read as a red −0.8, so the cell said "loss" about a profit. A
// red cell must mean the month lost money). The movement in points survives in the hover, and
// a ▲/▼ after the month margin says whether the month ran above or below the job so far.
// The records here are that grid's shape — built by the Profit Summary page, rendered by
// RunningProfitTable and written to the Excel export.

/// <summary>Money movements within £50 of zero read as no movement (stale detection, trajectory
/// "flat", the grid's "—" for a month where nothing happened at all, and the neutral cell for a
/// month whose £ is nil).</summary>
public static class RunningMovement
{
    public const decimal ZeroThreshold = 50m;

    /// <summary>The low-invoicing floor's default: a month invoicing less than this shows its £
    /// only, greyed — £500 invoiced against £3,000 of bills is −500%, which is noise, not a
    /// margin (Jeremy, 2026-09-07). The page lets the reader change it.</summary>
    public const decimal DefaultMonthPercentFloor = 5_000m;
}

/// <summary>One month's (or window's) own figures. Percent is null when nothing was invoiced —
/// no base, no honest percentage (the n/a cell); Empty is a month where nothing moved at all
/// (the "—" cell).</summary>
public sealed record MonthCell(decimal Income, decimal Profit)
{
    public decimal? Percent => Income == 0m ? null : Profit / Income * 100m;
    public bool Empty => Income == 0m && Math.Abs(Profit) < RunningMovement.ZeroThreshold;

    /// <summary>The sign the cell is coloured on: +1 made money, −1 lost money, 0 nil (within the threshold).</summary>
    public int MoneySign => Math.Abs(Profit) < RunningMovement.ZeroThreshold ? 0 : Math.Sign(Profit);
}

/// <summary>The grid's cell: the job to date at a month end. Running is the main figure (null
/// while nothing has ever been invoiced — no base, no honest percentage: the n/a cell); Empty is
/// a month end before anything had happened at all (the "—" cell). Own is that month's own
/// figures — the small print and the colour. PriorRunning is the running % at the END of the
/// previous month (null when nothing had been invoiced by then): the month's movement is
/// Running − PriorRunning, and the ▲/▼ marker compares the month's own margin against it.</summary>
public sealed record RunningCell(MonthCell Own, decimal CumIncome, decimal CumProfit, decimal? PriorRunning)
{
    public decimal? Running => CumIncome == 0m ? null : CumProfit / CumIncome * 100m;
    public bool Empty => CumIncome == 0m && Math.Abs(CumProfit) < RunningMovement.ZeroThreshold;

    /// <summary>The month's movement in the running %, in points (null when either end has no %) — the hover's figure.</summary>
    public decimal? MovementPp => Running is decimal now && PriorRunning is decimal prior ? now - prior : null;

    /// <summary>The month's own margin, or null when the month's invoicing is under the floor (or nil) — no honest percentage to print.</summary>
    public decimal? MonthPercent(decimal floor) => Own.Income > 0m && Own.Income >= floor ? Own.Percent : null;

    /// <summary>+1 when the month's own margin ran above the running % at the end of the previous
    /// month (▲), −1 below (▼), 0 when there is nothing to compare (no month %, or no prior %).</summary>
    public int MonthDirection(decimal floor) =>
        MonthPercent(floor) is decimal month && PriorRunning is decimal prior && Math.Abs(month - prior) >= 0.05m
            ? Math.Sign(month - prior)
            : 0;
}

public sealed record MovementRow(
    Project Project,
    IReadOnlyList<RunningCell> MonthCells,  // the window's month ends, oldest first
    MonthCell Window,                       // the six months' own figures taken together (the Δ cell's hover)
    decimal? WindowDelta,                   // running % now minus six months ago — "6-mo Δ" (null when there was no % back then)
    decimal? RunningPercent,                // running % to date — "Position now"
    decimal PositionMoney,                  // cumulative profit £ (the memo line)
    decimal MoneySixMonthDelta,             // £ over the window — the trajectory's headline
    bool Stale);

public sealed record MovementModel(
    IReadOnlyList<DateTime> Months,
    IReadOnlyList<MovementRow> Rows,
    IReadOnlyList<RunningCell> ColumnTotals,   // the combined book to date, per month end
    MonthCell TotalWindow,
    decimal? TotalWindowDelta,
    decimal? TotalRunningPercent,
    decimal TotalPositionMoney,
    IReadOnlyList<Project> Excluded,
    decimal MonthPercentFloor);                // months invoicing under this show their £ only

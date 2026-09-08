namespace Jewel.JPMS.Features.Labour;

/// <summary>How labour records read on screen — the status and absence words, the money and
/// day figures, the confidence and bar arithmetic — shared by the overview page and its
/// components, defined once.</summary>
public static class LabourDisplay
{
    public static string Pounds(decimal value) => value.ToString("£#,##0");
    public static string Days(decimal value) => value == 0m ? "0" : value.ToString("0.##");

    public static string ConfidenceLabel(LabourOverviewTotals totals) =>
        totals.ElapsedWorkerDays == 0 ? "0%"
            : $"{(double)totals.ConfirmedWorkerDays / totals.ElapsedWorkerDays:P0}";

    public static string BarWidth(decimal value, decimal max) =>
        max <= 0m ? "0%" : $"{(double)(value / max) * 100:0}%";

    public static string CodingOutcomeLabel(XeroCodingOutcome outcome) => outcome switch
    {
        XeroCodingOutcome.BillRecoded => "bill recoded",
        XeroCodingOutcome.DraftStaged => "draft staged",
        XeroCodingOutcome.Skipped => "skipped",
        XeroCodingOutcome.WouldRecodeBill => "would recode bill",
        XeroCodingOutcome.WouldStageDraft => "would stage draft",
        XeroCodingOutcome.Reset => "reset",
        XeroCodingOutcome.BillApproved => "bill approved",
        _ => "failed",
    };

    /// <summary>A recorded outcome as the settlement table's Coding column reads it — the
    /// stored name (BillRecoded, DraftStaged, Skipped, Failed, Reset) in the same words as the
    /// run's own report.</summary>
    public static string CodingOutcomeLabel(string storedOutcome) =>
        Enum.TryParse<XeroCodingOutcome>(storedOutcome, out var outcome) ? CodingOutcomeLabel(outcome) : storedOutcome;

    /// <summary>A recorded outcome that blocks the run until reset (or until its bill vanishes
    /// from Xero) — the rows the settlement table offers "Reset" on.</summary>
    public static bool CodingOutcomeBlocksRerun(string storedOutcome) => XeroCodingOutcomes.IsWritten(storedOutcome);

    public static string StatusLabel(TimesheetStatus status) => status switch
    {
        TimesheetStatus.Approved => "approved",
        TimesheetStatus.Rejected => "sent back",
        _ => "waiting",
    };

    public static string AbsenceLabel(AbsenceKind kind) => kind switch
    {
        AbsenceKind.Holiday => "Holiday",
        AbsenceKind.HalfDay => "Half day",
        AbsenceKind.NotWorked => "Not worked",
        _ => "Sick",
    };
}

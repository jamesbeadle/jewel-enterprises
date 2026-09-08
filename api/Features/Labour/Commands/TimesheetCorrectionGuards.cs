using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// The points past which a timesheet may no longer be corrected (2026-09-07, the accountant's
// ask). Approval posts cost; unapprove and move exist because an approved day on the wrong
// project is a normal month-end event. But once that cost has gone DOWNSTREAM — the worker's
// week part signed off, an invoice line marked as covering the day, the Xero coding run having
// posted the worker's month — a quiet change on the row would leave the trail no longer tying
// to the money. Each guard names the step that undoes the downstream fact, so the person can
// take it deliberately (remove the sign-off, unmark the cover, reset the coding outcome) and
// then correct the day. Shared by UnapproveTimesheetHandler and MoveTimesheetHandler.

internal static class TimesheetCorrectionGuards
{
    /// <summary>The worker's settlement counterparty — the key covers are stored under (the
    /// linked company, else the worker themself when a sole trader; "" when neither).</summary>
    public static string CounterpartyOf(WorkerEntity worker) =>
        worker.SubcontractorId ?? (worker.IsSoleTrader ? worker.WorkerId : "");

    /// <summary>Sign-off is a view over approval: a week part signed off with this day approved
    /// would read signed-off-but-unapproved the moment the day went back. Unapprove refuses; a
    /// move leaves the day approved and needs no such check.</summary>
    public static async Task<string?> SignedOffReasonAsync(JpmsContext context, TimesheetEntity timesheet,
        CancellationToken cancellationToken)
    {
        var day = timesheet.WorkedOn.UtcDateTime.Date;
        var weekStart = new DateTimeOffset(ForecastRules.WeekStartOf(day), TimeSpan.Zero);
        var monthStart = new DateTimeOffset(ForecastRules.MonthStartOf(day), TimeSpan.Zero);
        var signedOff = await context.LabourWeekSignOffs.AsNoTracking()
            .AnyAsync(row => row.WorkerId == timesheet.WorkerId && row.WeekStart == weekStart && row.MonthStart == monthStart,
                cancellationToken);
        return signedOff
            ? $"{timesheet.WorkedOn:ddd dd MMM} sits in a signed-off week ({LabourWeekParts.Describe(weekStart, monthStart)}) — "
              + "remove the sign-off first (Labour overview, or remove_labour_week_sign_off), then correct the day."
            : null;
    }

    /// <summary>An invoice line marked as covering the day for the worker's counterparty means
    /// the cost is reconciled money. Project-scoped covers count for both corrections; the
    /// worker-month covers marked from the allocation queue (ProjectId "") count only for
    /// unapprove — a move between projects leaves the worker-month total untouched.</summary>
    public static async Task<string?> SettledReasonAsync(JpmsContext context, TimesheetEntity timesheet, WorkerEntity worker,
        bool includeWorkerMonthCovers, CancellationToken cancellationToken)
    {
        var counterparty = CounterpartyOf(worker);
        if (counterparty == "") return null;
        var day = timesheet.WorkedOn;
        var projectId = timesheet.ProjectId;
        var covered = await context.XeroLineTimesheetCovers.AsNoTracking()
            .AnyAsync(cover => cover.SubcontractorId == counterparty
                               && cover.PeriodStart <= day && cover.PeriodEnd > day
                               && (cover.ProjectId == projectId || (includeWorkerMonthCovers && cover.ProjectId == "")),
                cancellationToken);
        return covered
            ? $"{worker.Name}'s {day:ddd dd MMM} has been settled — an invoice line is marked as covering it, so its cost "
              + "is reconciled money now. Post a settlement variance for the difference instead, or unmark the cover "
              + "first and then correct the day."
            : null;
    }

    /// <summary>The Xero coding run has posted the worker's month (bill recoded, draft staged or
    /// bill approved as its latest recorded outcome): the schedule the bill was coded to would silently drift
    /// from the timesheets. reset_xero_coding_outcome is the deliberate way back.</summary>
    public static async Task<string?> CodedReasonAsync(JpmsContext context, TimesheetEntity timesheet, WorkerEntity worker,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateTimeOffset(ForecastRules.MonthStartOf(timesheet.WorkedOn.UtcDateTime.Date), TimeSpan.Zero);
        var latest = await context.XeroCodingRuns.AsNoTracking()
            .Where(run => run.WorkerId == timesheet.WorkerId && run.Month == monthStart)
            .OrderByDescending(run => run.RunAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (latest is null) return null;
        if (!XeroCodingOutcomes.IsWritten(latest.Outcome)) return null;
        var what = (XeroCodingOutcome)latest.Outcome switch
        {
            XeroCodingOutcome.BillRecoded => "its bill was recoded",
            XeroCodingOutcome.BillApproved => "its bill was approved",
            _ => "a draft bill was staged",
        };
        return $"{worker.Name}'s {monthStart:MMMM yyyy} has already been coded to Xero ({what} on {latest.RunAt:dd MMM}) — "
             + "reset that outcome first (reset_xero_coding_outcome, with a reason), correct the day, then run the coding again.";
    }

    /// <summary>The first downstream fact standing in the way, or null when the day is free to
    /// correct. Order: sign-off (cheapest to undo), cover, coding run.</summary>
    public static async Task<string?> FirstBlockAsync(JpmsContext context, TimesheetEntity timesheet, WorkerEntity worker,
        bool checkSignOff, bool includeWorkerMonthCovers, CancellationToken cancellationToken)
    {
        if (checkSignOff && await SignedOffReasonAsync(context, timesheet, cancellationToken) is { } signedOff) return signedOff;
        if (await SettledReasonAsync(context, timesheet, worker, includeWorkerMonthCovers, cancellationToken) is { } settled) return settled;
        return await CodedReasonAsync(context, timesheet, worker, cancellationToken);
    }
}

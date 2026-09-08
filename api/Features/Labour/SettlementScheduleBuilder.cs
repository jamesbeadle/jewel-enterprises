using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>
/// Builds the per-worker monthly settlement schedules (scope §6): approved timesheet cost split
/// by site × cost code (labour), plus sign-off-level materials/travel lines, the CIS deduction,
/// and the reconciliation verdict against covered Xero bills. Shared by the query slice and the
/// §6a coding run so the automation codes EXACTLY what the screen shows.
/// </summary>
public sealed class SettlementScheduleBuilder
{
    private readonly JpmsContext context;
    public SettlementScheduleBuilder(JpmsContext context) { this.context = context; }

    public async Task<SettlementScheduleSnapshot> BuildAsync(int year, int month, CancellationToken cancellationToken)
    {
        var monthStart = new DateTimeOffset(new DateTime(year, month, 1), TimeSpan.Zero);
        var monthEnd = monthStart.AddMonths(1);

        var workers = await context.Workers.Where(worker => worker.IsActive)
            .OrderBy(worker => worker.Name).ToListAsync(cancellationToken);
        var approved = await context.Timesheets
            .Where(sheet => sheet.WorkedOn >= monthStart && sheet.WorkedOn < monthEnd
                && sheet.WorkerId != "" && sheet.Status == (int)TimesheetStatus.Approved)
            .ToListAsync(cancellationToken);
        var extraLines = await context.WorkerSettlementLines
            .Where(line => line.Month == monthStart)
            .ToListAsync(cancellationToken);
        var cisStatuses = await context.WorkerCisStatuses
            .Where(row => row.EffectiveFrom < monthEnd).OrderBy(row => row.EffectiveFrom)
            .ToListAsync(cancellationToken);
        // Sign-off markers are per month part (2026-09-02): only THIS month's rows count, so a
        // week straddling the month end settles here on its own part and the neighbouring
        // month's part is that month's business.
        var signOffs = await context.LabourWeekSignOffs
            .Where(row => row.MonthStart == monthStart)
            .ToListAsync(cancellationToken);
        var covers = await context.XeroLineTimesheetCovers
            .Where(cover => cover.PeriodStart < monthEnd && cover.PeriodEnd > monthStart)
            .ToListAsync(cancellationToken);
        var coveredLineIds = covers.Select(cover => cover.XeroLedgerLineId).Distinct().ToList();
        var coveredLines = coveredLineIds.Count == 0
            ? new List<Data.Entities.XeroLedgerLineEntity>()
            : await context.XeroLedgerLines.Where(line => coveredLineIds.Contains(line.XeroLedgerLineId))
                .ToListAsync(cancellationToken);
        var variancesByCoveredLine = await SettlementVariances.ByCoveredLineAsync(context, coveredLineIds, cancellationToken);
        var runs = await context.XeroCodingRuns
            .Where(run => run.Month == monthStart).OrderBy(run => run.RunAt)
            .ToListAsync(cancellationToken);
        var projectNames = await context.Projects
            .ToDictionaryAsync(project => project.ProjectId, project => project.Name, cancellationToken);
        var subcontractors = await context.Subcontractors
            .ToDictionaryAsync(sub => sub.SubcontractorId, sub => sub.CompanyName, cancellationToken);

        var approvedByWorker = approved.ToLookup(sheet => sheet.WorkerId);
        var extraByWorker = extraLines.ToLookup(line => line.WorkerId);
        var coversBySub = covers.ToLookup(cover => cover.SubcontractorId);
        var coveredNetByLine = coveredLines.ToDictionary(line => line.XeroLedgerLineId, line => line.Net);
        var runsByWorker = runs.ToLookup(run => run.WorkerId);

        var rows = new List<WorkerSettlementSchedule>();
        foreach (var worker in workers)
        {
            var sheets = approvedByWorker[worker.WorkerId].ToList();
            var extras = extraByWorker[worker.WorkerId].ToList();
            if (sheets.Count == 0 && extras.Count == 0) continue;

            var lines = sheets
                .GroupBy(sheet => (sheet.ProjectId, sheet.CostCode))
                .Select(group => new ScheduleLine(
                    group.Key.ProjectId,
                    projectNames.TryGetValue(group.Key.ProjectId, out var name) ? name : group.Key.ProjectId,
                    group.Key.CostCode,
                    SettlementLineNature.CisLabour,
                    group.Sum(sheet => sheet.CostAmount),
                    null))
                .Concat(extras.Select(extra => new ScheduleLine(
                    extra.ProjectId,
                    projectNames.TryGetValue(extra.ProjectId, out var name) ? name : extra.ProjectId,
                    extra.CostCode,
                    (SettlementLineNature)extra.Nature,
                    extra.Amount,
                    extra.WorkerSettlementLineId)))
                .OrderBy(line => line.ProjectName).ThenBy(line => line.CostCode).ThenBy(line => line.Nature)
                .ToList();

            var grossLabour = lines.Where(line => line.Nature == SettlementLineNature.CisLabour).Sum(line => line.Amount);
            var grossOther = lines.Where(line => line.Nature != SettlementLineNature.CisLabour).Sum(line => line.Amount);
            var cisRate = cisStatuses.Where(row => row.WorkerId == worker.WorkerId)
                .OrderBy(row => row.EffectiveFrom).LastOrDefault()?.CisRatePercent ?? 20m;
            var cisDeduction = decimal.Round(grossLabour * cisRate / 100m, 2);

            // Covered bills for this worker's settlement counterparty in the period — the linked
            // company, or the worker themself when flagged a sole trader (2026-08-31). A worker
            // with neither cannot be reconciled against Xero — verdict says chase the link. A
            // cover the coding run marked per worker (2026-09-08) counts for that worker alone;
            // a cover without a worker is the counterparty's as a whole.
            var counterparty = WorkerSettlementIdentity.CounterpartyId(
                worker.SubcontractorId, worker.IsSoleTrader, worker.WorkerId);
            var coveredTotal = counterparty is null ? 0m
                : coversBySub[counterparty]
                    .Where(cover => cover.WorkerId is null || cover.WorkerId == worker.WorkerId)
                    .Sum(cover => coveredNetByLine.TryGetValue(cover.XeroLedgerLineId, out var net) ? net : 0m);

            // A posted variance is the accepted part of the difference (2026-09-08): what is left
            // is what still needs explaining, so a bill settled with a variance reads Matches.
            var postedVariance = counterparty is null ? 0m
                : SettlementVariances.PostedFor(worker.WorkerId, coversBySub[counterparty], variancesByCoveredLine);
            var grossTotal = grossLabour + grossOther;
            var difference = decimal.Round(coveredTotal - grossTotal - postedVariance, 2);
            var verdict =
                grossTotal == 0m && coveredTotal == 0m ? ScheduleVerdict.Nothing
                : coveredTotal == 0m ? ScheduleVerdict.NoBillYet
                : Math.Abs(difference) < 0.01m ? ScheduleVerdict.Matches
                : ScheduleVerdict.VarianceOpen;

            // Fully signed off = every week that carries approved time in this month has this
            // month's sign-off marker (its part of the week, for a week straddling the month
            // end). The §6a run refuses anything less.
            var approvedWeeks = sheets
                .Select(sheet => ForecastRules.WeekStartOf(sheet.WorkedOn.UtcDateTime.Date))
                .Distinct().ToList();
            var signedWeeks = signOffs.Where(row => row.WorkerId == worker.WorkerId)
                .Select(row => row.WeekStart.UtcDateTime.Date).ToHashSet();
            var fullySignedOff = approvedWeeks.Count > 0 && approvedWeeks.All(signedWeeks.Contains);

            var lastRun = runsByWorker[worker.WorkerId].LastOrDefault();

            // SubcontractorId on the row is the COUNTERPARTY id (company id, or the worker's own
            // id for a sole trader) — it is what covers key on and what the coding run looks up;
            // the name follows suit so a sole trader's draft bill is contacted under their name.
            rows.Add(new WorkerSettlementSchedule(
                worker.WorkerId, worker.Name, counterparty,
                worker.SubcontractorId is not null && subcontractors.TryGetValue(worker.SubcontractorId, out var sub) ? sub
                    : worker.IsSoleTrader ? worker.Name : "",
                lines, grossLabour, grossOther, grossTotal, cisRate, cisDeduction,
                decimal.Round(grossTotal - cisDeduction, 2),
                coveredTotal, difference, verdict, fullySignedOff,
                lastRun is null ? "" : ((XeroCodingOutcome)lastRun.Outcome).ToString(),
                lastRun?.RunAt,
                PostedVariance: postedVariance));
        }

        var rowsWithBills = CoveredBillResolver.Attach(rows, coversBySub, coveredLines.ToDictionary(line => line.XeroLedgerLineId));
        return new SettlementScheduleSnapshot(year, month, rowsWithBills,
            InvoicesToChase: rows.Count(row => row.Verdict == ScheduleVerdict.NoBillYet),
            WorkersToReconcile: rows.Count(row => row.Verdict is ScheduleVerdict.NoBillYet or ScheduleVerdict.VarianceOpen));
    }
}

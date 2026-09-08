using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

public sealed class GetLabourOverviewEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetLabourOverview, LabourOverviewSnapshot> handler;
    public GetLabourOverviewEndpoint(SignedInUserResolver users, IQueryHandler<GetLabourOverview, LabourOverviewSnapshot> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(GetLabourOverview))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "labour/overview/{year:int}/{month:int}")] HttpRequest request,
        int year, int month)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        // Rates and £ ride on every row, so the whole overview is gated to the managing roles.
        if (!LabourRoleSets.ManageWorkers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (year < 2020 || year > 2100 || month < 1 || month > 12) return new BadRequestResult();
        return new OkObjectResult(await handler.HandleAsync(new GetLabourOverview(year, month), request.HttpContext.RequestAborted));
    }
}

/// <summary>
/// Builds the whole month in one pass: per-worker forecast + placement grid, per-site and
/// per-cost-code recorded totals, the chase list and the confidence segments. Recorded cost for
/// unapproved rows is valued at the worker's current rate (pending view); approved rows use their
/// snapshotted CostAmount — same split the Financials tab draws.
/// </summary>
public sealed class GetLabourOverviewHandler : IQueryHandler<GetLabourOverview, LabourOverviewSnapshot>
{
    private readonly JpmsContext context;
    public GetLabourOverviewHandler(JpmsContext context) { this.context = context; }

    public async Task<LabourOverviewSnapshot> HandleAsync(GetLabourOverview query, CancellationToken cancellationToken)
    {
        var monthStart = new DateTimeOffset(new DateTime(query.Year, query.Month, 1), TimeSpan.Zero);
        var monthEnd = monthStart.AddMonths(1);
        var today = SiteClock.Today().UtcDateTime.Date;

        var workers = await context.Workers.Where(worker => worker.IsActive)
            .OrderBy(worker => worker.Name).ToListAsync(cancellationToken);
        var workerIds = workers.Select(worker => worker.WorkerId).ToList();

        var timesheets = await context.Timesheets
            .Where(sheet => sheet.WorkedOn >= monthStart && sheet.WorkedOn < monthEnd && sheet.WorkerId != "")
            .ToListAsync(cancellationToken);
        var absences = await context.WorkerAbsences
            .Where(absence => absence.Date >= monthStart && absence.Date < monthEnd)
            .ToListAsync(cancellationToken);
        var openAttendance = await context.SiteAttendances
            .Where(row => row.WorkDate >= monthStart && row.WorkDate < monthEnd && row.SignedOutAt == null)
            .ToListAsync(cancellationToken);
        // This month's sign-off markers only (per month part, 2026-09-02): a straddling week's
        // other part belongs to the neighbouring month's overview.
        var signOffs = await context.LabourWeekSignOffs
            .Where(row => row.MonthStart == monthStart)
            .ToListAsync(cancellationToken);
        var contracts = await context.WorkerContracts
            .Where(row => row.EffectiveFrom < monthEnd).OrderBy(row => row.EffectiveFrom)
            .ToListAsync(cancellationToken);
        // The chase expectation inputs (2026-08-31, the accountant's month-end doc, item C/D/G):
        // project assignments, this month's dismissals, and the month's coding runs — so the
        // generator only chases days someone was actually expected, and stops chasing what a
        // human has signed off, settled or dismissed.
        var assignedWorkerIds = (await context.ProjectWorkerAssignments
                .Where(row => row.IsActive)
                .Select(row => row.WorkerId)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();
        var dismissals = await context.LabourChaseDismissals
            .Where(row => row.Date >= monthStart && row.Date < monthEnd)
            .ToListAsync(cancellationToken);
        var dismissalsByWorker = dismissals.ToLookup(row => row.WorkerId);
        var settledWorkerIds = (await context.XeroCodingRuns
                .Where(run => run.Month == monthStart && XeroCodingOutcomes.WrittenValues.Contains(run.Outcome))
                .Select(run => run.WorkerId)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();
        var cisStatuses = await context.WorkerCisStatuses
            .Where(row => row.EffectiveFrom < monthEnd).OrderBy(row => row.EffectiveFrom)
            .ToListAsync(cancellationToken);
        var projectNames = await context.Projects
            .ToDictionaryAsync(project => project.ProjectId, project => project.Name, cancellationToken);
        // Grouped, not ToDictionary: Code is not a primary key, and a code that was deactivated
        // and re-added exists twice — a keyed dictionary throws on the duplicate and 500s the
        // whole overview. Prefer the active row's name.
        var costCentreNames = (await context.CostCenters.ToListAsync(cancellationToken))
            .GroupBy(centre => centre.Code)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(centre => centre.IsActive).First().Name);

        var timesheetsByWorker = timesheets.ToLookup(sheet => sheet.WorkerId);
        var absencesByWorker = absences.ToLookup(absence => absence.WorkerId);
        var signOffsByWorker = signOffs.ToLookup(row => row.WorkerId);
        var openByWorker = openAttendance.ToLookup(row => row.WorkerId);

        var workerRows = new List<LabourOverviewWorker>();
        var chase = new List<LabourChaseItem>();
        var appliedDismissals = 0;
        var weekBuckets = new Dictionary<DateTime, (int Elapsed, int Confirmed, decimal Unconfirmed)>();
        decimal projectedSpend = 0m, timeOffCost = 0m, amountDueTotal = 0m, unconfirmedCost = 0m;
        int elapsedWorkerDays = 0, confirmedWorkerDays = 0;

        foreach (var worker in workers)
        {
            var dayRate = worker.HourlyRate * ForecastRules.StandardHoursPerDay;
            var contractedDays = contracts.Where(row => row.WorkerId == worker.WorkerId && row.EffectiveFrom < monthEnd)
                .OrderBy(row => row.EffectiveFrom).LastOrDefault()?.ContractedDaysPerMonth ?? 0m;
            var cisRate = cisStatuses.Where(row => row.WorkerId == worker.WorkerId && row.EffectiveFrom < monthEnd)
                .OrderBy(row => row.EffectiveFrom).LastOrDefault()?.CisRatePercent ?? 20m;

            var workerSheets = timesheetsByWorker[worker.WorkerId].ToList();
            var workerAbsences = absencesByWorker[worker.WorkerId].ToList();

            var days = new List<LabourOverviewDay>();
            foreach (var sheet in workerSheets)
            {
                projectNames.TryGetValue(sheet.ProjectId, out var projectName);
                days.Add(new LabourOverviewDay(sheet.WorkedOn, sheet.ProjectId, projectName ?? sheet.ProjectId,
                    sheet.Hours, (TimesheetStatus)sheet.Status, null));
            }
            foreach (var absence in workerAbsences)
                days.Add(new LabourOverviewDay(absence.Date, "", "", 0m, null, (AbsenceKind)absence.Kind));

            var absenceKinds = workerAbsences.Select(absence => (AbsenceKind)absence.Kind).ToList();
            var projected = ForecastRules.ProjectedCost(contractedDays, dayRate, absenceKinds);
            var timeOff = ForecastRules.TimeOffCost(dayRate, absenceKinds);
            var due = ForecastRules.AmountDue(projected, cisRate);

            var sheetDates = workerSheets.Select(sheet => sheet.WorkedOn.UtcDateTime.Date).ToHashSet();
            var absenceDates = workerAbsences.Select(absence => absence.Date.UtcDateTime.Date).ToHashSet();
            var openDates = openByWorker[worker.WorkerId].Select(row => row.WorkDate.UtcDateTime.Date).ToHashSet();
            var dismissedDates = dismissalsByWorker[worker.WorkerId].Select(row => row.Date.UtcDateTime.Date).ToHashSet();
            var signedWeekStarts = signOffsByWorker[worker.WorkerId].Select(row => row.WeekStart.UtcDateTime.Date).ToHashSet();
            var monthSettled = settledWorkerIds.Contains(worker.WorkerId);
            var engagedFrom = worker.EngagedFrom?.UtcDateTime.Date;
            var engagedTo = worker.EngagedTo?.UtcDateTime.Date;
            var isAssigned = assignedWorkerIds.Contains(worker.WorkerId);

            // Elapsed weekdays this month, rebuilt 2026-08-31 (the accountant's month-end doc):
            // the old walk chased every weekday × every active worker, so six zero-contract
            // workers raised an item for every weekday in August and each one dragged a full day
            // rate into unconfirmedCost — the figures were wrong, not just noisy. A day now
            // counts only when it was RECORDED (timesheet or absence — evidence beats
            // expectation) or EXPECTED: inside the engagement window AND the worker is
            // contracted, assigned to a project, or holds an open sign-in that day. An expected,
            // unconfirmed day in a signed-off week, in a worker-month the coding run has already
            // settled, or dismissed with a reason, is a day a human has already answered for —
            // it leaves the chase list and the accrual together, so the confidence figures always
            // agree with the list below them.
            var first = monthStart.UtcDateTime.Date;
            var lastElapsed = today < monthEnd.UtcDateTime.Date.AddDays(-1) ? today : monthEnd.UtcDateTime.Date.AddDays(-1);
            for (var date = first; date <= lastElapsed; date = date.AddDays(1))
            {
                if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
                var confirmed = sheetDates.Contains(date) || absenceDates.Contains(date);
                if (!confirmed)
                {
                    var inEngagement = (engagedFrom is null || date >= engagedFrom)
                                       && (engagedTo is null || date <= engagedTo);
                    var expected = inEngagement
                                   && (contractedDays > 0m || isAssigned || openDates.Contains(date));
                    if (!expected) continue;
                    var weekSignedOff = signedWeekStarts.Contains(ForecastRules.WeekStartOf(date));
                    if (dismissedDates.Contains(date)) { appliedDismissals++; continue; }
                    if (monthSettled || weekSignedOff) continue;
                }

                var weekStart = ForecastRules.WeekStartOf(date);
                // Explicitly typed: the ternary's untyped (0, 0, 0m) fallback would strip the
                // tuple's element names and .Elapsed/.Confirmed/.Unconfirmed with them.
                (int Elapsed, int Confirmed, decimal Unconfirmed) bucket =
                    weekBuckets.TryGetValue(weekStart, out var existing) ? existing : default;
                bucket.Elapsed++;
                elapsedWorkerDays++;
                if (confirmed) { bucket.Confirmed++; confirmedWorkerDays++; }
                else
                {
                    bucket.Unconfirmed += dayRate;
                    unconfirmedCost += dayRate;
                    if (openDates.Contains(date))
                    {
                        var open = openByWorker[worker.WorkerId].First(row => row.WorkDate.UtcDateTime.Date == date);
                        projectNames.TryGetValue(open.ProjectId, out var openProjectName);
                        chase.Add(new LabourChaseItem(worker.WorkerId, worker.Name, new DateTimeOffset(date, TimeSpan.Zero),
                            LabourChaseReason.OpenAttendance, open.ProjectId, openProjectName ?? open.ProjectId));
                    }
                    else
                    {
                        chase.Add(new LabourChaseItem(worker.WorkerId, worker.Name, new DateTimeOffset(date, TimeSpan.Zero),
                            LabourChaseReason.NoTimesheet, "", ""));
                    }
                }
                weekBuckets[weekStart] = bucket;
            }

            var workerSignOffs = signOffsByWorker[worker.WorkerId]
                .Select(row => new LabourWeekSignOff(row.WorkerId, row.WeekStart, row.SignedOffByEmail, row.SignedOffAt, row.MonthStart))
                .OrderBy(row => row.WeekStart).ToList();

            projectedSpend += projected;
            timeOffCost += timeOff;
            amountDueTotal += due;

            workerRows.Add(new LabourOverviewWorker(worker.WorkerId, worker.Name, dayRate, contractedDays, cisRate,
                ForecastRules.HoursToDays(workerSheets.Sum(sheet => sheet.Hours)),
                workerAbsences.Sum(absence => ForecastRules.AbsenceDeductionDays((AbsenceKind)absence.Kind)),
                projected, due, days.OrderBy(day => day.Date).ToList(), workerSignOffs));
        }

        // Recorded £ per site / cost code: approved rows carry their snapshot; submitted rows are
        // valued at the worker's current rate so the view is live without touching actuals.
        var ratesByWorker = workers.ToDictionary(worker => worker.WorkerId, worker => worker.HourlyRate);
        decimal RecordedCost(Data.Entities.TimesheetEntity sheet) =>
            sheet.Status == (int)TimesheetStatus.Approved
                ? sheet.CostAmount
                : decimal.Round(sheet.Hours * (ratesByWorker.TryGetValue(sheet.WorkerId, out var rate) ? rate : 0m), 2);

        var sites = timesheets.GroupBy(sheet => sheet.ProjectId)
            .Select(group => new LabourOverviewSite(group.Key,
                projectNames.TryGetValue(group.Key, out var name) ? name : group.Key,
                ForecastRules.HoursToDays(group.Sum(sheet => sheet.Hours)),
                group.Sum(RecordedCost)))
            .OrderByDescending(site => site.CostRecorded).ToList();

        var costCodes = timesheets.GroupBy(sheet => sheet.CostCode)
            .Select(group => new LabourOverviewCostCode(group.Key,
                costCentreNames.TryGetValue(group.Key, out var trade) ? trade : "",
                ForecastRules.HoursToDays(group.Sum(sheet => sheet.Hours)),
                group.Sum(RecordedCost)))
            .OrderByDescending(code => code.CostRecorded).ToList();

        var weeks = weekBuckets.OrderBy(pair => pair.Key)
            .Select(pair => new LabourWeekConfidence(new DateTimeOffset(pair.Key, TimeSpan.Zero),
                pair.Value.Elapsed, pair.Value.Confirmed, pair.Value.Unconfirmed))
            .ToList();

        var totals = new LabourOverviewTotals(projectedSpend, timeOffCost, amountDueTotal,
            elapsedWorkerDays, confirmedWorkerDays, unconfirmedCost, weeks);

        return new LabourOverviewSnapshot(query.Year, query.Month, totals, workerRows, sites, costCodes,
            chase.OrderBy(item => item.Date).ThenBy(item => item.WorkerName).ToList(),
            // Only dismissals that actually suppressed an expected day — a row later superseded
            // by a timesheet/absence, or made moot by sign-off, is not counted as a decision in
            // force this month.
            DismissedThisMonth: appliedDismissals);
    }
}

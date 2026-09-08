using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Contracts.Labour;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The labour month-end read surface (2026-08-31, the accountant's ask): the settlement schedule
/// for a month (per-worker verdict, FullySignedOff, the schedule's own totals — the state to
/// check before and after run_xero_coding), the cross-project worker-month view (this month's
/// run meant sweeping thirty week-views to find sixteen days), and the effective-dated Xero
/// mappings (so a gap the coding run reports can be fixed without opening the portal). Every
/// tool wraps the query handler its endpoint composes — or reads the same tables view_labour_week
/// reads — and mirrors that endpoint's role gate exactly.
/// </summary>
internal static class AiLabourMonthEndTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    public static IReadOnlyList<AiTool> Build()
    {
        return new List<AiTool>
        {
            new(
                "view_settlement_month",
                "Every worker's settlement schedule for one month with its reconciliation verdict — the "
                + "Labour overview's settlement view, and the state to read BEFORE and AFTER run_xero_coding. "
                + "Per worker: the schedule lines (site × cost code × nature × amount — exactly what the "
                + "coding run will write to Xero), gross/CIS/net totals, the covered bill total and "
                + "difference, the verdict (Matches / VarianceOpen / NoBillYet / Nothing), FullySignedOff "
                + "(the coding run refuses a worker-month that is not), and the last coding run's outcome. "
                + "Plus the month's chase counts: invoices to chase, workers to reconcile.",
                AiToolSchema.Object(
                    ("year", "number", "The settlement year, e.g. 2026. Left out, the current month's year.", false),
                    ("month", "number", "The settlement month 1-12. Left out, the current month.", false)),
                AiToolKind.Read,
                // Mirrors GetSettlementSchedulesEndpoint.
                LabourRoleSets.ManageSettlement,
                async (context, input, ct) =>
                {
                    var today = SiteClock.Today();
                    var year = (int)(AiToolSchema.Number(input, "year") ?? today.Year);
                    var month = (int)(AiToolSchema.Number(input, "month") ?? today.Month);
                    if (year < 2020 || year > 2100 || month < 1 || month > 12)
                        return Fail("year must be 2020-2100 and month 1-12.");

                    var snapshot = await context.Services
                        .GetRequiredService<IQueryHandler<GetSettlementSchedules, SettlementScheduleSnapshot>>()
                        .HandleAsync(new GetSettlementSchedules(year, month), ct);

                    return Serialise(new
                    {
                        ok = true,
                        snapshot.Year,
                        snapshot.Month,
                        invoicesToChase = snapshot.InvoicesToChase,
                        workersToReconcile = snapshot.WorkersToReconcile,
                        workers = snapshot.Workers.Select(worker => new
                        {
                            worker.WorkerId,
                            worker.WorkerName,
                            worker.SubcontractorId,
                            subcontractorName = string.IsNullOrWhiteSpace(worker.SubcontractorName) ? null : worker.SubcontractorName,
                            verdict = worker.Verdict.ToString(),
                            fullySignedOff = worker.FullySignedOff,
                            grossLabour = worker.GrossLabour,
                            grossOther = worker.GrossOther,
                            grossTotal = worker.GrossTotal,
                            cisRatePercent = worker.CisRatePercent,
                            cisDeduction = worker.CisDeduction,
                            netPayable = worker.NetPayable,
                            coveredBillTotal = worker.CoveredBillTotal,
                            difference = worker.Difference,
                            lastCodingOutcome = string.IsNullOrWhiteSpace(worker.LastCodingOutcome) ? null : worker.LastCodingOutcome,
                            lastCodedAt = worker.LastCodedAt,
                            coveredBill = worker.CoveredBill is null ? null : new
                            {
                                xeroInvoiceId = worker.CoveredBill.XeroInvoiceId,
                                label = worker.CoveredBill.Label,
                                status = worker.CoveredBill.Status,
                                total = worker.CoveredBill.Total,
                                workerNames = worker.CoveredBill.WorkerNames,
                                isApprovable = worker.CoveredBill.IsApprovable
                            },
                            lines = worker.Lines.Select(line => new
                            {
                                line.ProjectId,
                                line.ProjectName,
                                line.CostCode,
                                nature = line.Nature.ToString(),
                                line.Amount,
                                line.WorkerSettlementLineId
                            })
                        }),
                        note = "The month-end chain: sign_off_labour_week per worker-week → "
                            + "preview_xero_coding (what the run would do, writes nothing) → "
                            + "run_xero_coding (fully signed-off months only). The run's normal path "
                            + "(2026-09-03) is to find the worker's existing bill — covered, or recognised "
                            + "by contact + period, draft OR authorised — recode it to the schedule and "
                            + "re-point the cover itself, so verdict Matches survives the run; it stages a "
                            + "DRAFT only where no bill exists. coveredBill names the bill a worker's "
                            + "month is covered by and its status; once every worker on it reads Matches "
                            + "(isApprovable), approve_labour_bill (confirm-first) takes it DRAFT → "
                            + "AUTHORISED in Xero. add_labour_settlement_variance posts an accepted difference. A "
                            + "verdict of NoBillYet with lastCodingOutcome DraftStaged means the staged "
                            + "draft is awaiting approval in Xero, not a missing invoice; lastCodingOutcome "
                            + "Reset means a person reopened the month for the run."
                    });
                }),

            new(
                "view_worker_month",
                "One worker's whole month across EVERY project — the cross-project view the per-project "
                + "view_labour_week cannot give: each week with its sign-off state, each day's site, hours, "
                + "cost code and status, recorded absences, and month totals. This is how to find a "
                + "worker's unapproved or unsigned days before sign-off without sweeping week-views "
                + "project by project.",
                AiToolSchema.Object(
                    ("workerName", "string", "The worker's name as the user says it, matched against the register.", true),
                    ("year", "number", "The year, e.g. 2026. Left out, the current month's year.", false),
                    ("month", "number", "The month 1-12. Left out, the current month.", false)),
                AiToolKind.Read,
                // Mirrors view_labour_week's gate (ListTimesheetDetailsForProjectEndpoint): all internal
                // roles read hours; £ stripped below unless the caller is on the commercial team.
                JpmsRoleSets.AllInternal,
                async (context, input, ct) =>
                {
                    var workerName = AiToolSchema.Text(input, "workerName");
                    if (string.IsNullOrWhiteSpace(workerName)) return Fail("workerName is required.");

                    var today = SiteClock.Today();
                    var year = (int)(AiToolSchema.Number(input, "year") ?? today.Year);
                    var month = (int)(AiToolSchema.Number(input, "month") ?? today.Month);
                    if (year < 2020 || year > 2100 || month < 1 || month > 12)
                        return Fail("year must be 2020-2100 and month 1-12.");

                    var workers = await context.Db.Workers.AsNoTracking().ToListAsync(ct);
                    Data.Entities.WorkerEntity worker;
                    try
                    {
                        worker = WorkerNameResolver.Resolve(workers, workerName, "viewing their month");
                    }
                    catch (InvalidOperationException miss)
                    {
                        return Fail(miss.Message);
                    }

                    var monthStart = new DateTimeOffset(new DateTime(year, month, 1), TimeSpan.Zero);
                    var monthEnd = monthStart.AddMonths(1);

                    var sheets = await context.Db.Timesheets.AsNoTracking()
                        .Where(row => row.WorkerId == worker.WorkerId
                                      && row.WorkedOn >= monthStart && row.WorkedOn < monthEnd)
                        .OrderBy(row => row.WorkedOn)
                        .ToListAsync(ct);
                    var absences = await context.Db.WorkerAbsences.AsNoTracking()
                        .Where(row => row.WorkerId == worker.WorkerId
                                      && row.Date >= monthStart && row.Date < monthEnd)
                        .ToListAsync(ct);
                    // This month's markers only: sign-off is per month part of a week
                    // (2026-09-02), so a week straddling the month end reports the state of
                    // ITS part here and the other part on the neighbouring month.
                    var signOffs = await context.Db.LabourWeekSignOffs.AsNoTracking()
                        .Where(row => row.WorkerId == worker.WorkerId && row.MonthStart == monthStart)
                        .ToListAsync(ct);
                    var projectIds = sheets.Select(row => row.ProjectId).Distinct().ToList();
                    var projects = await context.Db.Projects.AsNoTracking()
                        .Where(row => projectIds.Contains(row.ProjectId))
                        .ToDictionaryAsync(row => row.ProjectId, row => row.Reference, ct);

                    // Same rule as view_labour_week and its backing endpoint: hours for all
                    // internal roles, £ only for the commercial team.
                    var includeMoney = JpmsRoleSets.CommercialTeam.IncludesAny(context.User.Roles);

                    DateTimeOffset WeekOf(DateTimeOffset date)
                    {
                        var day = SiteClock.WorkDateOf(date);
                        return day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
                    }

                    var signedWeeks = signOffs.ToDictionary(row => WeekOf(row.WeekStart), row => row);
                    string? MonthPartOf(DateTimeOffset weekStart)
                    {
                        var week = weekStart.UtcDateTime.Date;
                        if (!ForecastRules.WeekStraddlesMonthEnd(week)) return null;
                        var (first, last) = ForecastRules.WeekPart(week, monthStart.UtcDateTime.Date);
                        return first == last ? $"{first:%d} {first:MMM}" : $"{first:%d}–{last:%d} {last:MMM}";
                    }
                    var weeks = sheets
                        .GroupBy(row => WeekOf(row.WorkedOn))
                        .OrderBy(group => group.Key)
                        .Select(group => new
                        {
                            weekStart = group.Key.ToString("yyyy-MM-dd"),
                            // Present only for a week that straddles the month end: the days of
                            // it that belong to THIS month, which is what its sign-off covers.
                            monthPart = MonthPartOf(group.Key),
                            signedOff = signedWeeks.ContainsKey(group.Key),
                            signedOffBy = signedWeeks.TryGetValue(group.Key, out var marker)
                                ? (string.IsNullOrWhiteSpace(marker.SignedOffByEmail) ? null : marker.SignedOffByEmail)
                                : null,
                            days = group.OrderBy(row => row.WorkedOn).Select(row => new
                            {
                                date = row.WorkedOn.ToString("yyyy-MM-dd"),
                                day = row.WorkedOn.ToString("ddd"),
                                project = projects.TryGetValue(row.ProjectId, out var reference) ? reference : row.ProjectId,
                                projectId = row.ProjectId,
                                hours = row.Hours,
                                costCode = string.IsNullOrWhiteSpace(row.CostCode) ? "uncoded" : row.CostCode,
                                status = ((TimesheetStatus)row.Status).ToString(),
                                approvedCost = includeMoney && row.Status == (int)TimesheetStatus.Approved
                                    ? (decimal?)row.CostAmount
                                    : null
                            }).ToList()
                        }).ToList();

                    return Serialise(new
                    {
                        ok = true,
                        worker = new { worker.WorkerId, worker.Name },
                        year,
                        month,
                        includesMoney = includeMoney,
                        totals = new
                        {
                            days = sheets.Count,
                            hours = sheets.Sum(row => row.Hours),
                            submitted = sheets.Count(row => row.Status == (int)TimesheetStatus.Submitted),
                            approved = sheets.Count(row => row.Status == (int)TimesheetStatus.Approved),
                            rejected = sheets.Count(row => row.Status == (int)TimesheetStatus.Rejected),
                            approvedCost = includeMoney
                                ? (decimal?)sheets.Where(row => row.Status == (int)TimesheetStatus.Approved)
                                    .Sum(row => row.CostAmount)
                                : null
                        },
                        weeks,
                        absences = absences.OrderBy(row => row.Date).Select(row => new
                        {
                            date = row.Date.ToString("yyyy-MM-dd"),
                            kind = ((AbsenceKind)row.Kind).ToString(),
                            note = string.IsNullOrWhiteSpace(row.Note) ? null : row.Note
                        }),
                        note = sheets.Count == 0 && absences.Count == 0
                            ? "Nothing recorded for this worker in this month."
                            : "A week signs off (sign_off_labour_week) only when every elapsed day is "
                              + "approved, rejected or covered by an absence — Submitted days here are "
                              + "what stands in the way. Approve per project with approve_worker_week. "
                              + "A week with a monthPart straddles the month end and signs off per month: "
                              + "pass monthStart (any date in this month) to sign THIS month's days only — "
                              + "the other month's days never hold this month's settlement up."
                    });
                }),

            new(
                "get_xero_mappings",
                "Both effective-dated Xero maps the labour coding run codes with: each project's site "
                + "tracking option, and each cost code's tracking option plus per-nature account codes "
                + "(labour / materials / travel). Current rows first, closed rows kept for history. This is "
                + "what to read when run_xero_coding reports a mapping gap — set_site_xero_mapping and "
                + "set_cost_code_xero_mapping fix one from the connector.",
                AiToolSchema.Empty(),
                AiToolKind.Read,
                // Mirrors ListXeroMappingsEndpoint.
                LabourRoleSets.ManageSettlement,
                async (context, _, ct) =>
                {
                    var snapshot = await context.Services
                        .GetRequiredService<IQueryHandler<ListXeroMappings, XeroMappingsSnapshot>>()
                        .HandleAsync(new ListXeroMappings(), ct);

                    object Site(SiteXeroMapping row) => new
                    {
                        row.ProjectId,
                        row.ProjectName,
                        trackingOption = row.XeroTrackingOptionName,
                        effectiveFrom = row.EffectiveFrom,
                        effectiveTo = row.EffectiveTo
                    };
                    object Code(CostCodeXeroMapping row) => new
                    {
                        row.CostCode,
                        trackingOption = string.IsNullOrWhiteSpace(row.XeroTrackingOptionName) ? null : row.XeroTrackingOptionName,
                        labourAccountCode = string.IsNullOrWhiteSpace(row.LabourAccountCode) ? null : row.LabourAccountCode,
                        materialsAccountCode = string.IsNullOrWhiteSpace(row.MaterialsAccountCode) ? null : row.MaterialsAccountCode,
                        travelAccountCode = string.IsNullOrWhiteSpace(row.TravelAccountCode) ? null : row.TravelAccountCode,
                        effectiveFrom = row.EffectiveFrom,
                        effectiveTo = row.EffectiveTo
                    };

                    return Serialise(new
                    {
                        ok = true,
                        sites = new
                        {
                            current = snapshot.Sites.Where(row => row.EffectiveTo is null).Select(Site),
                            closed = snapshot.Sites.Where(row => row.EffectiveTo is not null).Select(Site)
                        },
                        costCodes = new
                        {
                            current = snapshot.CostCodes.Where(row => row.EffectiveTo is null).Select(Code),
                            closed = snapshot.CostCodes.Where(row => row.EffectiveTo is not null).Select(Code)
                        },
                        note = "Mappings are effective-dated bridges: setting one CLOSES the open row and "
                            + "starts a new one, never edits — historic reads keep translating through the "
                            + "closed rows. A cost code with a blank tracking option codes under its own "
                            + "code name; a blank account code for a nature in use makes the coding run "
                            + "skip that worker until it is set."
                    });
                }),

            new(
                "view_labour_chase",
                "The Labour overview's chase list for one month: every worker-day still awaiting a "
                + "timesheet or an absence — AFTER the expectation test (only days the worker was "
                + "contracted, assigned to a project, or held an open sign-in, inside their engagement "
                + "window) and after suppressing signed-off weeks, settled worker-months and dismissed "
                + "days. Also the month's dismissed count. This is what to read before dismissing "
                + "(dismiss_labour_chase_day) or recording absences to close a month out.",
                AiToolSchema.Object(
                    ("year", "number", "The year, e.g. 2026. Left out, the current month's year.", false),
                    ("month", "number", "The month 1-12. Left out, the current month.", false)),
                AiToolKind.Read,
                // Mirrors GetLabourOverviewEndpoint's gate (rates and £ ride on the overview; the
                // chase items themselves carry no money, but the read stays with the managing roles).
                LabourRoleSets.ManageWorkers,
                async (context, input, ct) =>
                {
                    var today = SiteClock.Today();
                    var year = (int)(AiToolSchema.Number(input, "year") ?? today.Year);
                    var month = (int)(AiToolSchema.Number(input, "month") ?? today.Month);
                    if (year < 2020 || year > 2100 || month < 1 || month > 12)
                        return Fail("year must be 2020-2100 and month 1-12.");

                    var snapshot = await context.Services
                        .GetRequiredService<IQueryHandler<GetLabourOverview, LabourOverviewSnapshot>>()
                        .HandleAsync(new GetLabourOverview(year, month), ct);

                    return Serialise(new
                    {
                        ok = true,
                        snapshot.Year,
                        snapshot.Month,
                        count = snapshot.Chase.Count,
                        dismissedThisMonth = snapshot.DismissedThisMonth,
                        chase = snapshot.Chase.Select(item => new
                        {
                            item.WorkerName,
                            date = item.Date.ToString("yyyy-MM-dd"),
                            reason = item.Reason.ToString(),
                            project = string.IsNullOrWhiteSpace(item.ProjectName) ? null : item.ProjectName
                        }),
                        note = snapshot.Chase.Count == 0
                            ? "Nothing to chase — every expected day is answered for."
                            : "NoTimesheet days need a timesheet (submit_worker_week), an absence "
                              + "(record_worker_absence), or a reasoned dismissal "
                              + "(dismiss_labour_chase_day). OpenAttendance days have a sign-in with no "
                              + "sign-out — the site closes those. A worker chased every day usually "
                              + "needs the real fix: contracted days, a project assignment, or "
                              + "engagement dates."
                    });
                }),
        };
    }
}

using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Features.Labour;

namespace Jewel.JPMS.Services;

public sealed class HttpLabourStore : ILabourStore
{
    private readonly WorkersReadModel workersReadModel;
    private readonly WorkerAssignmentsReadModel assignmentsReadModel;
    private readonly LabourTimesheetsReadModel timesheetsReadModel;
    private readonly SiteAttendanceReadModel attendanceReadModel;
    private readonly MyLabourDayReadModel myDayReadModel;
    private readonly LabourSettlementReadModel settlementReadModel;
    private readonly LabourOverviewReadModel overviewReadModel;
    private readonly SettlementSchedulesReadModel schedulesReadModel;
    private readonly XeroMappingsReadModel xeroMappingsReadModel;
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    // Keys with a load already started — an empty result must not re-trigger a fetch on every
    // re-render (see HttpDrawingStore / CLAUDE.md front-end data-loading convention).
    private bool workersRequested;
    private readonly HashSet<string> assignmentsRequested = new();
    private readonly HashSet<string> timesheetsRequested = new();
    private readonly HashSet<string> attendanceRequested = new();
    private bool myDayRequested;
    private readonly HashSet<string> settlementRequested = new();
    private readonly HashSet<string> overviewRequested = new();
    private readonly HashSet<string> schedulesRequested = new();
    private bool mappingsRequested;

    public HttpLabourStore(WorkersReadModel workersReadModel, WorkerAssignmentsReadModel assignmentsReadModel,
        LabourTimesheetsReadModel timesheetsReadModel, SiteAttendanceReadModel attendanceReadModel,
        MyLabourDayReadModel myDayReadModel, LabourSettlementReadModel settlementReadModel,
        LabourOverviewReadModel overviewReadModel,
        SettlementSchedulesReadModel schedulesReadModel, XeroMappingsReadModel xeroMappingsReadModel,
        IQueryClient queries, ICommandSender commands)
    {
        this.workersReadModel = workersReadModel;
        this.assignmentsReadModel = assignmentsReadModel;
        this.timesheetsReadModel = timesheetsReadModel;
        this.attendanceReadModel = attendanceReadModel;
        this.myDayReadModel = myDayReadModel;
        this.settlementReadModel = settlementReadModel;
        this.overviewReadModel = overviewReadModel;
        this.schedulesReadModel = schedulesReadModel;
        this.xeroMappingsReadModel = xeroMappingsReadModel;
        this.queries = queries;
        this.commands = commands;
        workersReadModel.OnChanged += () => OnChange?.Invoke();
        assignmentsReadModel.OnChanged += () => OnChange?.Invoke();
        timesheetsReadModel.OnChanged += () => OnChange?.Invoke();
        attendanceReadModel.OnChanged += () => OnChange?.Invoke();
        myDayReadModel.OnChanged += () => OnChange?.Invoke();
        settlementReadModel.OnChanged += () => OnChange?.Invoke();
        overviewReadModel.OnChanged += () => OnChange?.Invoke();
        schedulesReadModel.OnChanged += () => OnChange?.Invoke();
        xeroMappingsReadModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<Worker> Workers()
    {
        if (!workersRequested) { workersRequested = true; _ = LoadWorkersAsync(); }
        return workersReadModel.Current;
    }

    private async Task LoadWorkersAsync()
    {
        try { await workersReadModel.RefreshAsync(CancellationToken.None); }
        catch { workersRequested = false; }
    }

    public bool WorkersLoaded => workersReadModel.IsLoaded;

    public Task RefreshWorkersAsync() => workersReadModel.RefreshAsync(CancellationToken.None);

    public async Task<Worker> AddWorkerAsync(string name, decimal hourlyRate, string? subcontractorId, string contactEmail, string contactPhone,
        bool isSoleTrader = false, DateTimeOffset? engagedFrom = null, DateTimeOffset? engagedTo = null)
    {
        var worker = await commands.SendAsync(new AddWorker(name, hourlyRate, subcontractorId, contactEmail, contactPhone,
            isSoleTrader, engagedFrom, engagedTo), CancellationToken.None);
        await workersReadModel.RefreshAsync(CancellationToken.None);
        return worker;
    }

    public async Task<Worker> UpdateWorkerAsync(Worker worker)
    {
        var updated = await commands.SendAsync(new UpdateWorker(worker.WorkerId, worker.Name, worker.HourlyRate,
            worker.IsActive, worker.SubcontractorId, worker.ContactEmail, worker.ContactPhone,
            worker.IsSoleTrader, worker.EngagedFrom, worker.EngagedTo), CancellationToken.None);
        await workersReadModel.RefreshAsync(CancellationToken.None);
        return updated;
    }

    public async Task<Worker> SetWorkerSettlementIdentityAsync(string workerId, string? subcontractorId, bool isSoleTrader)
    {
        var updated = await commands.SendAsync(
            new SetWorkerSettlementIdentity(workerId, subcontractorId, isSoleTrader), CancellationToken.None);
        await workersReadModel.RefreshAsync(CancellationToken.None);
        return updated;
    }

    public async Task<WorkerDirectoryLinkReport> ReconcileWorkerLinksAsync(bool apply)
    {
        var report = await commands.SendAsync(new ReconcileWorkerDirectoryLinks(apply), CancellationToken.None);
        if (apply) await workersReadModel.RefreshAsync(CancellationToken.None);
        return report;
    }

    public Task DismissChaseDayAsync(string workerId, DateTimeOffset date, string reason) =>
        commands.SendAsync(new DismissLabourChaseDay(workerId, date, reason), CancellationToken.None);

    public async Task DeleteWorkerAsync(string workerId)
    {
        await commands.SendAsync(new DeleteWorker(workerId), CancellationToken.None);
        await workersReadModel.RefreshAsync(CancellationToken.None);
    }

    public IReadOnlyList<ProjectWorkerAssignment> AssignmentsFor(string projectId)
    {
        if (assignmentsRequested.Add(projectId)) _ = LoadAsync(() => assignmentsReadModel.RefreshAsync(projectId, CancellationToken.None), assignmentsRequested, projectId);
        return assignmentsReadModel.Current(projectId);
    }

    public bool AssignmentsLoadedFor(string projectId) => assignmentsReadModel.LoadedFor(projectId);

    public Task RefreshAssignmentsAsync(string projectId) => assignmentsReadModel.RefreshAsync(projectId, CancellationToken.None);

    public async Task SetAssignmentAsync(string projectId, string workerId, bool isActive)
    {
        await commands.SendAsync(new SetProjectWorkerAssignment(projectId, workerId, isActive), CancellationToken.None);
        await assignmentsReadModel.RefreshAsync(projectId, CancellationToken.None);
    }

    public MyLabourDay? MyDay()
    {
        if (!myDayRequested) { myDayRequested = true; _ = LoadMyDayAsync(); }
        return myDayReadModel.Current;
    }

    private async Task LoadMyDayAsync()
    {
        try { await myDayReadModel.RefreshAsync(CancellationToken.None); }
        catch { myDayRequested = false; }
    }

    public Task RefreshMyDayAsync() => myDayReadModel.RefreshAsync(CancellationToken.None);

    public async Task MySignInAsync(string projectId)
    {
        await commands.SendAsync(new MySiteSignIn(projectId), CancellationToken.None);
        await myDayReadModel.RefreshAsync(CancellationToken.None);
    }

    public async Task MySignOutAsync(string projectId, IReadOnlyList<SiteSignOutEntry> entries)
    {
        await commands.SendAsync(new MySiteSignOut(projectId, entries), CancellationToken.None);
        await myDayReadModel.RefreshAsync(CancellationToken.None);
    }

    public async Task MyResubmitAsync(string timesheetId, decimal hours, string costCode)
    {
        await commands.SendAsync(new MyResubmitTimesheet(timesheetId, hours, costCode), CancellationToken.None);
        await myDayReadModel.RefreshAsync(CancellationToken.None);
    }

    public IReadOnlyList<TimesheetDetail> TimesheetsFor(string projectId)
    {
        if (timesheetsRequested.Add(projectId)) _ = LoadAsync(() => timesheetsReadModel.RefreshAsync(projectId, CancellationToken.None), timesheetsRequested, projectId);
        return timesheetsReadModel.Current(projectId);
    }

    public bool TimesheetsLoadedFor(string projectId) => timesheetsReadModel.LoadedFor(projectId);

    public Task RefreshTimesheetsAsync(string projectId) => timesheetsReadModel.RefreshAsync(projectId, CancellationToken.None);

    public IReadOnlyList<SiteAttendance> AttendanceFor(string projectId)
    {
        if (attendanceRequested.Add(projectId)) _ = LoadAsync(() => attendanceReadModel.RefreshAsync(projectId, CancellationToken.None), attendanceRequested, projectId);
        return attendanceReadModel.Current(projectId);
    }

    public bool AttendanceLoadedFor(string projectId) => attendanceReadModel.LoadedFor(projectId);

    public Task RefreshAttendanceAsync(string projectId) => attendanceReadModel.RefreshAsync(projectId, CancellationToken.None);

    public async Task<TimesheetDetail> AddWorkerTimesheetAsync(string projectId, string workerId, DateTimeOffset workedOn, decimal hours, string costCode)
    {
        var added = await commands.SendAsync(new AddWorkerTimesheet(projectId, workerId, workedOn, hours, costCode), CancellationToken.None);
        await timesheetsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return added;
    }

    public async Task<WorkerWeekResult> SubmitWorkerWeekAsync(string workerId, DateTimeOffset weekStart, IReadOnlyList<WorkerWeekDayEntry> days)
    {
        var result = await commands.SendAsync(new SubmitWorkerWeek(workerId, weekStart, days), CancellationToken.None);
        // A week can straddle two months — refresh the overview for every month it touches, but
        // only ones already fetched (a refresh of a month nobody is looking at is a wasted query).
        var months = days.Select(day => (day.Date.Year, day.Date.Month)).Distinct();
        foreach (var (year, month) in months)
            if (overviewReadModel.LoadedFor(year, month))
                await overviewReadModel.RefreshAsync(year, month, CancellationToken.None);
        return result;
    }

    public async Task<TimesheetDetail> AdjustTimesheetAsync(string projectId, string timesheetId, decimal hours, string costCode)
    {
        var adjusted = await commands.SendAsync(new AdjustTimesheet(timesheetId, hours, costCode), CancellationToken.None);
        await timesheetsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return adjusted;
    }

    public async Task<LabourApprovalResult> ApproveTimesheetsAsync(string projectId, IReadOnlyList<string> timesheetIds,
        bool allowOverBudget = false, string overBudgetReason = "")
    {
        var result = await commands.SendAsync(
            new ApproveTimesheets(projectId, timesheetIds, allowOverBudget, overBudgetReason), CancellationToken.None);
        await timesheetsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return result;
    }

    public async Task<TimesheetDetail> RejectTimesheetAsync(string projectId, string timesheetId, string reason)
    {
        var rejected = await commands.SendAsync(new RejectTimesheet(timesheetId, reason), CancellationToken.None);
        await timesheetsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return rejected;
    }

    public async Task<TimesheetDetail> UnapproveTimesheetAsync(string projectId, string timesheetId, string reason)
    {
        var reversed = await commands.SendAsync(new UnapproveTimesheet(timesheetId, reason), CancellationToken.None);
        await timesheetsReadModel.RefreshAsync(projectId, CancellationToken.None);
        // The settlement view counts approved £ — it just lost some.
        if (settlementReadModel.LoadedFor(projectId)) await settlementReadModel.RefreshAsync(projectId, CancellationToken.None);
        return reversed;
    }

    public async Task<TimesheetMoveResult> MoveTimesheetAsync(string projectId, string timesheetId, string toProjectId, string reason,
        bool allowOverBudget = false)
    {
        var result = await commands.SendAsync(new MoveTimesheet(timesheetId, toProjectId, reason, allowOverBudget), CancellationToken.None);
        if (!result.Moved) return result;
        // The row has left this project's grid (and register, when it came from a sign-in); the
        // destination's read models refresh on their own next visit (stale-while-revalidate).
        await timesheetsReadModel.RefreshAsync(projectId, CancellationToken.None);
        if (attendanceReadModel.LoadedFor(projectId)) await attendanceReadModel.RefreshAsync(projectId, CancellationToken.None);
        if (settlementReadModel.LoadedFor(projectId)) await settlementReadModel.RefreshAsync(projectId, CancellationToken.None);
        return result;
    }

    public LabourOverviewSnapshot? Overview(int year, int month)
    {
        var key = $"{year:0000}-{month:00}";
        if (overviewRequested.Add(key)) _ = LoadAsync(() => overviewReadModel.RefreshAsync(year, month, CancellationToken.None), overviewRequested, key);
        return overviewReadModel.Current(year, month);
    }

    public bool OverviewLoadedFor(int year, int month) => overviewReadModel.LoadedFor(year, month);

    public Task RefreshOverviewAsync(int year, int month) => overviewReadModel.RefreshAsync(year, month, CancellationToken.None);

    public async Task SetWorkerContractAsync(int year, int month, string workerId, decimal contractedDaysPerMonth)
    {
        await commands.SendAsync(new SetWorkerContract(workerId, contractedDaysPerMonth), CancellationToken.None);
        await overviewReadModel.RefreshAsync(year, month, CancellationToken.None);
    }

    public async Task SetWorkerCisStatusAsync(int year, int month, string workerId, decimal cisRatePercent, string verifiedRef)
    {
        await commands.SendAsync(new SetWorkerCisStatus(workerId, cisRatePercent, verifiedRef), CancellationToken.None);
        await overviewReadModel.RefreshAsync(year, month, CancellationToken.None);
    }

    public async Task RecordAbsenceAsync(int year, int month, string workerId, DateTimeOffset date, AbsenceKind kind, string note)
    {
        await commands.SendAsync(new RecordWorkerAbsence(workerId, date, kind, note), CancellationToken.None);
        await overviewReadModel.RefreshAsync(year, month, CancellationToken.None);
    }

    public async Task<IReadOnlyList<DateTime>> RecordAbsenceRangeAsync(int year, int month, string workerId,
        DateTime from, DateTime to, AbsenceKind kind, string note)
    {
        var start = from.Date;
        var end = to.Date < start ? start : to.Date;
        var failedDates = new List<DateTime>();
        var recordedAny = false;
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            // A multi-day range means "the working week off" — weekends are not workdays here
            // (forecast, sign-off and chase are all Mon–Fri), so they are skipped rather than
            // filled with absences that would deduct days the forecast never counted.
            if (end > start && date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            try
            {
                await commands.SendAsync(new RecordWorkerAbsence(workerId,
                    new DateTimeOffset(date, TimeSpan.Zero), kind, note), CancellationToken.None);
                recordedAny = true;
            }
            catch (Exception) { failedDates.Add(date); }
        }
        if (recordedAny) await overviewReadModel.RefreshAsync(year, month, CancellationToken.None);
        return failedDates;
    }

    public async Task RemoveAbsenceAsync(int year, int month, string workerAbsenceId)
    {
        await commands.SendAsync(new RemoveWorkerAbsence(workerAbsenceId), CancellationToken.None);
        await overviewReadModel.RefreshAsync(year, month, CancellationToken.None);
    }

    // The month in view is the month whose part of the week is signed (2026-09-02) — a week
    // straddling the month end is two markers, one per month, addressed by MonthStart.
    public async Task SignOffWeekAsync(int year, int month, string workerId, DateTimeOffset weekStart)
    {
        await commands.SendAsync(new SignOffLabourWeek(workerId, weekStart, MonthStartOf(year, month)), CancellationToken.None);
        await overviewReadModel.RefreshAsync(year, month, CancellationToken.None);
    }

    public async Task RemoveWeekSignOffAsync(int year, int month, string workerId, DateTimeOffset weekStart)
    {
        await commands.SendAsync(new RemoveLabourWeekSignOff(workerId, weekStart, MonthStartOf(year, month)), CancellationToken.None);
        await overviewReadModel.RefreshAsync(year, month, CancellationToken.None);
    }

    private static DateTimeOffset MonthStartOf(int year, int month) =>
        new(new DateTime(year, month, 1), TimeSpan.Zero);

    public SettlementScheduleSnapshot? Schedules(int year, int month)
    {
        var key = $"{year:0000}-{month:00}";
        if (schedulesRequested.Add(key)) _ = LoadAsync(() => schedulesReadModel.RefreshAsync(year, month, CancellationToken.None), schedulesRequested, key);
        return schedulesReadModel.Current(year, month);
    }

    public bool SchedulesLoadedFor(int year, int month) => schedulesReadModel.LoadedFor(year, month);

    public Task RefreshSchedulesAsync(int year, int month) => schedulesReadModel.RefreshAsync(year, month, CancellationToken.None);

    public async Task AddSettlementLineAsync(int year, int month, string workerId, string projectId, string costCode, SettlementLineNature nature, decimal amount, string note)
    {
        await commands.SendAsync(new AddWorkerSettlementLine(workerId, year, month, projectId, costCode, nature, amount, note), CancellationToken.None);
        await schedulesReadModel.RefreshAsync(year, month, CancellationToken.None);
    }

    public async Task RemoveSettlementLineAsync(int year, int month, string workerSettlementLineId)
    {
        await commands.SendAsync(new RemoveWorkerSettlementLine(workerSettlementLineId), CancellationToken.None);
        await schedulesReadModel.RefreshAsync(year, month, CancellationToken.None);
    }

    public XeroMappingsSnapshot? XeroMappings()
    {
        if (!mappingsRequested) { mappingsRequested = true; _ = LoadMappingsAsync(); }
        return xeroMappingsReadModel.Current;
    }

    private async Task LoadMappingsAsync()
    {
        try { await xeroMappingsReadModel.RefreshAsync(CancellationToken.None); }
        catch { mappingsRequested = false; }
    }

    public Task RefreshXeroMappingsAsync() => xeroMappingsReadModel.RefreshAsync(CancellationToken.None);

    public async Task SetSiteXeroMappingAsync(string projectId, string optionId, string optionName)
    {
        await commands.SendAsync(new SetSiteXeroMapping(projectId, optionId, optionName), CancellationToken.None);
        await xeroMappingsReadModel.RefreshAsync(CancellationToken.None);
    }

    public async Task SetCostCodeXeroMappingAsync(string costCode, string optionId, string optionName, string labourAccount, string materialsAccount, string travelAccount)
    {
        await commands.SendAsync(new SetCostCodeXeroMapping(costCode, optionId, optionName, labourAccount, materialsAccount, travelAccount), CancellationToken.None);
        await xeroMappingsReadModel.RefreshAsync(CancellationToken.None);
    }

    public async Task<IReadOnlyList<XeroCodingRunResult>> RunXeroCodingAsync(int year, int month, IReadOnlyList<string>? workerIds, bool dryRun = false)
    {
        var results = await commands.SendAsync(new RunXeroCoding(year, month, workerIds, dryRun), CancellationToken.None);
        // A dry run changes nothing — the schedules on screen are still right.
        if (!dryRun) await schedulesReadModel.RefreshAsync(year, month, CancellationToken.None);
        return results;
    }

    public async Task ResetXeroCodingOutcomeAsync(int year, int month, string workerId, string reason)
    {
        await commands.SendAsync(new ResetXeroCodingOutcome(workerId, year, month, reason), CancellationToken.None);
        await schedulesReadModel.RefreshAsync(year, month, CancellationToken.None);
    }

    public IReadOnlyList<LabourSettlementRow> SettlementFor(string projectId)
    {
        if (settlementRequested.Add(projectId)) _ = LoadAsync(() => settlementReadModel.RefreshAsync(projectId, CancellationToken.None), settlementRequested, projectId);
        return settlementReadModel.Current(projectId);
    }

    public bool SettlementLoadedFor(string projectId) => settlementReadModel.LoadedFor(projectId);

    public Task RefreshSettlementAsync(string projectId) => settlementReadModel.RefreshAsync(projectId, CancellationToken.None);

    public async Task SetTimesheetCoverAsync(string projectId, string xeroLedgerLineId, bool isCovered, string subcontractorId, DateTimeOffset periodStart, DateTimeOffset periodEnd)
    {
        await commands.SendAsync(new SetXeroLineTimesheetCover(xeroLedgerLineId, isCovered, projectId, subcontractorId, periodStart, periodEnd), CancellationToken.None);
        await settlementReadModel.RefreshAsync(projectId, CancellationToken.None);
    }

    // Worker-month cover from the allocation page's Labour section: ProjectId deliberately empty
    // (a labour bill spans whatever projects the worker's month did; the worker-month
    // reconciliation never reads it) and no per-project settlement refresh — the caller re-reads
    // the ledger, which carries the covered flag. Refreshing settlement for "" would fire a
    // malformed per-project route.
    public Task SetTimesheetCoverForMonthAsync(string xeroLedgerLineId, bool isCovered, string subcontractorId, DateTimeOffset periodStart) =>
        commands.SendAsync(new SetXeroLineTimesheetCover(
            xeroLedgerLineId, isCovered, ProjectId: "", subcontractorId, periodStart, periodStart.AddMonths(1)), CancellationToken.None);

    public async Task AddSettlementVarianceAsync(string projectId, string costCode, string subcontractorId, decimal amount, string reason, string? xeroLedgerLineId)
    {
        await commands.SendAsync(new AddLabourSettlementVariance(projectId, costCode, subcontractorId, amount, reason, xeroLedgerLineId), CancellationToken.None);
        await settlementReadModel.RefreshAsync(projectId, CancellationToken.None);
    }

    private static async Task LoadAsync(Func<Task> refresh, HashSet<string> requested, string projectId)
    {
        try { await refresh(); }
        catch { requested.Remove(projectId); }
    }
}

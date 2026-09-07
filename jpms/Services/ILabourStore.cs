using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Services;

/// <summary>
/// Front-end store for labour tracking (docs/Labour-Time-Tracking-Scope.md). Synchronous reads
/// follow the fetch-once-per-key convention (CLAUDE.md): pages call the Refresh methods once
/// from OnInitializedAsync so tab navigation revalidates in the background.
/// </summary>
public interface ILabourStore
{
    event Action? OnChange;

    // Registry (commercial team only — includes rates).
    IReadOnlyList<Worker> Workers();
    /// <summary>False until the registry has landed — the synchronous reads answer with empty
    /// lists in the meantime, which no view can tell apart from a real empty result.</summary>
    bool WorkersLoaded { get; }
    Task RefreshWorkersAsync();
    Task<Worker> AddWorkerAsync(string name, decimal hourlyRate, string? subcontractorId, string contactEmail, string contactPhone,
        bool isSoleTrader = false, DateTimeOffset? engagedFrom = null, DateTimeOffset? engagedTo = null);
    Task<Worker> UpdateWorkerAsync(Worker worker);
    /// <summary>Sets a worker's settlement identity — directory link (null clears) + sole-trader
    /// flag — in one act; the company link wins where both are set (2026-08-31).</summary>
    Task<Worker> SetWorkerSettlementIdentityAsync(string workerId, string? subcontractorId, bool isSoleTrader);
    /// <summary>The worker↔directory matching sweep: apply=false reports, apply=true links the
    /// unambiguous matches (audited) and reports the rest.</summary>
    Task<WorkerDirectoryLinkReport> ReconcileWorkerLinksAsync(bool apply);
    /// <summary>Dismisses one chase-list day with a reason (audited); it leaves the list and the
    /// unconfirmed accrual together.</summary>
    Task DismissChaseDayAsync(string workerId, DateTimeOffset date, string reason);
    Task DeleteWorkerAsync(string workerId);

    // Project assignment.
    IReadOnlyList<ProjectWorkerAssignment> AssignmentsFor(string projectId);
    /// <summary>False until this project's assignments have landed — see <see cref="WorkersLoaded"/>.</summary>
    bool AssignmentsLoadedFor(string projectId);
    Task RefreshAssignmentsAsync(string projectId);
    Task SetAssignmentAsync(string projectId, string workerId, bool isActive);

    // My Day — the signed-in worker's own timesheet surface.
    MyLabourDay? MyDay();
    Task RefreshMyDayAsync();
    Task MySignInAsync(string projectId);
    Task MySignOutAsync(string projectId, IReadOnlyList<SiteSignOutEntry> entries);
    Task MyResubmitAsync(string timesheetId, decimal hours, string costCode);

    // Timesheets + register.
    IReadOnlyList<TimesheetDetail> TimesheetsFor(string projectId);
    /// <summary>False until this project's timesheets have landed — see <see cref="WorkersLoaded"/>.</summary>
    bool TimesheetsLoadedFor(string projectId);
    Task RefreshTimesheetsAsync(string projectId);
    IReadOnlyList<SiteAttendance> AttendanceFor(string projectId);
    /// <summary>False until this project's register has landed — see <see cref="WorkersLoaded"/>.</summary>
    bool AttendanceLoadedFor(string projectId);
    Task RefreshAttendanceAsync(string projectId);

    Task<TimesheetDetail> AddWorkerTimesheetAsync(string projectId, string workerId, DateTimeOffset workedOn, decimal hours, string costCode);
    /// <summary>The accountant's weekly entry: one worker's week of site days in one command.
    /// Refreshes the overview for every month the days touch (a week can straddle two).</summary>
    Task<WorkerWeekResult> SubmitWorkerWeekAsync(string workerId, DateTimeOffset weekStart, IReadOnlyList<WorkerWeekDayEntry> days);
    Task<TimesheetDetail> AdjustTimesheetAsync(string projectId, string timesheetId, decimal hours, string costCode);
    /// <summary>allowOverBudget is the MD/FD-only deliberate override of the budget hard-block
    /// (server-gated; the reason is mandatory and lands on the audit trail per row).</summary>
    Task<LabourApprovalResult> ApproveTimesheetsAsync(string projectId, IReadOnlyList<string> timesheetIds,
        bool allowOverBudget = false, string overBudgetReason = "");
    Task<TimesheetDetail> RejectTimesheetAsync(string projectId, string timesheetId, string reason);
    /// <summary>MD/FD/Admin correction (2026-09-07): puts an approved row back to Submitted,
    /// withdrawing its posted cost; the reason is mandatory and lands on the audit trail.</summary>
    Task<TimesheetDetail> UnapproveTimesheetAsync(string projectId, string timesheetId, string reason);
    /// <summary>MD/FD/Admin correction (2026-09-07): moves a row to another project as it stands.
    /// Moved = false carries the destination's budget refusal; allowOverBudget re-sends past it.</summary>
    Task<TimesheetMoveResult> MoveTimesheetAsync(string projectId, string timesheetId, string toProjectId, string reason,
        bool allowOverBudget = false);

    // Labour overview: the company-wide month view (forecast, placement grid, chase, sign-off).
    LabourOverviewSnapshot? Overview(int year, int month);
    /// <summary>False until this month's overview has landed — see <see cref="WorkersLoaded"/>.</summary>
    bool OverviewLoadedFor(int year, int month);
    Task RefreshOverviewAsync(int year, int month);
    Task SetWorkerContractAsync(int year, int month, string workerId, decimal contractedDaysPerMonth);
    Task SetWorkerCisStatusAsync(int year, int month, string workerId, decimal cisRatePercent, string verifiedRef);
    Task RecordAbsenceAsync(int year, int month, string workerId, DateTimeOffset date, AbsenceKind kind, string note);
    /// <summary>Records the same absence for every day from <paramref name="from"/> to
    /// <paramref name="to"/> inclusive — Mon–Fri only when the range spans more than one day
    /// (a single-day "range" records whatever day was picked, weekends included). One command
    /// per day over the existing endpoint, one overview refresh at the end. Returns the dates
    /// that could not be recorded (already recorded, server refusal) — empty means all landed.</summary>
    Task<IReadOnlyList<DateTime>> RecordAbsenceRangeAsync(int year, int month, string workerId,
        DateTime from, DateTime to, AbsenceKind kind, string note);
    Task RemoveAbsenceAsync(int year, int month, string workerAbsenceId);
    Task SignOffWeekAsync(int year, int month, string workerId, DateTimeOffset weekStart);
    Task RemoveWeekSignOffAsync(int year, int month, string workerId, DateTimeOffset weekStart);

    // Settlement schedules (per worker-month), Xero mappings, and the coding run.
    SettlementScheduleSnapshot? Schedules(int year, int month);
    /// <summary>False until this month's schedules have landed — see <see cref="WorkersLoaded"/>.</summary>
    bool SchedulesLoadedFor(int year, int month);
    Task RefreshSchedulesAsync(int year, int month);
    Task AddSettlementLineAsync(int year, int month, string workerId, string projectId, string costCode, SettlementLineNature nature, decimal amount, string note);
    Task RemoveSettlementLineAsync(int year, int month, string workerSettlementLineId);
    XeroMappingsSnapshot? XeroMappings();
    Task RefreshXeroMappingsAsync();
    Task SetSiteXeroMappingAsync(string projectId, string optionId, string optionName);
    Task SetCostCodeXeroMappingAsync(string costCode, string optionId, string optionName, string labourAccount, string materialsAccount, string travelAccount);
    /// <summary>The §6a run; dryRun previews per worker what it would do and writes nothing
    /// (2026-09-03). A real run refreshes the month's schedules afterwards.</summary>
    Task<IReadOnlyList<XeroCodingRunResult>> RunXeroCodingAsync(int year, int month, IReadOnlyList<string>? workerIds, bool dryRun = false);
    /// <summary>Reopens a worker-month for the run (appends a Reset outcome, reason recorded).</summary>
    Task ResetXeroCodingOutcomeAsync(int year, int month, string workerId, string reason);

    // Settlement reconciliation.
    IReadOnlyList<LabourSettlementRow> SettlementFor(string projectId);
    /// <summary>False until this project's settlement rows have landed — see <see cref="WorkersLoaded"/>.</summary>
    bool SettlementLoadedFor(string projectId);
    Task RefreshSettlementAsync(string projectId);
    Task SetTimesheetCoverAsync(string projectId, string xeroLedgerLineId, bool isCovered, string subcontractorId, DateTimeOffset periodStart, DateTimeOffset periodEnd);
    /// <summary>
    /// The cover mark as the allocation page's Labour section places it: worker-month scoped,
    /// no project — a labour bill spans whatever projects the worker's month did, and the
    /// worker-month reconciliation (schedules, the §6a run) never reads the cover's project.
    /// Unlike <see cref="SetTimesheetCoverAsync"/> it refreshes no per-project settlement read —
    /// the caller re-reads the ledger, which carries the covered flag.
    /// </summary>
    Task SetTimesheetCoverForMonthAsync(string xeroLedgerLineId, bool isCovered, string subcontractorId, DateTimeOffset periodStart);
    Task AddSettlementVarianceAsync(string projectId, string costCode, string subcontractorId, decimal amount, string reason, string? xeroLedgerLineId);
}

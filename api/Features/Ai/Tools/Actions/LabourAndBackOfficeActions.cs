namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>Labour and back-office commands as connector actions. Mirrors the command endpoints
/// under Features/Labour, Features/CostCenters, Features/Rates, Features/Hs,
/// Features/UsefulInformation, Features/Platform and Features/AccessRequests. Most Labour, Xero
/// and Registers command endpoints still gate with inline role checks and have no Authorisation
/// classes, so they cannot be mirrored here — each is recorded in the skip list at the bottom of
/// this file. The Labour exceptions (2026-08-28): AddWorker now has real gate classes its
/// endpoint composes, and the by-name connector-shaped commands SubmitWorkerWeekByName and
/// RecordWorkerAbsenceByName carry their own gates and resolve workers by NAME — built because
/// the only timesheet entry the connector once had was the legacy Commercial SubmitTimesheet
/// (slices deleted 2026-08-28), whose email-and-cost-code schema taught models to demand data
/// the portal does not need.
/// Where an authorisation keeps its role set as a private field, the VisibleTo below replicates
/// the identical roles with RoleSet.Of(...) and a comment names the source; where the set is an
/// accessible internal static (UsefulInformationRoles), it is referenced directly.</summary>
internal sealed partial class LabourAndBackOfficeActions : IAiActionSource
{
    // Replica of AddCostCenterAuthorisation.RolesThatMayManageCostCenters (identical set in
    // ReviseCostCenterAuthorisation).
    private static readonly RoleSet CostCenterManagers =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.Estimator);

    // Replica of AddRateAuthorisation.RolesThatMayEditRates (identical to
    // ReviseRateAuthorisation.RolesThatMayReviseRates).
    private static readonly RoleSet RateEditors =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.Estimator);

    // Replica of LogHsRecordAuthorisation.RolesThatMayLogHsRecords (identical sets in
    // UpdateHsRecordAuthorisation and RecordAttendanceForHsRecordAuthorisation).
    private static readonly RoleSet HsRecordManagers =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager,
            JpmsRoles.HealthAndSafetyLead);

    // Mirror of AdminGate.Allows — Role.Admin or the Finance Director, who is granted the same
    // permissions without holding the Admin identity (see AdminGate).
    private static readonly RoleSet AdminGateRoles =
        RoleSet.Of(Role.Admin, JpmsRoles.FinanceDirector);

    // SubmitAccessRequestAuthorisation checks no roles at all — only that the command's Email is
    // the signed-in user's own email — so the broadest possible set is every portal role.
    private static readonly RoleSet AnySignedInRole =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector,
            JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.SiteManager,
            JpmsRoles.HealthAndSafetyLead, JpmsRoles.OfficeComplianceCoordinator,
            JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing, JpmsRoles.Architect, JpmsRoles.Client,
            JpmsRoles.Subcontractor, JpmsRoles.Foreman, JpmsRoles.SiteOperative,
            JpmsRoles.Accounts);

    public IEnumerable<AiAction> Build() =>
        TimesheetActions()
            .Concat(WorkerLinkActions())
            .Concat(MonthEndActions())
            .Concat(CostCentreActions())
            .Concat(RateActions())
            .Concat(HealthAndSafetyActions())
            .Concat(UsefulInformationActions())
            .Concat(PlatformActions())
            .Concat(AccessRequestActions());

    // (AddLabourSettlementVariance is no longer skipped — gate classes added 2026-08-31 (SettlementCommandGates.cs), action add_labour_settlement_variance above; the command gained a CreatedByEmail stamp parameter the interface HandleAsync now carries, so the gateway path stamps the same actor the endpoint's overload does.)
    // (AddWorker is no longer skipped — gate classes added 2026-08-28, action add_worker above.)
    // Skipped: UpdateWorker — gate classes exist (2026-08-28, endpoint composes them), but the command is keyed by an opaque WorkerId the connector cannot resolve; expose via a by-name wrapper (the SubmitWorkerWeekByName pattern) if a need appears.
    // Skipped: DeleteWorker — no Authorisation class (inline LabourRoleSets.ManageWorkers check only).
    // Skipped: AddWorkerTimesheet — no Authorisation class (inline LabourRoleSets.ApproveTimesheets check only).
    // (AdjustTimesheet is no longer connector-unreachable — the connector codes through CodeWorkerWeekByName (code_worker_week above), which resolves worker name + dates to the week's timesheets and runs AdjustTimesheetHandler per row. The id-keyed AdjustTimesheet itself stays unmirrored: opaque TimesheetId.)
    // (ApproveTimesheets likewise — the connector approves through ApproveWorkerWeekByName (approve_worker_week above, confirm-first), which delegates the resolved ids to ApproveTimesheetsHandler's approvedByEmail overload via the EmailStamps parameter. The id-keyed command stays unmirrored: no Authorisation class, opaque ids. Distinct from the legacy Commercial ApproveTimesheet, whose slices were deleted 2026-08-28.)
    // (RejectTimesheet likewise — reject_worker_day above resolves name + date and runs RejectTimesheetHandler. The id-keyed command stays unmirrored: no Authorisation class, opaque TimesheetId.)
    // (UnapproveTimesheet / MoveTimesheet likewise (2026-09-07) — unapprove_worker_day / move_worker_day resolve name + date and run the same handlers. The id-keyed commands have Authorisation classes (the endpoints compose them) but stay unmirrored: opaque TimesheetId.)
    // Skipped: SubmitWorkerWeek — no Authorisation class (inline LabourRoleSets.ApproveTimesheets check only); the connector enters weeks through SubmitWorkerWeekByName (submit_worker_week above), which delegates to the same handler. (Distinct from the legacy Commercial SubmitTimesheet — action removed from CommercialActions and slices deleted, both 2026-08-28.)
    // Skipped: MySiteSignIn — no Authorisation class (inline LabourRoleSets.LogOwnTime check only).
    // Skipped: MySiteSignOut — no Authorisation class (inline LabourRoleSets.LogOwnTime check only).
    // Skipped: MyResubmitTimesheet — no Authorisation class (inline LabourRoleSets.LogOwnTime check only).
    // (SignOffLabourWeek is no longer connector-unreachable — the connector signs off through SignOffWorkerWeekByName (sign_off_labour_week above, confirm-first), which resolves the worker name and delegates to SignOffLabourWeekHandler's signedOffByEmail overload via the EmailStamps parameter, converting WeekNotSignableException to the gateway's message convention. The id-keyed SignOffLabourWeek itself stays unmirrored: opaque WorkerId.)
    // (RemoveLabourWeekSignOff likewise — remove_labour_week_sign_off above resolves the name and delegates to the registered handler. The id-keyed command stays unmirrored: opaque WorkerId.)
    // Skipped: SetProjectWorkerAssignment — no Authorisation class (inline LabourRoleSets.ManageWorkers check only).
    // Skipped: SetWorkerContract — no Authorisation class (inline LabourRoleSets.ManageWorkers check only).
    // Skipped: SetWorkerCisStatus — no Authorisation class (inline LabourRoleSets.ManageWorkers check only).
    // (RecordWorkerAbsence is no longer skipped — the connector records absences through RecordWorkerAbsenceByName (record_worker_absence above), which delegates to the same handler with the recordedByEmail overload's stamp carried as an EmailStamps parameter; the endpoint gained RecordWorkerAbsenceAuthorisation 2026-08-28.)
    // Skipped: RemoveWorkerAbsence — no Authorisation class (inline LabourRoleSets.ManageWorkers check only).
    // Skipped: AddWorkerSettlementLine — no Authorisation class (inline LabourRoleSets.ManageSettlement check only).
    // Skipped: RemoveWorkerSettlementLine — no Authorisation class (inline LabourRoleSets.ManageSettlement check only).
    // (SetSiteXeroMapping is no longer skipped — gate classes added 2026-08-31 (SettlementCommandGates.cs), action set_site_xero_mapping above.)
    // (SetCostCodeXeroMapping is no longer skipped — gate classes added 2026-08-31, action set_cost_code_xero_mapping above.)
    // (SetXeroLineTimesheetCover is no longer skipped — gate classes added 2026-08-31, action set_xero_line_timesheet_cover above; CreatedByEmail stamp parameter added, interface HandleAsync carries it.)
    // (RunXeroCoding is no longer connector-unreachable — run_xero_coding above delegates through RunXeroCodingByName, which resolves worker names and passes the caller into RunXeroCodingHandler's runByEmail overload. The id-keyed command stays unmirrored: opaque WorkerIds.)
    // Skipped: SyncXeroLedger — no Authorisation class (inline XeroLedgerRoles.AllowedToAllocate check only).
    // Skipped: AllocateSuggestedXeroLines — no Authorisation class (inline XeroLedgerRoles.AllowedToAllocate check only).
    // Skipped: RetryXeroWriteBack — no Authorisation class (inline XeroLedgerRoles.AllowedToAllocate check only).
    // Skipped: SetXeroAllocation — no Authorisation class (inline XeroLedgerRoles.AllowedToAllocate check only).
    // Skipped: SyncXeroSitePnl — no Authorisation class (inline XeroSitePnlRoles.AllowedToView check only).
    // Skipped: SaveRegisterItem — no Authorisation class (inline RegisterRoleSets.ManageRegisters check only).
    // Skipped: DeactivateRegisterItem — no Authorisation class (inline RegisterRoleSets.ManageRegisters check only).
    // Skipped: PublishPolicyDocument — no Authorisation class (inline RegisterRoleSets.ManageRegisters check only).
    // Skipped: SignPolicy — no Authorisation class, and its handler is not a registered ICommandHandler.
    // Skipped: Ping (Features/Health) — no command dispatch; a bare health probe.
}

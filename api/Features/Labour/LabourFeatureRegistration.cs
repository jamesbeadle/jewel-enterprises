using Jewel.JPMS.Api.Features.Labour.Commands;
using Jewel.JPMS.Api.Features.Labour.Queries;
using Jewel.JPMS.Contracts.Labour;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Labour;

public static class LabourFeatureRegistration
{
    public static IServiceCollection AddLabourFeature(this IServiceCollection services)
    {
        // Worker registry + project assignment.
        services.AddScoped<IQueryHandler<ListWorkers, IReadOnlyList<Worker>>, ListWorkersHandler>();
        services.AddScoped<IQueryHandler<ListWorkerAssignmentsForProject, IReadOnlyList<ProjectWorkerAssignment>>, ListWorkerAssignmentsForProjectHandler>();
        services.AddScoped<ICommandHandler<AddWorker, Worker>, AddWorkerHandler>();
        services.AddScoped<AddWorkerAuthorisation>();
        services.AddScoped<AddWorkerValidation>();
        services.AddScoped<ICommandHandler<UpdateWorker, Worker>, UpdateWorkerHandler>();
        services.AddScoped<UpdateWorkerAuthorisation>();
        services.AddScoped<UpdateWorkerValidation>();
        services.AddScoped<ICommandHandler<DeleteWorker, Acknowledgement>, DeleteWorkerHandler>();
        services.AddScoped<ICommandHandler<SetProjectWorkerAssignment, ProjectWorkerAssignment>, SetProjectWorkerAssignmentHandler>();
        // Worker ↔ directory linking (2026-08-31): the settlement-identity command the portal UI
        // posts, its by-name connector wrappers, and the reconcile/backfill sweep.
        services.AddScoped<ICommandHandler<SetWorkerSettlementIdentity, Worker>, SetWorkerSettlementIdentityHandler>();
        services.AddScoped<SetWorkerSettlementIdentityAuthorisation>();
        services.AddScoped<SetWorkerSettlementIdentityValidation>();
        services.AddScoped<ICommandHandler<LinkWorkerToCompanyByName, Worker>, LinkWorkerToCompanyByNameHandler>();
        services.AddScoped<LinkWorkerToCompanyByNameAuthorisation>();
        services.AddScoped<LinkWorkerToCompanyByNameValidation>();
        services.AddScoped<ICommandHandler<SetWorkerSoleTraderByName, Worker>, SetWorkerSoleTraderByNameHandler>();
        services.AddScoped<SetWorkerSoleTraderByNameAuthorisation>();
        services.AddScoped<SetWorkerSoleTraderByNameValidation>();
        services.AddScoped<ReconcileWorkerDirectoryLinksHandler>();
        services.AddScoped<ICommandHandler<ReconcileWorkerDirectoryLinks, WorkerDirectoryLinkReport>>(
            provider => provider.GetRequiredService<ReconcileWorkerDirectoryLinksHandler>());
        services.AddScoped<ReconcileWorkerDirectoryLinksAuthorisation>();
        services.AddScoped<ReconcileWorkerDirectoryLinksValidation>();
        // Chase-list dismissals (2026-08-31): the overview's dismiss command and its by-name
        // connector wrappers.
        services.AddScoped<DismissLabourChaseDayHandler>();
        services.AddScoped<ICommandHandler<DismissLabourChaseDay, Acknowledgement>>(
            provider => provider.GetRequiredService<DismissLabourChaseDayHandler>());
        services.AddScoped<DismissLabourChaseDayAuthorisation>();
        services.AddScoped<DismissLabourChaseDayValidation>();
        services.AddScoped<ICommandHandler<DismissLabourChaseDayByName, Acknowledgement>, DismissLabourChaseDayByNameHandler>();
        services.AddScoped<DismissLabourChaseDayByNameAuthorisation>();
        services.AddScoped<DismissLabourChaseDayByNameValidation>();
        services.AddScoped<ICommandHandler<RestoreLabourChaseDayByName, Acknowledgement>, RestoreLabourChaseDayByNameHandler>();
        services.AddScoped<RestoreLabourChaseDayByNameAuthorisation>();
        services.AddScoped<RestoreLabourChaseDayByNameValidation>();

        // Site register.
        services.AddScoped<IQueryHandler<ListSiteAttendanceForProject, IReadOnlyList<SiteAttendance>>, ListSiteAttendanceForProjectHandler>();

        // My Day — the worker's own authenticated timesheet surface. Handlers are resolved
        // concretely because they need the signed-in email alongside the command.
        services.AddScoped<GetMyLabourDayHandler>();
        services.AddScoped<MySiteSignInHandler>();
        services.AddScoped<MySiteSignOutHandler>();
        services.AddScoped<MyResubmitTimesheetHandler>();

        // Labour tab: week grid, adjust / approve / reject.
        services.AddScoped<ListTimesheetDetailsForProjectHandler>();
        services.AddScoped<IQueryHandler<ListTimesheetDetailsForProject, IReadOnlyList<TimesheetDetail>>>(
            provider => provider.GetRequiredService<ListTimesheetDetailsForProjectHandler>());
        services.AddScoped<ICommandHandler<AdjustTimesheet, TimesheetDetail>, AdjustTimesheetHandler>();
        services.AddScoped<AdjustTimesheetAuthorisation>();
        services.AddScoped<AdjustTimesheetValidation>();
        services.AddScoped<ICommandHandler<AddWorkerTimesheet, TimesheetDetail>, AddWorkerTimesheetHandler>();
        services.AddScoped<SubmitWorkerWeekHandler>();
        services.AddScoped<ICommandHandler<SubmitWorkerWeek, WorkerWeekResult>>(
            provider => provider.GetRequiredService<SubmitWorkerWeekHandler>());
        // Connector week entry (submit_worker_week action): by-name wrapper over the handler above.
        services.AddScoped<ICommandHandler<SubmitWorkerWeekByName, WorkerWeekResult>, SubmitWorkerWeekByNameHandler>();
        services.AddScoped<SubmitWorkerWeekByNameAuthorisation>();
        services.AddScoped<SubmitWorkerWeekByNameValidation>();
        services.AddScoped<ApproveTimesheetsHandler>();
        services.AddScoped<ICommandHandler<ApproveTimesheets, LabourApprovalResult>>(
            provider => provider.GetRequiredService<ApproveTimesheetsHandler>());
        services.AddScoped<ICommandHandler<RejectTimesheet, TimesheetDetail>, RejectTimesheetHandler>();
        // Connector coding/approval (code_worker_week / approve_worker_week / reject_worker_day
        // actions): by-name wrappers over the grid's own handlers above — same gates, same
        // hard-blocks, so the two surfaces cannot drift.
        services.AddScoped<ICommandHandler<CodeWorkerWeekByName, WorkerWeekCodingResult>, CodeWorkerWeekByNameHandler>();
        services.AddScoped<CodeWorkerWeekByNameAuthorisation>();
        services.AddScoped<CodeWorkerWeekByNameValidation>();
        services.AddScoped<ICommandHandler<ApproveWorkerWeekByName, WorkerWeekApprovalResult>, ApproveWorkerWeekByNameHandler>();
        services.AddScoped<ApproveWorkerWeekByNameAuthorisation>();
        services.AddScoped<ApproveWorkerWeekByNameValidation>();
        services.AddScoped<ICommandHandler<RejectWorkerDayByName, TimesheetDetail>, RejectWorkerDayByNameHandler>();
        services.AddScoped<RejectWorkerDayByNameAuthorisation>();
        services.AddScoped<RejectWorkerDayByNameValidation>();
        // Corrections (2026-09-07, the accountant's ask): unapprove puts an approved day back to
        // Submitted (cost withdrawn), move re-homes a day on another project — MD/FD/Admin only,
        // reason mandatory, audited; the by-name wrappers (unapprove_worker_day / move_worker_day)
        // run the same handlers.
        services.AddScoped<UnapproveTimesheetHandler>();
        services.AddScoped<ICommandHandler<UnapproveTimesheet, TimesheetDetail>>(
            provider => provider.GetRequiredService<UnapproveTimesheetHandler>());
        services.AddScoped<UnapproveTimesheetAuthorisation>();
        services.AddScoped<UnapproveTimesheetValidation>();
        services.AddScoped<MoveTimesheetHandler>();
        services.AddScoped<ICommandHandler<MoveTimesheet, TimesheetMoveResult>>(
            provider => provider.GetRequiredService<MoveTimesheetHandler>());
        services.AddScoped<MoveTimesheetAuthorisation>();
        services.AddScoped<MoveTimesheetValidation>();
        services.AddScoped<ICommandHandler<UnapproveWorkerDayByName, TimesheetDetail>, UnapproveWorkerDayByNameHandler>();
        services.AddScoped<UnapproveWorkerDayByNameAuthorisation>();
        services.AddScoped<UnapproveWorkerDayByNameValidation>();
        services.AddScoped<ICommandHandler<MoveWorkerDayByName, TimesheetMoveResult>, MoveWorkerDayByNameHandler>();
        services.AddScoped<MoveWorkerDayByNameAuthorisation>();
        services.AddScoped<MoveWorkerDayByNameValidation>();

        // Labour overview: forecast, placement grid, chase list (scope §4–§5).
        services.AddScoped<IQueryHandler<GetLabourOverview, LabourOverviewSnapshot>, GetLabourOverviewHandler>();
        services.AddScoped<ICommandHandler<SetWorkerContract, Acknowledgement>, SetWorkerContractHandler>();
        services.AddScoped<ICommandHandler<SetWorkerCisStatus, Acknowledgement>, SetWorkerCisStatusHandler>();
        services.AddScoped<RecordWorkerAbsenceHandler>();
        services.AddScoped<RecordWorkerAbsenceAuthorisation>();
        services.AddScoped<ICommandHandler<RecordWorkerAbsence, WorkerAbsence>>(
            provider => provider.GetRequiredService<RecordWorkerAbsenceHandler>());
        // Connector absence entry (record_worker_absence action): by-name wrapper over the handler above.
        services.AddScoped<ICommandHandler<RecordWorkerAbsenceByName, WorkerAbsence>, RecordWorkerAbsenceByNameHandler>();
        services.AddScoped<RecordWorkerAbsenceByNameAuthorisation>();
        services.AddScoped<RecordWorkerAbsenceByNameValidation>();
        services.AddScoped<ICommandHandler<RemoveWorkerAbsence, Acknowledgement>, RemoveWorkerAbsenceHandler>();
        services.AddScoped<SignOffLabourWeekHandler>();
        services.AddScoped<ICommandHandler<SignOffLabourWeek, LabourWeekSignOff>>(
            provider => provider.GetRequiredService<SignOffLabourWeekHandler>());
        services.AddScoped<ICommandHandler<RemoveLabourWeekSignOff, Acknowledgement>, RemoveLabourWeekSignOffHandler>();
        // Connector sign-off (sign_off_labour_week / remove_labour_week_sign_off actions):
        // by-name wrappers over the handlers above — same signable rule, same gates.
        services.AddScoped<ICommandHandler<SignOffWorkerWeekByName, LabourWeekSignOff>, SignOffWorkerWeekByNameHandler>();
        services.AddScoped<SignOffWorkerWeekByNameAuthorisation>();
        services.AddScoped<SignOffWorkerWeekByNameValidation>();
        services.AddScoped<ICommandHandler<RemoveWorkerWeekSignOffByName, Acknowledgement>, RemoveWorkerWeekSignOffByNameHandler>();
        services.AddScoped<RemoveWorkerWeekSignOffByNameAuthorisation>();
        services.AddScoped<RemoveWorkerWeekSignOffByNameValidation>();

        // Settlement schedules, Xero mappings and the §6a coding run.
        services.AddScoped<SettlementScheduleBuilder>();
        services.AddScoped<IQueryHandler<GetSettlementSchedules, SettlementScheduleSnapshot>, GetSettlementSchedulesHandler>();
        services.AddScoped<AddWorkerSettlementLineHandler>();
        services.AddScoped<ICommandHandler<AddWorkerSettlementLine, Acknowledgement>>(
            provider => provider.GetRequiredService<AddWorkerSettlementLineHandler>());
        services.AddScoped<ICommandHandler<RemoveWorkerSettlementLine, Acknowledgement>, RemoveWorkerSettlementLineHandler>();
        services.AddScoped<IQueryHandler<ListXeroMappings, XeroMappingsSnapshot>, ListXeroMappingsHandler>();
        services.AddScoped<ICommandHandler<SetSiteXeroMapping, Acknowledgement>, SetSiteXeroMappingHandler>();
        services.AddScoped<ICommandHandler<SetCostCodeXeroMapping, Acknowledgement>, SetCostCodeXeroMappingHandler>();
        services.AddScoped<RunXeroCodingHandler>();
        services.AddScoped<ICommandHandler<RunXeroCoding, IReadOnlyList<XeroCodingRunResult>>>(
            provider => provider.GetRequiredService<RunXeroCodingHandler>());
        // Connector coding run (run_xero_coding action): by-name wrapper over the runner above.
        services.AddScoped<ICommandHandler<RunXeroCodingByName, XeroCodingRunReport>, RunXeroCodingByNameHandler>();
        services.AddScoped<RunXeroCodingByNameAuthorisation>();
        services.AddScoped<RunXeroCodingByNameValidation>();
        // Dry run (preview_xero_coding) and the coding-outcome reset (2026-09-03, items D + E):
        // the reset has an endpoint of its own; both have by-name wrappers for the connector.
        services.AddScoped<ICommandHandler<PreviewXeroCodingByName, XeroCodingRunReport>, PreviewXeroCodingByNameHandler>();
        services.AddScoped<PreviewXeroCodingByNameAuthorisation>();
        services.AddScoped<PreviewXeroCodingByNameValidation>();
        services.AddScoped<ResetXeroCodingOutcomeHandler>();
        services.AddScoped<ICommandHandler<ResetXeroCodingOutcome, Acknowledgement>>(
            provider => provider.GetRequiredService<ResetXeroCodingOutcomeHandler>());
        services.AddScoped<ICommandHandler<ResetXeroCodingOutcomeByName, Acknowledgement>, ResetXeroCodingOutcomeByNameHandler>();
        services.AddScoped<ResetXeroCodingOutcomeByNameAuthorisation>();
        services.AddScoped<ResetXeroCodingOutcomeByNameValidation>();
        // Approving a covered labour bill (2026-09-08): the Settlement view's button and the
        // approve_labour_bill action share one handler and one pair of gates.
        services.AddScoped<ApproveLabourBillHandler>();
        services.AddScoped<ICommandHandler<ApproveLabourBill, LabourBillApproval>>(
            provider => provider.GetRequiredService<ApproveLabourBillHandler>());
        services.AddScoped<ApproveLabourBillAuthorisation>();
        services.AddScoped<ApproveLabourBillValidation>();
        // Gate classes for the settlement/Xero write cluster (2026-08-31): the endpoints keep
        // their inline checks; these exist so the connector's action gateway composes the same
        // RoleSet constants and argument rules (SettlementCommandGates.cs).
        services.AddScoped<SetSiteXeroMappingAuthorisation>();
        services.AddScoped<SetSiteXeroMappingValidation>();
        services.AddScoped<SetCostCodeXeroMappingAuthorisation>();
        services.AddScoped<SetCostCodeXeroMappingValidation>();

        // Settlement reconciliation.
        services.AddScoped<IQueryHandler<ListLabourSettlementForProject, IReadOnlyList<LabourSettlementRow>>, ListLabourSettlementForProjectHandler>();
        services.AddScoped<SetXeroLineTimesheetCoverHandler>();
        services.AddScoped<ICommandHandler<SetXeroLineTimesheetCover, Acknowledgement>>(
            provider => provider.GetRequiredService<SetXeroLineTimesheetCoverHandler>());
        services.AddScoped<AddLabourSettlementVarianceHandler>();
        services.AddScoped<ICommandHandler<AddLabourSettlementVariance, LabourSettlementVariance>>(
            provider => provider.GetRequiredService<AddLabourSettlementVarianceHandler>());
        services.AddScoped<SetXeroLineTimesheetCoverAuthorisation>();
        services.AddScoped<SetXeroLineTimesheetCoverValidation>();
        services.AddScoped<AddLabourSettlementVarianceAuthorisation>();
        services.AddScoped<AddLabourSettlementVarianceValidation>();

        return services;
    }
}

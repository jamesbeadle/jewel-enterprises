using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Features.Labour;

public static class LabourRouteRegistration
{
    public static IServiceCollection AddLabourReadModels(this IServiceCollection services)
    {
        services.AddScoped<WorkersReadModel>();
        services.AddScoped<WorkerAssignmentsReadModel>();
        services.AddScoped<LabourTimesheetsReadModel>();
        services.AddScoped<SiteAttendanceReadModel>();
        services.AddScoped<MyLabourDayReadModel>();
        services.AddScoped<LabourSettlementReadModel>();
        services.AddScoped<LabourOverviewReadModel>();
        services.AddScoped<SettlementSchedulesReadModel>();
        services.AddScoped<XeroMappingsReadModel>();
        return services;
    }

    public static void RegisterLabourRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListWorkers, IReadOnlyList<Worker>>(
            QueryRoute.Static("/api/labour/workers"));

        queries.Register<ListWorkerAssignmentsForProject, IReadOnlyList<ProjectWorkerAssignment>>(
            new QueryRoute("/api/projects/{projectId}/labour/assignments",
                query => $"/api/projects/{((ListWorkerAssignmentsForProject)query).ProjectId}/labour/assignments"));

        queries.Register<ListTimesheetDetailsForProject, IReadOnlyList<TimesheetDetail>>(
            new QueryRoute("/api/projects/{projectId}/labour/timesheets",
                query => $"/api/projects/{((ListTimesheetDetailsForProject)query).ProjectId}/labour/timesheets"));

        queries.Register<ListSiteAttendanceForProject, IReadOnlyList<SiteAttendance>>(
            new QueryRoute("/api/projects/{projectId}/labour/attendance",
                query => $"/api/projects/{((ListSiteAttendanceForProject)query).ProjectId}/labour/attendance"));

        queries.Register<GetMyLabourDay, MyLabourDay>(
            QueryRoute.Static("/api/my/labour/day"));

        queries.Register<ListLabourSettlementForProject, IReadOnlyList<LabourSettlementRow>>(
            new QueryRoute("/api/projects/{projectId}/labour/settlement",
                query => $"/api/projects/{((ListLabourSettlementForProject)query).ProjectId}/labour/settlement"));

        commands.Register<AddWorker, Worker>(CommandRoute.Post("/api/labour/workers"));

        commands.Register<UpdateWorker, Worker>(
            new CommandRoute("PUT", "/api/labour/workers/{workerId}",
                command => $"/api/labour/workers/{((UpdateWorker)command).WorkerId}"));

        commands.Register<DeleteWorker, Acknowledgement>(
            new CommandRoute("DELETE", "/api/labour/workers/{workerId}",
                command => $"/api/labour/workers/{((DeleteWorker)command).WorkerId}"));

        commands.Register<SetProjectWorkerAssignment, ProjectWorkerAssignment>(
            new CommandRoute("POST", "/api/projects/{projectId}/labour/assignments",
                command => $"/api/projects/{((SetProjectWorkerAssignment)command).ProjectId}/labour/assignments"));

        commands.Register<MySiteSignIn, Acknowledgement>(CommandRoute.Post("/api/my/labour/sign-in"));
        commands.Register<MySiteSignOut, Acknowledgement>(CommandRoute.Post("/api/my/labour/sign-out"));
        commands.Register<MyResubmitTimesheet, Acknowledgement>(CommandRoute.Post("/api/my/labour/resubmit"));

        commands.Register<AddWorkerTimesheet, TimesheetDetail>(
            new CommandRoute("POST", "/api/projects/{projectId}/labour/timesheets",
                command => $"/api/projects/{((AddWorkerTimesheet)command).ProjectId}/labour/timesheets"));

        // The accountant's weekly entry — one worker's week of site days in one command.
        commands.Register<SubmitWorkerWeek, WorkerWeekResult>(
            CommandRoute.Post("/api/labour/weeks/timesheets"));

        commands.Register<AdjustTimesheet, TimesheetDetail>(
            new CommandRoute("PUT", "/api/labour/timesheets/{timesheetId}",
                command => $"/api/labour/timesheets/{((AdjustTimesheet)command).TimesheetId}"));

        commands.Register<ApproveTimesheets, LabourApprovalResult>(
            new CommandRoute("POST", "/api/projects/{projectId}/labour/approvals",
                command => $"/api/projects/{((ApproveTimesheets)command).ProjectId}/labour/approvals"));

        commands.Register<RejectTimesheet, TimesheetDetail>(
            new CommandRoute("POST", "/api/labour/timesheets/{timesheetId}/rejection",
                command => $"/api/labour/timesheets/{((RejectTimesheet)command).TimesheetId}/rejection"));

        // Corrections (2026-09-07): the MD/FD's way back once approval has posted.
        commands.Register<UnapproveTimesheet, TimesheetDetail>(
            new CommandRoute("POST", "/api/labour/timesheets/{timesheetId}/unapproval",
                command => $"/api/labour/timesheets/{((UnapproveTimesheet)command).TimesheetId}/unapproval"));

        commands.Register<MoveTimesheet, TimesheetMoveResult>(
            new CommandRoute("POST", "/api/labour/timesheets/{timesheetId}/move",
                command => $"/api/labour/timesheets/{((MoveTimesheet)command).TimesheetId}/move"));

        commands.Register<SetXeroLineTimesheetCover, Acknowledgement>(
            CommandRoute.Post("/api/labour/timesheet-covers"));

        commands.Register<AddLabourSettlementVariance, LabourSettlementVariance>(
            new CommandRoute("POST", "/api/projects/{projectId}/labour/settlement-variances",
                command => $"/api/projects/{((AddLabourSettlementVariance)command).ProjectId}/labour/settlement-variances"));

        // Labour overview: forecast, absence, weekly sign-off (scope §4–§5).
        queries.Register<GetLabourOverview, LabourOverviewSnapshot>(
            new QueryRoute("/api/labour/overview/{year}/{month}",
                query => $"/api/labour/overview/{((GetLabourOverview)query).Year}/{((GetLabourOverview)query).Month}"));

        commands.Register<SetWorkerContract, Acknowledgement>(CommandRoute.Post("/api/labour/workers/contract"));
        commands.Register<SetWorkerCisStatus, Acknowledgement>(CommandRoute.Post("/api/labour/workers/cis"));
        commands.Register<RecordWorkerAbsence, WorkerAbsence>(CommandRoute.Post("/api/labour/absences"));
        commands.Register<RemoveWorkerAbsence, Acknowledgement>(CommandRoute.Post("/api/labour/absences/remove"));
        commands.Register<SignOffLabourWeek, LabourWeekSignOff>(CommandRoute.Post("/api/labour/weeks/sign-off"));
        commands.Register<RemoveLabourWeekSignOff, Acknowledgement>(CommandRoute.Post("/api/labour/weeks/remove-sign-off"));

        // Settlement schedules, Xero mappings, and the §6a coding run.
        queries.Register<GetSettlementSchedules, SettlementScheduleSnapshot>(
            new QueryRoute("/api/labour/schedules/{year}/{month}",
                query => $"/api/labour/schedules/{((GetSettlementSchedules)query).Year}/{((GetSettlementSchedules)query).Month}"));
        queries.Register<ListXeroMappings, XeroMappingsSnapshot>(
            QueryRoute.Static("/api/labour/xero-mappings"));

        commands.Register<AddWorkerSettlementLine, Acknowledgement>(CommandRoute.Post("/api/labour/settlement-lines"));
        commands.Register<RemoveWorkerSettlementLine, Acknowledgement>(CommandRoute.Post("/api/labour/settlement-lines/remove"));
        commands.Register<SetSiteXeroMapping, Acknowledgement>(CommandRoute.Post("/api/labour/xero-mappings/site"));
        commands.Register<SetCostCodeXeroMapping, Acknowledgement>(CommandRoute.Post("/api/labour/xero-mappings/cost-code"));
        commands.Register<RunXeroCoding, IReadOnlyList<XeroCodingRunResult>>(CommandRoute.Post("/api/labour/xero-coding/run"));
        commands.Register<ResetXeroCodingOutcome, Acknowledgement>(CommandRoute.Post("/api/labour/xero-coding/reset"));
        commands.Register<ApproveLabourBill, LabourBillApproval>(CommandRoute.Post("/api/labour/xero-coding/approve-bill"));

        // Worker ↔ directory linking and chase dismissals (2026-08-31, the month-end doc).
        commands.Register<SetWorkerSettlementIdentity, Worker>(
            new CommandRoute("POST", "/api/labour/workers/{workerId}/settlement-identity",
                command => $"/api/labour/workers/{((SetWorkerSettlementIdentity)command).WorkerId}/settlement-identity"));
        commands.Register<ReconcileWorkerDirectoryLinks, WorkerDirectoryLinkReport>(
            CommandRoute.Post("/api/labour/workers/reconcile-links"));
        commands.Register<DismissLabourChaseDay, Acknowledgement>(
            CommandRoute.Post("/api/labour/chase/dismiss"));
    }
}

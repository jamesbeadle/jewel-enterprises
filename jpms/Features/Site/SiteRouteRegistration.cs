using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Features.Site;

public static class SiteRouteRegistration
{
    public static IServiceCollection AddSiteReadModels(this IServiceCollection services)
    {
        services.AddScoped<SiteReportsReadModel>();
        services.AddScoped<ProgrammeReadModel>();
        return services;
    }

    public static void RegisterSiteRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListSiteReportsForProject, IReadOnlyList<SiteReport>>(
            new QueryRoute("/api/projects/{projectId}/site-reports",
                query => $"/api/projects/{((ListSiteReportsForProject)query).ProjectId}/site-reports"));

        queries.Register<GetProgrammeForProject, IReadOnlyList<ProgrammeTask>>(
            new QueryRoute("/api/projects/{projectId}/programme",
                query => $"/api/projects/{((GetProgrammeForProject)query).ProjectId}/programme"));

        queries.Register<GetProgrammeDetail, ProgrammeDetail>(
            new QueryRoute("/api/projects/{projectId}/programme-detail",
                query => $"/api/projects/{((GetProgrammeDetail)query).ProjectId}/programme-detail"));

        commands.Register<AssembleSiteReport, SiteReport>(
            new CommandRoute("POST", "/api/projects/{projectId}/site-reports",
                command => $"/api/projects/{((AssembleSiteReport)command).ProjectId}/site-reports"));

        commands.Register<ApproveSiteReport, SiteReport>(
            new CommandRoute("POST", "/api/site-reports/{siteReportId}/approval",
                command => $"/api/site-reports/{((ApproveSiteReport)command).SiteReportId}/approval"));

        commands.Register<AddProgrammeTask, ProgrammeTask>(
            new CommandRoute("POST", "/api/projects/{projectId}/programme",
                command => $"/api/projects/{((AddProgrammeTask)command).ProjectId}/programme"));

        commands.Register<UpdateProgrammeTask, ProgrammeTask>(
            new CommandRoute("PUT", "/api/programme-tasks/{programmeTaskId}",
                command => $"/api/programme-tasks/{((UpdateProgrammeTask)command).ProgrammeTaskId}"));

        commands.Register<AddProgrammeTaskLink, ProgrammeTaskLink>(
            new CommandRoute("POST", "/api/projects/{projectId}/programme/links",
                command => $"/api/projects/{((AddProgrammeTaskLink)command).ProjectId}/programme/links"));

        commands.Register<RemoveProgrammeTaskLink, Acknowledgement>(
            new CommandRoute("DELETE", "/api/programme-links/{programmeTaskLinkId}",
                command => $"/api/programme-links/{((RemoveProgrammeTaskLink)command).ProgrammeTaskLinkId}"));

        commands.Register<TakeProgrammeBaseline, ProgrammeBaseline>(
            new CommandRoute("POST", "/api/projects/{projectId}/programme/baselines",
                command => $"/api/projects/{((TakeProgrammeBaseline)command).ProjectId}/programme/baselines"));

        commands.Register<RemoveProgrammeTask, Acknowledgement>(
            new CommandRoute("DELETE", "/api/programme-tasks/{programmeTaskId}",
                command => $"/api/programme-tasks/{((RemoveProgrammeTask)command).ProgrammeTaskId}"));

        commands.Register<RemoveProgrammeBaseline, Acknowledgement>(
            new CommandRoute("DELETE", "/api/programme-baselines/{programmeBaselineId}",
                command => $"/api/programme-baselines/{((RemoveProgrammeBaseline)command).ProgrammeBaselineId}"));

        // Draft programme updates from the certified valuation (2026-09-08).
        queries.Register<GetOpenProgrammeDraft, ProgrammeDraftDetail?>(
            new QueryRoute("/api/projects/{projectId}/programme/draft",
                query => $"/api/projects/{((GetOpenProgrammeDraft)query).ProjectId}/programme/draft"));

        commands.Register<DraftProgrammeFromValuation, ProgrammeDraftDetail>(
            new CommandRoute("POST", "/api/projects/{projectId}/programme/drafts",
                command => $"/api/projects/{((DraftProgrammeFromValuation)command).ProjectId}/programme/drafts"));

        commands.Register<SuggestProgrammeDraftMappings, ProgrammeDraftDetail>(
            new CommandRoute("POST", "/api/programme-drafts/{programmeDraftId}/suggestions",
                command => $"/api/programme-drafts/{((SuggestProgrammeDraftMappings)command).ProgrammeDraftId}/suggestions"));

        commands.Register<ReviewProgrammeDraftLine, ProgrammeDraftLine>(
            new CommandRoute("PUT", "/api/programme-draft-lines/{programmeDraftLineId}",
                command => $"/api/programme-draft-lines/{((ReviewProgrammeDraftLine)command).ProgrammeDraftLineId}"));

        commands.Register<ApplyProgrammeDraft, ProgrammeDraft>(
            new CommandRoute("POST", "/api/programme-drafts/{programmeDraftId}/apply",
                command => $"/api/programme-drafts/{((ApplyProgrammeDraft)command).ProgrammeDraftId}/apply"));

        commands.Register<DiscardProgrammeDraft, ProgrammeDraft>(
            new CommandRoute("POST", "/api/programme-drafts/{programmeDraftId}/discard",
                command => $"/api/programme-drafts/{((DiscardProgrammeDraft)command).ProgrammeDraftId}/discard"));
    }
}

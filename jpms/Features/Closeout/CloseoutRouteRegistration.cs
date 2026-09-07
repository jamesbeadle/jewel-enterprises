using Jewel.JPMS.Contracts.Closeout;

namespace Jewel.JPMS.Features.Closeout;

public static class CloseoutRouteRegistration
{
    public static IServiceCollection AddCloseoutReadModels(this IServiceCollection services)
    {
        services.AddScoped<DefectsReadModel>();
        return services;
    }

    public static void RegisterCloseoutRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListDefectsForProject, IReadOnlyList<Defect>>(
            new QueryRoute("/api/projects/{projectId}/defects",
                query => $"/api/projects/{((ListDefectsForProject)query).ProjectId}/defects"));

        // One defect for its own page (/projects/{projectId}/defects/{defectId}).
        queries.Register<GetDefectById, Defect?>(
            new QueryRoute("/api/defects/{defectId}",
                query => $"/api/defects/{((GetDefectById)query).DefectId}"));

        queries.Register<GetSettlementForProject, SettlementRecord?>(
            new QueryRoute("/api/projects/{projectId}/settlement",
                query => $"/api/projects/{((GetSettlementForProject)query).ProjectId}/settlement"));

        queries.Register<GetVatAnalysisForProject, VatAnalysis?>(
            new QueryRoute("/api/projects/{projectId}/vat",
                query => $"/api/projects/{((GetVatAnalysisForProject)query).ProjectId}/vat"));

        queries.Register<GetRetentionForProject, RetentionRelease?>(
            new QueryRoute("/api/projects/{projectId}/retention",
                query => $"/api/projects/{((GetRetentionForProject)query).ProjectId}/retention"));

        commands.Register<RaiseDefect, Defect>(
            new CommandRoute("POST", "/api/projects/{projectId}/defects",
                command => $"/api/projects/{((RaiseDefect)command).ProjectId}/defects"));

        commands.Register<UpdateDefect, Defect>(
            new CommandRoute("PUT", "/api/defects/{defectId}",
                command => $"/api/defects/{((UpdateDefect)command).DefectId}"));

        // The Control Centre's "create new → Defect": raise + link the originating email.
        commands.Register<CreateDefectFromMessage, Defect>(
            new CommandRoute("POST", "/api/mailbox/message/create-defect",
                _ => "/api/mailbox/message/create-defect"));

        commands.Register<AgreeSettlement, SettlementRecord>(
            new CommandRoute("POST", "/api/projects/{projectId}/settlement",
                command => $"/api/projects/{((AgreeSettlement)command).ProjectId}/settlement"));

        commands.Register<AgreeVatAnalysis, VatAnalysis>(
            new CommandRoute("POST", "/api/projects/{projectId}/vat",
                command => $"/api/projects/{((AgreeVatAnalysis)command).ProjectId}/vat"));

        commands.Register<ReleaseRetention, RetentionRelease>(
            new CommandRoute("POST", "/api/projects/{projectId}/retention/release",
                command => $"/api/projects/{((ReleaseRetention)command).ProjectId}/retention/release"));
    }
}

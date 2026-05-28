using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Cvr;

public static class CvrRouteRegistration
{
    public static IServiceCollection AddCvrReadModels(this IServiceCollection services)
    {
        services.AddScoped<CvrSnapshotsReadModel>();
        return services;
    }

    public static void RegisterCvrRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListCvrSnapshotsForProject, IReadOnlyList<CvrSnapshot>>(
            new QueryRoute("/api/projects/{projectId}/cvr-snapshots",
                query => $"/api/projects/{((ListCvrSnapshotsForProject)query).ProjectId}/cvr-snapshots"));

        queries.Register<ListForecastComponentsForProject, IReadOnlyList<ForecastComponent>>(
            new QueryRoute("/api/projects/{projectId}/forecast-components",
                query => $"/api/projects/{((ListForecastComponentsForProject)query).ProjectId}/forecast-components"));

        queries.Register<ListQsAccrualsForProject, IReadOnlyList<QsAccrual>>(
            new QueryRoute("/api/projects/{projectId}/qs-accruals",
                query => $"/api/projects/{((ListQsAccrualsForProject)query).ProjectId}/qs-accruals"));

        queries.Register<ListPrelimItemsForProject, IReadOnlyList<PrelimItem>>(
            new QueryRoute("/api/projects/{projectId}/prelims",
                query => $"/api/projects/{((ListPrelimItemsForProject)query).ProjectId}/prelims"));

        queries.Register<ListPrelimEntriesForItem, IReadOnlyList<PrelimForecastEntry>>(
            new QueryRoute("/api/prelims/{prelimItemId}/entries",
                query => $"/api/prelims/{((ListPrelimEntriesForItem)query).PrelimItemId}/entries"));

        queries.Register<ListEotsForProject, IReadOnlyList<Eot>>(
            new QueryRoute("/api/projects/{projectId}/eots",
                query => $"/api/projects/{((ListEotsForProject)query).ProjectId}/eots"));

        queries.Register<ListCvrPackagesForProject, IReadOnlyList<CvrPackageRow>>(
            new QueryRoute("/api/projects/{projectId}/cvr-packages",
                query => $"/api/projects/{((ListCvrPackagesForProject)query).ProjectId}/cvr-packages"));

        commands.Register<CaptureCvrSnapshot, CvrSnapshot>(
            new CommandRoute("POST", "/api/projects/{projectId}/cvr-snapshots",
                command => $"/api/projects/{((CaptureCvrSnapshot)command).ProjectId}/cvr-snapshots"));

        commands.Register<RecordCvrPackageRow, CvrPackageRow>(
            new CommandRoute("POST", "/api/projects/{projectId}/cvr-packages",
                command => $"/api/projects/{((RecordCvrPackageRow)command).ProjectId}/cvr-packages"));

        commands.Register<RecordQsAccrual, QsAccrual>(
            new CommandRoute("POST", "/api/projects/{projectId}/qs-accruals",
                command => $"/api/projects/{((RecordQsAccrual)command).ProjectId}/qs-accruals"));

        commands.Register<UpdateQsAccrual, QsAccrual>(
            new CommandRoute("PUT", "/api/qs-accruals/{qsAccrualId}",
                command => $"/api/qs-accruals/{((UpdateQsAccrual)command).QsAccrualId}"));

        commands.Register<GrantEot, Eot>(
            new CommandRoute("POST", "/api/projects/{projectId}/eots",
                command => $"/api/projects/{((GrantEot)command).ProjectId}/eots"));

        commands.Register<UpdateEot, Eot>(
            new CommandRoute("PUT", "/api/eots/{eotId}",
                command => $"/api/eots/{((UpdateEot)command).EotId}"));
    }
}

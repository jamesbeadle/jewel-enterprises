using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Cvr.Commands;
using Jewel.JPMS.Api.Features.Cvr.Queries;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Cvr;

public static class CvrFeatureRegistration
{
    public static IServiceCollection AddCvrFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListCvrSnapshotsForProject, IReadOnlyList<CvrSnapshot>>, ListCvrSnapshotsForProjectHandler>();
        services.AddScoped<IQueryHandler<ListForecastComponentsForProject, IReadOnlyList<ForecastComponent>>, ListForecastComponentsForProjectHandler>();
        services.AddScoped<IQueryHandler<ListQsAccrualsForProject, IReadOnlyList<QsAccrual>>, ListQsAccrualsForProjectHandler>();
        services.AddScoped<IQueryHandler<ListPrelimItemsForProject, IReadOnlyList<PrelimItem>>, ListPrelimItemsForProjectHandler>();
        services.AddScoped<IQueryHandler<ListPrelimEntriesForItem, IReadOnlyList<PrelimForecastEntry>>, ListPrelimEntriesForItemHandler>();
        services.AddScoped<IQueryHandler<ListEotsForProject, IReadOnlyList<Eot>>, ListEotsForProjectHandler>();

        services.AddScoped<ICommandHandler<CaptureCvrSnapshot, CvrSnapshot>, CaptureCvrSnapshotHandler>();
        services.AddScoped<CaptureCvrSnapshotAuthorisation>();
        services.AddScoped<CaptureCvrSnapshotValidation>();

        services.AddScoped<ICommandHandler<RecordQsAccrual, QsAccrual>, RecordQsAccrualHandler>();
        services.AddScoped<RecordQsAccrualAuthorisation>();
        services.AddScoped<RecordQsAccrualValidation>();

        services.AddScoped<ICommandHandler<UpdateQsAccrual, QsAccrual>, UpdateQsAccrualHandler>();
        services.AddScoped<UpdateQsAccrualAuthorisation>();
        services.AddScoped<UpdateQsAccrualValidation>();

        services.AddScoped<ICommandHandler<GrantEot, Eot>, GrantEotHandler>();
        services.AddScoped<GrantEotAuthorisation>();
        services.AddScoped<GrantEotValidation>();

        services.AddScoped<ICommandHandler<UpdateEot, Eot>, UpdateEotHandler>();
        services.AddScoped<UpdateEotAuthorisation>();
        services.AddScoped<UpdateEotValidation>();

        return services;
    }
}

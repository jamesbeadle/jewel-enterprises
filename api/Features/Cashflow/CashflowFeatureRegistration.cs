using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Cashflow.Commands;
using Jewel.JPMS.Api.Features.Cashflow.Queries;
using Jewel.JPMS.Contracts.Cashflow;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Cashflow;

public static class CashflowFeatureRegistration
{
    public static IServiceCollection AddCashflowFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<GetLatestCashflowSnapshot, CashflowSnapshot?>, GetLatestCashflowSnapshotHandler>();

        services.AddScoped<ICommandHandler<CaptureCashflowSnapshot, CashflowSnapshot>, CaptureCashflowSnapshotHandler>();
        services.AddScoped<CaptureCashflowSnapshotAuthorisation>();
        services.AddScoped<CaptureCashflowSnapshotValidation>();

        return services;
    }
}

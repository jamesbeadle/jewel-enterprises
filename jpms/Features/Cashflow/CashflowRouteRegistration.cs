using Jewel.JPMS.Contracts.Cashflow;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Cashflow;

public static class CashflowRouteRegistration
{
    public static IServiceCollection AddCashflowReadModels(this IServiceCollection services)
    {
        services.AddScoped<CashflowReadModel>();
        return services;
    }

    public static void RegisterCashflowRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<GetLatestCashflowSnapshot, CashflowSnapshot?>(QueryRoute.Static("/api/cashflow/latest"));
        commands.Register<CaptureCashflowSnapshot, CashflowSnapshot>(CommandRoute.Post("/api/cashflow/snapshots"));
    }
}

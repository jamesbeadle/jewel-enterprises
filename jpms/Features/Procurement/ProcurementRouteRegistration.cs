using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Procurement;

public static class ProcurementRouteRegistration
{
    public static IServiceCollection AddProcurementReadModels(this IServiceCollection services)
    {
        services.AddScoped<BidPackagesReadModel>();
        services.AddScoped<WorkOrdersReadModel>();
        return services;
    }

    public static void RegisterProcurementRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListBidPackagesForProject, IReadOnlyList<BidPackage>>(
            new QueryRoute("/api/projects/{projectId}/bid-packages",
                query => $"/api/projects/{((ListBidPackagesForProject)query).ProjectId}/bid-packages"));

        queries.Register<GetBidPackageById, BidPackage?>(
            new QueryRoute("/api/bid-packages/{bidPackageId}",
                query => $"/api/bid-packages/{((GetBidPackageById)query).BidPackageId}"));

        queries.Register<ListQuotesForBidPackage, IReadOnlyList<Quote>>(
            new QueryRoute("/api/bid-packages/{bidPackageId}/quotes",
                query => $"/api/bid-packages/{((ListQuotesForBidPackage)query).BidPackageId}/quotes"));

        queries.Register<ListWorkOrders, IReadOnlyList<WorkOrder>>(QueryRoute.Static("/api/work-orders"));

        commands.Register<CreateBidPackage, BidPackage>(
            new CommandRoute("POST", "/api/projects/{projectId}/bid-packages",
                command => $"/api/projects/{((CreateBidPackage)command).ProjectId}/bid-packages"));

        commands.Register<UpdateBidPackageScope, BidPackage>(
            new CommandRoute("PUT", "/api/bid-packages/{bidPackageId}",
                command => $"/api/bid-packages/{((UpdateBidPackageScope)command).BidPackageId}"));

        commands.Register<SubmitQuoteForBidPackage, Quote>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/quotes",
                command => $"/api/bid-packages/{((SubmitQuoteForBidPackage)command).BidPackageId}/quotes"));

        commands.Register<ReviseQuote, Quote>(
            new CommandRoute("PUT", "/api/quotes/{quoteId}",
                command => $"/api/quotes/{((ReviseQuote)command).QuoteId}"));

        commands.Register<AwardBidPackage, WorkOrder>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/award",
                command => $"/api/bid-packages/{((AwardBidPackage)command).BidPackageId}/award"));

        commands.Register<UpdateWorkOrder, WorkOrder>(
            new CommandRoute("PUT", "/api/work-orders/{workOrderId}",
                command => $"/api/work-orders/{((UpdateWorkOrder)command).WorkOrderId}"));
    }
}

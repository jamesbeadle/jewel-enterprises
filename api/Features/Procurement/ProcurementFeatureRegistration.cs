using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Procurement.Commands;
using Jewel.JPMS.Api.Features.Procurement.Queries;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Procurement;

public static class ProcurementFeatureRegistration
{
    public static IServiceCollection AddProcurementFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListBidPackagesForProject, IReadOnlyList<BidPackage>>, ListBidPackagesForProjectHandler>();
        services.AddScoped<IQueryHandler<GetBidPackageById, BidPackage?>, GetBidPackageByIdHandler>();
        services.AddScoped<IQueryHandler<ListQuotesForBidPackage, IReadOnlyList<Quote>>, ListQuotesForBidPackageHandler>();
        services.AddScoped<IQueryHandler<ListWorkOrders, IReadOnlyList<WorkOrder>>, ListWorkOrdersHandler>();

        services.AddScoped<ICommandHandler<CreateBidPackage, BidPackage>, CreateBidPackageHandler>();
        services.AddScoped<CreateBidPackageAuthorisation>();
        services.AddScoped<CreateBidPackageValidation>();

        services.AddScoped<ICommandHandler<UpdateBidPackageScope, BidPackage>, UpdateBidPackageScopeHandler>();
        services.AddScoped<UpdateBidPackageScopeAuthorisation>();
        services.AddScoped<UpdateBidPackageScopeValidation>();

        services.AddScoped<ICommandHandler<SubmitQuoteForBidPackage, Quote>, SubmitQuoteForBidPackageHandler>();
        services.AddScoped<SubmitQuoteForBidPackageAuthorisation>();
        services.AddScoped<SubmitQuoteForBidPackageValidation>();

        services.AddScoped<ICommandHandler<ReviseQuote, Quote>, ReviseQuoteHandler>();
        services.AddScoped<ReviseQuoteAuthorisation>();
        services.AddScoped<ReviseQuoteValidation>();

        services.AddScoped<ICommandHandler<AwardBidPackage, WorkOrder>, AwardBidPackageHandler>();
        services.AddScoped<AwardBidPackageAuthorisation>();
        services.AddScoped<AwardBidPackageValidation>();

        services.AddScoped<ICommandHandler<UpdateWorkOrder, WorkOrder>, UpdateWorkOrderHandler>();
        services.AddScoped<UpdateWorkOrderAuthorisation>();
        services.AddScoped<UpdateWorkOrderValidation>();

        return services;
    }
}

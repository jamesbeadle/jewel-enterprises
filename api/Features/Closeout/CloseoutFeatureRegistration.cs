using Jewel.JPMS.Api.Features.Closeout.Commands;
using Jewel.JPMS.Api.Features.Closeout.Queries;
using Jewel.JPMS.Contracts.Closeout;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Closeout;

public static class CloseoutFeatureRegistration
{
    public static IServiceCollection AddCloseoutFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListDefectsForProject, IReadOnlyList<Defect>>, ListDefectsForProjectHandler>();
        services.AddScoped<IQueryHandler<GetDefectById, Defect?>, GetDefectByIdHandler>();
        // The compose pipeline's bridge to a defect: a sent email carrying a defect's tag, to its
        // supplier, stamps the defect as sent (Open → In progress).
        services.AddScoped<DefectSupplierSendRecorder>();
        services.AddScoped<IQueryHandler<GetRetentionForProject, RetentionRelease?>, GetRetentionForProjectHandler>();
        services.AddScoped<IQueryHandler<GetSettlementForProject, SettlementRecord?>, GetSettlementForProjectHandler>();
        services.AddScoped<IQueryHandler<GetVatAnalysisForProject, VatAnalysis?>, GetVatAnalysisForProjectHandler>();

        services.AddScoped<ICommandHandler<RaiseDefect, Defect>, RaiseDefectHandler>();
        services.AddScoped<RaiseDefectAuthorisation>();
        services.AddScoped<RaiseDefectValidation>();

        services.AddScoped<ICommandHandler<UpdateDefect, Defect>, UpdateDefectHandler>();
        services.AddScoped<UpdateDefectAuthorisation>();
        services.AddScoped<UpdateDefectValidation>();

        // The Control Centre's "create new → Defect": raise + link the originating email.
        services.AddScoped<ICommandHandler<CreateDefectFromMessage, Defect>, CreateDefectFromMessageHandler>();
        services.AddScoped<CreateDefectFromMessageAuthorisation>();
        services.AddScoped<CreateDefectFromMessageValidation>();

        services.AddScoped<ICommandHandler<AgreeSettlement, SettlementRecord>, AgreeSettlementHandler>();
        services.AddScoped<AgreeSettlementAuthorisation>();
        services.AddScoped<AgreeSettlementValidation>();

        services.AddScoped<ICommandHandler<AgreeVatAnalysis, VatAnalysis>, AgreeVatAnalysisHandler>();
        services.AddScoped<AgreeVatAnalysisAuthorisation>();
        services.AddScoped<AgreeVatAnalysisValidation>();

        services.AddScoped<ICommandHandler<ReleaseRetention, RetentionRelease>, ReleaseRetentionHandler>();
        services.AddScoped<ReleaseRetentionAuthorisation>();
        services.AddScoped<ReleaseRetentionValidation>();

        return services;
    }
}

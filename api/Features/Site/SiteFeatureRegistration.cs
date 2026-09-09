using Jewel.JPMS.Api.Features.Site.Commands;
using Jewel.JPMS.Api.Features.Site.Queries;
using Jewel.JPMS.Contracts.Site;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Site;

public static class SiteFeatureRegistration
{
    public static IServiceCollection AddSiteFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListSiteReportsForProject, IReadOnlyList<SiteReport>>, ListSiteReportsForProjectHandler>();
        services.AddScoped<IQueryHandler<GetProgrammeForProject, IReadOnlyList<ProgrammeTask>>, GetProgrammeForProjectHandler>();
        services.AddScoped<IQueryHandler<GetProgrammeDetail, ProgrammeDetail>, GetProgrammeDetailHandler>();

        services.AddScoped<ICommandHandler<AssembleSiteReport, SiteReport>, AssembleSiteReportHandler>();
        services.AddScoped<AssembleSiteReportAuthorisation>();
        services.AddScoped<AssembleSiteReportValidation>();

        services.AddScoped<ICommandHandler<ApproveSiteReport, SiteReport>, ApproveSiteReportHandler>();
        services.AddScoped<ApproveSiteReportAuthorisation>();
        services.AddScoped<ApproveSiteReportValidation>();

        services.AddScoped<ICommandHandler<AddProgrammeTask, ProgrammeTask>, AddProgrammeTaskHandler>();
        services.AddScoped<AddProgrammeTaskAuthorisation>();
        services.AddScoped<AddProgrammeTaskValidation>();

        services.AddScoped<ICommandHandler<UpdateProgrammeTask, ProgrammeTask>, UpdateProgrammeTaskHandler>();
        services.AddScoped<UpdateProgrammeTaskAuthorisation>();
        services.AddScoped<UpdateProgrammeTaskValidation>();

        services.AddScoped<ICommandHandler<AddProgrammeTaskLink, ProgrammeTaskLink>, AddProgrammeTaskLinkHandler>();
        services.AddScoped<AddProgrammeTaskLinkAuthorisation>();
        services.AddScoped<AddProgrammeTaskLinkValidation>();

        services.AddScoped<ICommandHandler<RemoveProgrammeTaskLink, Jewel.JPMS.Contracts.Cqrs.Acknowledgement>, RemoveProgrammeTaskLinkHandler>();
        services.AddScoped<RemoveProgrammeTaskLinkAuthorisation>();
        services.AddScoped<RemoveProgrammeTaskLinkValidation>();

        services.AddScoped<ProgrammeVariationEffectAuthorisation>();
        services.AddScoped<ICommandHandler<RecordProgrammeVariationEffect, ProgrammeVariationEffect>, RecordProgrammeVariationEffectHandler>();
        services.AddScoped<RecordProgrammeVariationEffectValidation>();
        services.AddScoped<ICommandHandler<RemoveProgrammeVariationEffect, Jewel.JPMS.Contracts.Cqrs.Acknowledgement>, RemoveProgrammeVariationEffectHandler>();
        services.AddScoped<RemoveProgrammeVariationEffectValidation>();

        services.AddScoped<ICommandHandler<TakeProgrammeBaseline, ProgrammeBaseline>, TakeProgrammeBaselineHandler>();
        services.AddScoped<TakeProgrammeBaselineAuthorisation>();
        services.AddScoped<TakeProgrammeBaselineValidation>();

        services.AddScoped<ICommandHandler<RemoveProgrammeTask, Jewel.JPMS.Contracts.Cqrs.Acknowledgement>, RemoveProgrammeTaskHandler>();
        services.AddScoped<RemoveProgrammeTaskAuthorisation>();
        services.AddScoped<RemoveProgrammeTaskValidation>();

        services.AddScoped<ICommandHandler<RemoveProgrammeBaseline, Jewel.JPMS.Contracts.Cqrs.Acknowledgement>, RemoveProgrammeBaselineHandler>();
        services.AddScoped<RemoveProgrammeBaselineAuthorisation>();
        services.AddScoped<RemoveProgrammeBaselineValidation>();

        // Draft programme updates from the certified valuation (2026-09-08). One authorisation
        // for the five commands — they are all edits of the programme in waiting.
        services.AddScoped<IQueryHandler<GetOpenProgrammeDraft, ProgrammeDraftDetail?>, GetOpenProgrammeDraftHandler>();
        services.AddScoped<ProgrammeDraftAuthorisation>();

        services.AddScoped<ICommandHandler<DraftProgrammeFromValuation, ProgrammeDraftDetail>, DraftProgrammeFromValuationHandler>();
        services.AddScoped<DraftProgrammeFromValuationValidation>();

        services.AddScoped<ICommandHandler<SuggestProgrammeDraftMappings, ProgrammeDraftDetail>, SuggestProgrammeDraftMappingsHandler>();

        services.AddScoped<ICommandHandler<ReviewProgrammeDraftLine, ProgrammeDraftLine>, ReviewProgrammeDraftLineHandler>();
        services.AddScoped<ReviewProgrammeDraftLineValidation>();

        services.AddScoped<ICommandHandler<ApplyProgrammeDraft, ProgrammeDraft>, ApplyProgrammeDraftHandler>();
        services.AddScoped<ApplyProgrammeDraftValidation>();

        services.AddScoped<ICommandHandler<DiscardProgrammeDraft, ProgrammeDraft>, DiscardProgrammeDraftHandler>();
        services.AddScoped<DiscardProgrammeDraftValidation>();

        return services;
    }
}

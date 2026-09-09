using Jewel.JPMS.Api.Features.Ai.Queries;
using Jewel.JPMS.Contracts.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai;

/// <summary>
/// Registers what remains of the AI feature now the in-portal chat is gone (2026-08-27, replaced
/// by the MCP connector — docs/ai/10-mcp-connector.md): the one-shot Claude client the Procurement
/// helpers use, the agent activity log, and the skill store. A real Claude client when an API key
/// is present in configuration, otherwise a no-op so the rest of the app runs unchanged — the
/// triage draft falls back to plain subject/body. The key is read from app settings / Key Vault
/// only (Anthropic__ApiKey) — never from source control.
/// </summary>
public static class AiFeatureRegistration
{
    public static IServiceCollection AddAiFeature(this IServiceCollection services, IConfiguration configuration)
    {
        // Fails the boot if the hand-kept AI registries (tools, labels) have drifted apart —
        // deterministic static data, so a throw here can only mean a commit shipped the drift.
        AiRegistryDriftCheck.Assert();

        var options = AnthropicOptions.FromConfiguration(configuration);
        services.AddSingleton(options);

        if (options.IsConfigured)
        {
            // Own HttpClient instance so it doesn't clash with the Graph client's registration.
            services.AddSingleton<IClaudeClient>(sp =>
                new ClaudeClient(new HttpClient(), options, sp.GetRequiredService<ILogger<ClaudeClient>>()));
        }
        else
        {
            services.AddSingleton<IClaudeClient, NullClaudeClient>();
        }

        // Reading scans (2026-09-09): Azure AI Vision's Read behind the document reader when
        // DocumentOcr__Endpoint / __ApiKey are set; otherwise scans show as page images only.
        var ocrOptions = Scans.DocumentOcrOptions.FromConfiguration(configuration);
        services.AddSingleton(ocrOptions);
        if (ocrOptions.IsConfigured)
            services.AddSingleton<Scans.IDocumentOcr>(sp => new Scans.AzureVisionOcr(new HttpClient(), ocrOptions));
        else
            services.AddSingleton<Scans.IDocumentOcr, Scans.NullDocumentOcr>();

        // The agent activity log. Scoped because it writes through the request's JpmsContext.
        services.AddScoped<AgentActivityLog>();
        services.AddScoped<IQueryHandler<ListAgentActivity, IReadOnlyList<AgentActivity>>, ListAgentActivityHandler>();

        // The skill store — the portal's own working knowledge, edited in the portal and read by
        // the MCP connector's load_skill tools.
        services.AddScoped<IQueryHandler<ListAiSkills, IReadOnlyList<SkillSummary>>, Skills.ListAiSkillsHandler>();
        services.AddScoped<IQueryHandler<GetAiSkill, SkillDetail?>, Skills.GetAiSkillHandler>();
        services.AddScoped<ICommandHandler<SaveAiSkill, Acknowledgement>, Skills.SaveAiSkillHandler>();
        services.AddScoped<Skills.SaveAiSkillAuthorisation>();
        services.AddScoped<Skills.SaveAiSkillValidation>();
        services.AddScoped<ICommandHandler<SaveAiSkillReference, Acknowledgement>, Skills.SaveAiSkillReferenceHandler>();
        services.AddScoped<Skills.SaveAiSkillReferenceAuthorisation>();
        services.AddScoped<Skills.SaveAiSkillReferenceValidation>();

        // Skills wired to connector actions — the AI Actions admin page's catalogue and picker
        // (describe_action reads the same rows straight off the request's JpmsContext).
        services.AddScoped<IQueryHandler<GetAiActionCatalogue, AiActionCatalogue>, Skills.GetAiActionCatalogueHandler>();
        services.AddScoped<ICommandHandler<SaveAiActionSkills, Acknowledgement>, Skills.SaveAiActionSkillsHandler>();
        services.AddScoped<Skills.SaveAiActionSkillsAuthorisation>();
        services.AddScoped<Skills.SaveAiActionSkillsValidation>();

        return services;
    }
}

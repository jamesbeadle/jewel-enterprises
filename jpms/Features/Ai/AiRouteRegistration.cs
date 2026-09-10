
namespace Jewel.JPMS.Features.Ai;

public static class AiRouteRegistration
{
    public static void RegisterAiRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListAgentActivity, IReadOnlyList<AgentActivity>>(
            new QueryRoute("/api/agents/activity", BuildAgentActivityPath));

        // The skill store — the AI Skills admin page (docs/ai/10-mcp-connector.md §2d).
        queries.Register<ListAiSkills, IReadOnlyList<SkillSummary>>(
            new QueryRoute("/api/ai/skills", _ => "/api/ai/skills"));

        queries.Register<GetAiSkill, SkillDetail?>(
            new QueryRoute("/api/ai/skills/{skillKey}",
                query => $"/api/ai/skills/{Uri.EscapeDataString(((GetAiSkill)query).SkillKey)}"));

        commands.Register<SaveAiSkill, Acknowledgement>(
            CommandRoute.Post("/api/ai/skills"));

        commands.Register<SaveAiSkillReference, Acknowledgement>(
            CommandRoute.Post("/api/ai/skills/references"));

        // Skills wired to connector actions — the AI Actions admin page (/admin/ai-actions).
        queries.Register<GetAiActionCatalogue, AiActionCatalogue>(
            new QueryRoute("/api/ai/action-skills", _ => "/api/ai/action-skills"));

        commands.Register<SaveAiActionSkills, Acknowledgement>(
            CommandRoute.Post("/api/ai/action-skills"));
    }

    private static string BuildAgentActivityPath(object query)
    {
        var request = (ListAgentActivity)query;
        var parameters = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.ProjectId))
            parameters.Add($"projectId={Uri.EscapeDataString(request.ProjectId)}");
        if (!string.IsNullOrWhiteSpace(request.AgentKey))
            parameters.Add($"agentKey={Uri.EscapeDataString(request.AgentKey)}");
        if (request.AutonomousOnly == true)
            parameters.Add("autonomousOnly=1");
        if (request.Take > 0)
            parameters.Add($"take={request.Take}");

        return parameters.Count == 0
            ? "/api/agents/activity"
            : $"/api/agents/activity?{string.Join("&", parameters)}";
    }
}

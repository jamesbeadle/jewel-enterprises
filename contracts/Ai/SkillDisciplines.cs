namespace Jewel.JPMS.Contracts.Ai;

/// <summary>The disciplines a stored skill can belong to — the value of <c>Skills.AgentKey</c>.
/// "shared" is house-wide knowledge every discipline reads; the rest group a discipline's own
/// doctrine so <c>list_skills</c> can present it under a heading. This is a labelling axis only:
/// over the MCP connector every skill is served to every user, and which skills ride with which
/// action is the AI Actions page's attachments, not this key. (The per-agent routing this key
/// once drove went with the in-portal chat, 2026-08-27; the column keeps its name so nothing
/// persisted moves.)</summary>
public static class SkillDisciplines
{
    public const string Shared = "shared";

    public static readonly IReadOnlyList<(string Key, string DisplayName)> All = new[]
    {
        (Shared, "Shared — every discipline"),
        ("commercial", "Commercial"),
        ("bid-packages", "Bid packages"),
        ("timesheets", "Timesheets"),
    };

    public static bool IsKnown(string? key) =>
        key is not null && All.Any(discipline => discipline.Key == key);

    public static string LabelFor(string key) =>
        All.FirstOrDefault(discipline => discipline.Key == key).DisplayName ?? key;
}

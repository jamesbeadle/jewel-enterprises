
namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The skill store's read tools: list what the portal has been taught, load one skill's full
/// text, and load a skill's larger reference documents on demand. The store survives the retired
/// in-portal chat unchanged (docs/ai/10-mcp-connector.md §2d) — over the MCP connector the
/// model discovers skills with list_skills instead of a system prompt, and the AgentKey column
/// now reads as the skill's discipline grouping.</summary>
internal static class AiSkillTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    /// <summary>The agent key that marks a skill as pinned/loadable for EVERY agent.</summary>
    internal const string SharedAgentKey = "shared";

    public static IReadOnlyList<AiTool> Build()
    {
        return new List<AiTool>
        {
            new(
                "list_skills",
                "The portal's stored skills — the working doctrine the team has taught the AI "
                + "(house style, commercial rules, fact patterns), each with its key, name and when "
                + "it applies. Call this once early in a piece of portal work, then load_skill for "
                + "any skill that covers the task in hand and follow what it says.",
                AiToolSchema.Empty(),
                AiToolKind.Read,
                JpmsRoleSets.AllInternal,
                async (context, _, ct) =>
                {
                    var skills = await context.Db.Skills
                        .AsNoTracking()
                        .Where(row => row.IsActive)
                        .OrderBy(row => row.AgentKey).ThenBy(row => row.SkillKey)
                        .Select(row => new { key = row.SkillKey, name = row.DisplayName, row.Description, discipline = row.AgentKey })
                        .ToListAsync(ct);
                    return Serialise(new { ok = true, count = skills.Count, skills,
                        note = "load_skill(skill_key) returns a skill's full text." });
                }),

            new(
                "load_skill",
                "Load the full text of one of the portal's stored skills — the discipline manuals "
                + "list_skills names. Call it when the task in hand is one a listed skill covers and "
                + "you have not already loaded it this conversation; then follow what it says. Do "
                + "not guess at keys: only the listed ones exist.",
                AiToolSchema.Object(("skill_key", "string", "A skill key from list_skills.", true)),
                AiToolKind.Read,
                JpmsRoleSets.AllInternal,
                async (context, input, ct) =>
                {
                    var key = AiToolSchema.Text(input, "skill_key")?.Trim();
                    if (string.IsNullOrWhiteSpace(key)) return Fail("A skill_key is required.");

                    var skill = await context.Db.Skills
                        .AsNoTracking()
                        .FirstOrDefaultAsync(row => row.SkillKey == key && row.IsActive, ct);

                    if (skill is null)
                        return Fail($"No skill named {key} exists — list_skills shows the keys that do.");

                    var references = await context.Db.SkillReferences
                        .AsNoTracking()
                        .Where(row => row.SkillKey == skill.SkillKey)
                        .OrderBy(row => row.RefKey)
                        .Select(row => new { row.RefKey, row.DisplayName, row.Description })
                        .ToListAsync(ct);

                    return Serialise(new
                    {
                        ok = true,
                        skill = skill.SkillKey,
                        version = skill.Version,
                        body = skill.Body,
                        references,
                        note = references.Count == 0
                            ? "This skill has no reference documents."
                            : "Reference documents are listed by key — call load_skill_reference "
                              + "only for one the task actually needs."
                    });
                }),

            new(
                "load_skill_reference",
                "Load one reference document belonging to a skill — the larger source material "
                + "(clause maps, methodologies, precedent libraries) a skill names but does not "
                + "inline. Call load_skill first; it lists the reference keys that exist. Load a "
                + "reference only when the task actually needs its contents — they are large.",
                AiToolSchema.Object(
                    ("skill_key", "string", "The owning skill's key.", true),
                    ("ref_key", "string", "A reference key that load_skill listed.", true)),
                AiToolKind.Read,
                JpmsRoleSets.AllInternal,
                async (context, input, ct) =>
                {
                    var skillKey = AiToolSchema.Text(input, "skill_key")?.Trim();
                    var refKey = AiToolSchema.Text(input, "ref_key")?.Trim();
                    if (string.IsNullOrWhiteSpace(skillKey) || string.IsNullOrWhiteSpace(refKey))
                        return Fail("Both skill_key and ref_key are required.");

                    var skill = await context.Db.Skills
                        .AsNoTracking()
                        .FirstOrDefaultAsync(row => row.SkillKey == skillKey && row.IsActive, ct);

                    if (skill is null)
                        return Fail($"No skill named {skillKey} exists — list_skills shows the keys that do.");

                    var reference = await context.Db.SkillReferences
                        .AsNoTracking()
                        .FirstOrDefaultAsync(row => row.SkillKey == skillKey && row.RefKey == refKey, ct);

                    if (reference is null)
                        return Fail($"The skill {skillKey} has no reference named {refKey}. "
                                    + "load_skill lists the keys that exist.");

                    return Serialise(new
                    {
                        ok = true,
                        skill = skillKey,
                        reference = reference.RefKey,
                        title = reference.DisplayName,
                        body = reference.Body
                    });
                })
        };
    }
}

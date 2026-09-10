using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Ai;

/// <summary>
/// Skills are the team-owned knowledge the connector serves (docs/ai/10-mcp-connector.md §2d): versioned markdown
/// manuals — doctrine, method, standing rules — stored in the database and edited in the portal by
/// the person who owns the discipline. The connector's tools and actions are hard-coded; the
/// construction-industry knowledge is a skill, and updating it is a portal action, not a deploy.
///
/// <para>Format follows agentskills.io: a name, a description
/// (what the orchestrator routes on), a markdown body, and optional reference documents loaded on
/// demand by the model via load_skill / load_skill_reference.</para>
/// </summary>
public sealed record SkillSummary(
    string SkillKey,
    /// <summary>The discipline it belongs to (a SkillDisciplines key), or "shared" for house-wide knowledge.</summary>
    string AgentKey,
    string DisplayName,
    string Description,
    /// <summary>Pinned skills ride in the system prompt whenever their agent is in force; unpinned
    /// ones are listed by name and loaded on demand with load_skill.</summary>
    bool Pinned,
    bool IsActive,
    int Version,
    int BodyLength,
    int ReferenceCount,
    string UpdatedByEmail,
    DateTimeOffset UpdatedAt);

public sealed record SkillReferenceDetail(
    string RefKey,
    string DisplayName,
    string Description,
    string Body,
    DateTimeOffset UpdatedAt);

public sealed record SkillDetail(
    string SkillKey,
    string AgentKey,
    string DisplayName,
    string Description,
    string Body,
    bool Pinned,
    bool IsActive,
    int Version,
    string UpdatedByEmail,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<SkillReferenceDetail> References);

/// <summary>Every skill, newest first. The admin page's list.</summary>
public sealed record ListAiSkills : IQuery<IReadOnlyList<SkillSummary>>;

/// <summary>One skill with its body and references — the admin page's editor.</summary>
public sealed record GetAiSkill(string SkillKey) : IQuery<SkillDetail?>;

/// <summary>
/// Create or update a skill. An existing key is a new version (the old body is kept as a
/// revision); a new key is version 1. <c>SavedByEmail</c> is re-stamped from the session.
/// </summary>
public sealed record SaveAiSkill(
    string SkillKey,
    string AgentKey,
    string DisplayName,
    string Description,
    string Body,
    bool Pinned,
    bool IsActive,
    string SavedByEmail) : ICommand<Acknowledgement>;

/// <summary>Create or update one reference document under a skill.</summary>
public sealed record SaveAiSkillReference(
    string SkillKey,
    string RefKey,
    string DisplayName,
    string Description,
    string Body,
    string SavedByEmail) : ICommand<Acknowledgement>;

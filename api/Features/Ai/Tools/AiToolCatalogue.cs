using Ganss.Xss;
using Jewel.JPMS.Api.Features.Labour; // SiteClock (view_labour_week's week arithmetic)
using Jewel.JPMS.Api.Features.MailboxIntake.Graph; // IIntakeMessageReader (record email reads)
using Jewel.JPMS.Api.Features.Requests; // TriageRoles (internal, same assembly)
using Jewel.JPMS.Contracts.Ai;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The tools the assistant can call, and the only ones it is ever told about.
///
/// <para>Filtered per user by <see cref="AiTool.VisibleTo"/> before the catalogue is sent, so a tool
/// the caller could not use is never described to the model — it cannot promise something it will
/// then be refused.</para>
///
/// <para>These read directly through EF rather than dispatching the CQRS query handlers, so each
/// tool's <see cref="AiTool.VisibleTo"/> has to carry the gate its backing query would have applied.
/// Checked against the endpoints when the panel widened to PM/QS on 2026-07-27: requests and
/// variations gate on <c>InternalAndArchitect</c>, contracts, cost centres and projects on
/// <c>AllInternal</c>, and every tool below declares one of those — so the widening granted nothing
/// those roles could not already read by clicking. <b>A new tool must declare the RoleSet its
/// backing query uses</b>, and a tool whose query is narrower than the panel's own gate must route
/// through the query handler instead. Noted in docs/ai/00-agent-architecture.md §4.</para>
/// </summary>
public static partial class AiToolCatalogue
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    /// <summary>
    /// How much of a request's conversation get_request_context returns unasked. Sized for FULL
    /// email bodies rather than Graph's 255-character previews: a six-leg Outlook thread, each reply
    /// carrying the quoted history beneath it, is comfortably 30k characters, and a budget set for
    /// previews would re-truncate exactly what the full-body fetch was added to recover.
    ///
    /// <para>The budget is spent per message inside RequestContextAssembler, so every message keeps
    /// its date, author, subject and attachment names however tight it gets.</para>
    /// </summary>
    private const int DefaultConversationChars = 25_000;

    private const int MaxConversationChars = 50_000;

    /// <summary>Every tool, before role filtering. (AiEmailTools' draft_outlook_email was retired
    /// 2026-08-14: assistant-drafted email now goes through the Control Centre's own composer —
    /// open_modal "compose_email" — so the user reviews and sends in the portal, never in Outlook.)</summary>
    public static IReadOnlyList<AiTool> All { get; } =
        Build()
            .Concat(AiRecordTools.Build())
            .Concat(AiSourceTools.Build())
            .Concat(AiCommercialTools.Build())
            .Concat(AiValuationInvoiceTools.Build())
            .Concat(AiMailboxTools.Build())
            .Concat(AiFinanceTools.Build())
            .Concat(AiWeeklyCashflowGridTool.Build())
            .Concat(AiLabourMonthEndTools.Build())
            .Concat(AiRegisterTools.Build())
            .Concat(AiKpiTools.Build())
            .Concat(AiSalesTools.Build())
            .Concat(AiDeliveryTools.Build())
            .Concat(AiSkillTools.Build())
            .Concat(AiWriteTools.Build())
            .Concat(AiActionGatewayTools.Build())
            .Concat(AiPageGuideTools.Build())
            .ToList();

    /// <summary>
    /// The catalogue this caller's AI tool is told about over the MCP connector: every tool whose
    /// backing query admits one of their roles, and nothing else — a tool the caller could not use
    /// is never described, so the model cannot promise something it will then be refused
    /// (the ADR-002 rule, carried over from the retired in-portal chat).
    /// </summary>
    public static IReadOnlyList<AiTool> ForConnector(SignedInUser user) =>
        All.Where(tool => tool.VisibleTo.IncludesAny(user.Roles)).ToList();

    /// <summary>By current name, or by a name the tool used to have (AiLegacyNames — the 2026-09-03
    /// Drawings → Documents rename), so a saved skill or an old habit still lands.</summary>
    public static AiTool? Find(string name)
    {
        var current = AiLegacyNames.Current(name);
        return All.FirstOrDefault(tool => string.Equals(tool.Name, current, StringComparison.OrdinalIgnoreCase));
    }

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);

    private static string NotFound(string message) => Serialise(new { ok = false, error = message });

    private static IReadOnlyList<AiTool> Build() =>
        ContextTools()
            .Concat(RecordsTools())
            .Concat(ProcurementTools())
            .Concat(SiteWorkTools())
            .Concat(TodoBriefTools())
            .Concat(LookupTools())
            .Concat(RequestContextTools())
            .Concat(MastersTools())
            .ToList();

    /// <summary>Project reference per id, for labelling cross-project matches. Blank ids (a
    /// company-wide to-do) are skipped rather than queried.</summary>
    private static async Task<Dictionary<string, string>> ProjectReferenceMapAsync(
        AiToolContext context, IEnumerable<string> projectIds, CancellationToken ct)
    {
        var ids = projectIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<string, string>();
        return await context.Db.Projects.AsNoTracking()
            .Where(row => ids.Contains(row.ProjectId))
            .ToDictionaryAsync(row => row.ProjectId, row => row.Reference, ct);
    }

    /// <summary>The named project, else the one in scope, else null.</summary>
    private static async Task<Data.Entities.ProjectEntity?> ResolveProjectAsync(
        AiToolContext context, string? projectId, CancellationToken ct)
    {
        var id = string.IsNullOrWhiteSpace(projectId) ? context.Scope?.ProjectId : projectId;
        if (string.IsNullOrWhiteSpace(id)) return null;
        return await context.Db.Projects.AsNoTracking().FirstOrDefaultAsync(row => row.ProjectId == id, ct);
    }
}

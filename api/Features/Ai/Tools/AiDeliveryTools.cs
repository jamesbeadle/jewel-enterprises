using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The delivery-side read surface (2026-08-31): the project calendar, building control, the
/// programme with its LAD claims, the Architect's Instruction register, progress updates/reports,
/// the drawing register, a drawing's structured read (2026-09-07) and the package reconciliation. Each tool wraps the SAME query handler its
/// HTTP endpoint composes and mirrors that endpoint's role gate exactly.
/// </summary>
internal static partial class AiDeliveryTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    /// <summary>Mirror of GetProgrammeDetailEndpoint.RolesThatMayReadSite and
    /// ListLadClaimsForProjectEndpoint.InternalReadRoles — both are JpmsRoleSets.AllInternal.</summary>
    private static readonly RoleSet ProgrammeReaders = JpmsRoleSets.AllInternal;

    /// <summary>Mirror of ReconciliationPackageQueryEndpoints.InternalReadRoles.</summary>
    private static readonly RoleSet ReconciliationReaders = JpmsRoleSets.AllInternal;

    /// <summary>Mirror of the drawing query endpoints' RolesThatMayReadDrawings
    /// (ListDrawingsForProjectEndpoint / ListDrawingFoldersForProjectEndpoint /
    /// ListRevisionsForDrawingEndpoint) — all JpmsRoleSets.DrawingReaders.</summary>
    private static readonly RoleSet DrawingReaders = JpmsRoleSets.DrawingReaders;

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    private static string? ProjectId(AiToolContext context, JsonElement input) =>
        AiToolSchema.Text(input, "projectId") ?? context.Scope?.ProjectId;

    private const string NoProject = "Say which project: pass projectId (list_projects returns ids).";

    /// <summary>The same query handler the tool's HTTP endpoint composes.</summary>
    private static Task<TResult> Query<TQuery, TResult>(AiToolContext context, TQuery query, CancellationToken ct)
        where TQuery : IQuery<TResult> =>
        context.Services.GetRequiredService<IQueryHandler<TQuery, TResult>>().HandleAsync(query, ct);

    public static IReadOnlyList<AiTool> Build() => new[]
    {
        ListCalendarEvents(),
        GetBuildingControl(),
        GetProgramme(),
        ListArchitectInstructions(),
        ListProgress(),
        ListDrawings(),
        GetDocumentExtraction(),
        GetPackageReconciliation(),
    };
}


namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>What a tool can see when it runs — one MCP tools/call. <paramref name="Services"/> is
/// the request scope, so a tool can resolve a feature service (RequestContextAssembler,
/// RequestEmailReader) rather than re-implementing it. <paramref name="Scope"/> is always null over
/// the connector (there is no page in view); it survives only so the tools' "defaults to the
/// record in view" fallbacks keep compiling.</summary>
public sealed record AiToolContext(
    JpmsContext Db, SignedInUser User, AiScope? Scope, IServiceProvider Services);

public enum AiToolKind
{
    /// <summary>Executes server-side and its result goes back to the model.</summary>
    Read = 0,
    /// <summary>Returned to the browser to execute. Never touches the server. Retired with the
    /// in-portal chat (2026-08-27) — kept so the enum's persisted meaning never shifts.</summary>
    Ui = 1,
    /// <summary>Executes server-side and CHANGES something, through the same authorisation,
    /// validation and command handler its HTTP endpoint uses. Marked so the connector can
    /// annotate it as non-read-only.</summary>
    Write = 2
}

/// <summary>
/// A tool as the model sees it, plus how to run it.
///
/// <para>Writes are deliberately absent from this version. Every tool here is a read or a UI
/// instruction, so the worst outcome of a bad model turn is a wasted call or an unexpected page.
/// Write tools arrive with the proposal card (see docs/ai/00-agent-architecture.md §4).</para>
/// </summary>
public sealed record AiTool(
    string Name,
    string Description,
    object InputSchema,
    AiToolKind Kind,
    RoleSet VisibleTo,
    Func<AiToolContext, JsonElement, CancellationToken, Task<string>> ExecuteAsync);

/// <summary>Helpers for the tiny hand-written JSON schemas the tools declare.</summary>
public static class AiToolSchema
{
    public static object Object(params (string Name, string Type, string Description, bool Required)[] properties)
    {
        var props = new Dictionary<string, object>();
        var required = new List<string>();
        foreach (var (name, type, description, isRequired) in properties)
        {
            props[name] = new { type, description };
            if (isRequired) required.Add(name);
        }
        return new
        {
            type = "object",
            properties = props,
            required = required.ToArray()
        };
    }

    public static object Empty() => new { type = "object", properties = new Dictionary<string, object>() };

    /// <summary>Reads a string argument, tolerating a missing or null property.</summary>
    public static string? Text(JsonElement input, string name) =>
        input.ValueKind == JsonValueKind.Object
        && input.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>Reads a boolean argument, tolerating a missing or null property — and the string
    /// "true"/"false" a model sometimes sends for a flag.</summary>
    public static bool? Flag(JsonElement input, string name)
    {
        if (input.ValueKind != JsonValueKind.Object || !input.TryGetProperty(name, out var value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null
        };
    }

    public static int? Number(JsonElement input, string name) =>
        input.ValueKind == JsonValueKind.Object
        && input.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Number
        && value.TryGetInt32(out var parsed)
            ? parsed
            : null;
}

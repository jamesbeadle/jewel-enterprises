namespace Jewel.JPMS.Api.Features.Ai;

/// <summary>
/// The carrier for an IMAGE flowing back through a tool result — read_email_attachment on a
/// photo, a drawing, a marked-up plan. Tool rows persist as plain strings, so the image rides as
/// a marker line, its media type, its file name and the base64, and the MCP endpoint turns the
/// row into a real image content block in the tools/call result — the model SEES the picture.
///
/// <para>The budget pass swaps the body for <see cref="BudgetStandIn"/> before counting: the
/// base64 is megabytes of characters but ~1,600 tokens of image, and counted raw it would blow
/// the transcript budget on its own and stub every other tool row in the conversation.</para>
/// </summary>
internal static class AiImageToolResult
{
    /// <summary>First line of a carrying row. U+0001 so no honest tool output can collide.</summary>
    public const string Marker = "\u0001image";

    public const string BudgetStandIn = "(image tool result — replays as an image block)";

    public static string Build(string fileName, string mediaType, byte[] content) =>
        // Re-encoded from the decoded bytes, not echoed: the API wants canonical base64.
        $"{Marker}\n{mediaType}\n{fileName}\n{Convert.ToBase64String(content)}";

    public static bool IsImage(string? body) =>
        body is not null && body.StartsWith(Marker + "\n", StringComparison.Ordinal);

    public static bool TryParse(string body, out string mediaType, out string fileName, out string base64)
    {
        mediaType = fileName = base64 = "";
        var parts = body.Split('\n', 4);
        if (parts.Length != 4) return false;
        mediaType = parts[1].Trim();
        fileName = parts[2].Trim();
        base64 = parts[3].Trim();
        return mediaType.Length > 0 && base64.Length > 0;
    }
}

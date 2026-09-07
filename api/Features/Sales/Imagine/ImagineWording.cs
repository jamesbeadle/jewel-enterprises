using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Sales.Imagine;

/// <summary>How the imagine page reads what the prospect typed and records what they did.</summary>
internal static class ImagineWording
{
    public static bool LooksLikeEmail(string value) =>
        value.Length is > 5 and <= 256 && value.IndexOf('@') > 0 && value.LastIndexOf('.') > value.IndexOf('@') && !value.Any(char.IsWhiteSpace);

    public static string FirstName(string contactName)
    {
        var name = (contactName ?? "").Trim();
        if (name.Length == 0) return "";
        // "Mr & Mrs Harding" → "Mr & Mrs Harding"; "Sarah Harding" → "Sarah".
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && !name.Contains('&') && !IsTitle(parts[0]) ? parts[0] : name;
    }

    public static bool IsTitle(string word) =>
        word.TrimEnd('.').ToLowerInvariant() is "mr" or "mrs" or "ms" or "miss" or "dr" or "sir" or "lady" or "lord";

    public static LeadActivityEntity Activity(string leadId, string summary) => new()
    {
        LeadActivityId = Guid.NewGuid().ToString("N"),
        LeadId = leadId,
        Kind = (int)LeadActivityKind.Imagine,
        Summary = Clip(summary, 4000),
        OccurredAt = DateTimeOffset.UtcNow,
        RecordedByEmail = "imagine@jpms"
    };

    public static string Clip(string value, int max) => value.Length <= max ? value : value[..(max - 1)] + "…";
}

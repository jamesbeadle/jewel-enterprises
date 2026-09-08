using System.Text;

namespace Jewel.JPMS.Api.Features.Site.Drafts;

/// <summary>
/// The one-shot prompt that asks Claude to map a draft's still-unmatched programme tasks to the
/// claim's cost centres, and the parser for what comes back. The model sees only what a
/// quantity surveyor would: the task titles, and each centre's code, name, completion and a few
/// of its line descriptions — enough to read "Ground Floor — Insulation, UFH & Screed" as three
/// centres and "Entrance Gate Survey" as none. Strict JSON, same convention as every other
/// one-shot prompt in the API; codes outside the claim are dropped by the parser, never trusted.
/// </summary>
internal static class ProgrammeDraftSuggestionPrompt
{
    public sealed record Suggestion(string ProgrammeTaskId, IReadOnlyList<string> CostCodes, string Why);

    private const int MaxSampleDescriptions = 4;
    private const int MaxWhyChars = 200;

    public const string System =
        "You are a quantity surveyor's assistant for a UK residential main contractor. You are given the "
        + "tasks on a construction programme that could not be matched by trade word, and the cost centres "
        + "on the project's valuation report (each a trade bucket of priced bill lines, with how complete "
        + "it is). Map each task to the cost centre(s) whose lines contain the work the task describes, so "
        + "the centre's percentage complete can be proposed as the task's progress.\n\n"
        + "Rules:\n"
        + "- Only use codes from the list given. Never invent one.\n"
        + "- A task title is usually \"where — what\" (\"First Floor — Plumbing 2nd Fix\"); the where does "
        + "not matter (the bill is not split by location), the what is the trade.\n"
        + "- Prefer one centre. Add more only when the task plainly spans trades (\"Insulation, UFH & "
        + "Screed\" is insulation, underfloor heating and screed).\n"
        + "- Leave a task unmapped (empty costCodes) when nothing on the bill covers it — snagging, "
        + "handover, surveys, client items — rather than guessing.\n"
        + "- Judge from the words and the line descriptions, not from percentages.\n\n"
        + "Answer with STRICT JSON only — no markdown fences, no commentary — one entry per task:\n"
        + "{\"mappings\":[{\"taskId\":\"…\",\"costCodes\":[\"…\"],\"why\":\"… (under 20 words)\"}]}";

    public static string User(
        IReadOnlyList<(string ProgrammeTaskId, string Title)> tasks,
        IReadOnlyList<ProgrammeDraftCostCentre> centres,
        IReadOnlyDictionary<string, IReadOnlyList<string>> sampleDescriptionsByCode)
    {
        var builder = new StringBuilder();
        builder.AppendLine("PROGRAMME TASKS TO MAP (id | title):");
        foreach (var task in tasks)
            builder.AppendLine($"- {task.ProgrammeTaskId} | {task.Title}");
        builder.AppendLine();

        builder.AppendLine("COST CENTRES ON THE CLAIM (code | name | % complete | £ amount | sample lines):");
        foreach (var centre in centres)
        {
            var samples = sampleDescriptionsByCode.TryGetValue(centre.CostCode, out var descriptions)
                ? string.Join("; ", descriptions.Take(MaxSampleDescriptions))
                : "";
            builder.AppendLine($"- {centre.CostCode} | {centre.Name} | {centre.Percent:0.#}% | £{centre.Amount:N0} | {samples}");
        }
        builder.AppendLine();
        builder.AppendLine("Map every task listed. STRICT JSON only.");
        return builder.ToString();
    }

    /// <summary>Empty when nothing usable came back. Tolerates fences and prose around the JSON;
    /// keeps only tasks that were asked about and codes that are on the claim.</summary>
    public static IReadOnlyList<Suggestion> Parse(
        string? responseText, IReadOnlyCollection<string> askedTaskIds, IReadOnlyCollection<string> presentCodes)
    {
        if (string.IsNullOrWhiteSpace(responseText)) return Array.Empty<Suggestion>();

        var start = responseText.IndexOf('{');
        var end = responseText.LastIndexOf('}');
        if (start < 0 || end <= start) return Array.Empty<Suggestion>();

        var asked = new HashSet<string>(askedTaskIds, StringComparer.OrdinalIgnoreCase);
        var present = new HashSet<string>(presentCodes, StringComparer.OrdinalIgnoreCase);
        var suggestions = new List<Suggestion>();
        try
        {
            using var document = JsonDocument.Parse(responseText[start..(end + 1)]);
            if (!document.RootElement.TryGetProperty("mappings", out var mappings) || mappings.ValueKind != JsonValueKind.Array)
                return Array.Empty<Suggestion>();

            foreach (var mapping in mappings.EnumerateArray())
            {
                var taskId = mapping.TryGetProperty("taskId", out var idElement) && idElement.ValueKind == JsonValueKind.String
                    ? idElement.GetString() ?? ""
                    : "";
                if (!asked.Contains(taskId)) continue;

                var codes = mapping.TryGetProperty("costCodes", out var codesElement) && codesElement.ValueKind == JsonValueKind.Array
                    ? codesElement.EnumerateArray()
                        .Where(code => code.ValueKind == JsonValueKind.String)
                        .Select(code => (code.GetString() ?? "").Trim())
                        .Where(present.Contains)
                        .Select(code => presentCodes.First(known => string.Equals(known, code, StringComparison.OrdinalIgnoreCase)))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList()
                    : new List<string>();

                var why = mapping.TryGetProperty("why", out var whyElement) && whyElement.ValueKind == JsonValueKind.String
                    ? (whyElement.GetString() ?? "").Trim()
                    : "";
                if (why.Length > MaxWhyChars) why = why[..MaxWhyChars];

                suggestions.Add(new Suggestion(taskId, codes.AsReadOnly(), why));
            }
        }
        catch (JsonException)
        {
            return Array.Empty<Suggestion>();
        }

        return suggestions.AsReadOnly();
    }
}

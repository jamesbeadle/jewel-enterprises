using System.Text;

namespace Jewel.JPMS.Models;

/// <summary>
/// One thing that went wrong, in the form a user can hand to whoever will fix it.
///
/// The split matters: <see cref="Summary"/> is what the user reads and is written for them, while
/// everything else exists so that "it broke" can become a diagnosis without a phone call. The
/// reference is short and pronounceable on purpose — it gets read down a phone and typed into a
/// message far more often than it gets copied.
/// </summary>
public sealed record ErrorReport(
    string Reference,
    DateTimeOffset OccurredAt,
    string Summary,
    string? Detail = null,
    string? Operation = null,
    string? HttpMethod = null,
    string? RequestPath = null,
    int? StatusCode = null,
    string? ExceptionType = null,
    string? StackTrace = null,
    string? Page = null,
    string? User = null)
{
    /// <summary>Six hex characters — enough to be unique among the handful of errors one person
    /// hits in a day, short enough to read aloud.</summary>
    public static string NewReference() =>
        $"JPMS-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    /// <summary>True when there is anything beyond the summary worth expanding.</summary>
    public bool HasDetail =>
        !string.IsNullOrWhiteSpace(Detail)
        || RequestPath is not null
        || StatusCode is not null
        || !string.IsNullOrWhiteSpace(StackTrace);

    /// <summary>The one-line "where" shown under the summary, e.g. "POST /api/directory · 500".</summary>
    public string? Endpoint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(RequestPath)) return null;
            var method = string.IsNullOrWhiteSpace(HttpMethod) ? "" : $"{HttpMethod} ";
            var status = StatusCode is null ? "" : $" · {StatusCode}";
            return $"{method}{RequestPath}{status}";
        }
    }

    /// <summary>
    /// The whole report as plain text, for the Copy button. Deliberately readable when pasted into
    /// an email or a WhatsApp message rather than machine-shaped — the recipient is a person.
    /// </summary>
    public string ToPlainText()
    {
        var text = new StringBuilder();
        text.AppendLine($"JPMS error {Reference}");
        // Local time with its real offset, then the UTC clock — StandardName says "GMT" all year,
        // which in summer mislabels a BST time by an hour for anyone matching it against Azure logs.
        var local = OccurredAt.ToLocalTime();
        text.AppendLine($"When:   {local:dd MMM yyyy, HH:mm:ss} (UTC{local:zzz}) · {OccurredAt.ToUniversalTime():HH:mm:ss} UTC");
        if (!string.IsNullOrWhiteSpace(User)) text.AppendLine($"Who:    {User}");
        if (!string.IsNullOrWhiteSpace(Page)) text.AppendLine($"Page:   {Page}");
        text.AppendLine($"What:   {Summary}");
        if (!string.IsNullOrWhiteSpace(Operation)) text.AppendLine($"Doing:  {Operation}");
        if (Endpoint is not null) text.AppendLine($"Where:  {Endpoint}");
        if (!string.IsNullOrWhiteSpace(Detail)) text.AppendLine($"Detail: {Detail}");
        if (!string.IsNullOrWhiteSpace(ExceptionType)) text.AppendLine($"Type:   {ExceptionType}");
        if (!string.IsNullOrWhiteSpace(StackTrace))
        {
            text.AppendLine("Stack:");
            text.AppendLine(StackTrace.Trim());
        }
        return text.ToString();
    }
}

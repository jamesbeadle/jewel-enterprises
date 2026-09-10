using System.Text.RegularExpressions;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.Todos;

/// <summary>
/// The to-do brief's reasoning (2026-09-10, the accountant's ask: "show me the to-do for
/// Ravenswood" should come back as what is open AND what clears each item, not a list of
/// titles). Pure — no context, no clock — so the rules are testable on their own and the
/// connector tool (get_todo_brief) only assembles.
///
/// <para>Three readings of an item, in order of trust. What it is ABOUT (AboutRecord) is a fact
/// someone set. What it NAMES ("raise V31", "chase BPI-0056") is a reference in its own words
/// (RecordReferenceScan — the same grammar the completion tagger files by). What it CONCERNS is
/// an inference: the wording says what kind of record the work should produce (<see cref="Intent"/>)
/// and the distinctive words of the title are matched against the project's records of that
/// kind, so "raise water softener variation" finds the water-softener variation if one exists
/// and reports its status — or reports that none does, which is the gap. An inference is always
/// labelled as one in the output.</para>
/// </summary>
public static class TodoBrief
{
    /// <summary>What the item's wording says the work is: the leading verb and the kind of
    /// record it should produce or act on. Null members when the wording says nothing.</summary>
    public sealed record Intent(string? Verb, RecordType? Expects, string Subject);

    /// <summary>A project record the item names or plausibly concerns. <paramref name="Named"/>
    /// is true when the item cites the reference itself; false means the match is on the
    /// title's words (<paramref name="MatchedWords"/>) and is an inference.</summary>
    public sealed record RelatedRecord(LinkableRecord Record, IReadOnlyList<string> MatchedWords, bool Named);

    // The verbs people open a to-do with. Matched at the start of the title (after any
    // reference or filler), lower-cased. Order is only the order of the table.
    private static readonly (string Verb, string Pattern)[] Verbs =
    {
        ("raise", @"^(raise|create|draft|prepare|put together|write up|issue)\b"),
        ("chase", @"^(chase|follow[- ]?up( on| with)?|remind|nudge|push)\b"),
        ("order", @"^(order|buy|purchase|procure|source)\b"),
        ("send", @"^(send|email|forward|circulate|return|submit)\b"),
        ("confirm", @"^(confirm|check|verify|agree|clarify|review|approve|sign off)\b"),
        ("book", @"^(book|arrange|organise|organize|schedule|set up)\b"),
        ("pay", @"^(pay|invoice|bill|settle)\b"),
    };

    // The kinds of record a title can name by its noun. The match is on the whole title, so
    // "raise water softener variation" reads Variation from its last word.
    private static readonly (RecordType Type, string Pattern)[] Nouns =
    {
        (RecordType.Variation,        @"\b(variations?|vos?\b|voqs?|variation orders?)\b"),
        (RecordType.BidPackageInvite, @"\b(quotes?|quotations?|tenders?|bid packages?|rft|prices? from)\b"),
        (RecordType.WorkOrder,        @"\b(work orders?|purchase orders?|\bpos?\b|wo-?\d*)\b"),
        (RecordType.Request,          @"\b(rfis?|rfas?|nods?|eots?|requests? for information|architect'?s? instruction)\b"),
        (RecordType.Defect,           @"\b(defects?|snags?|snagging)\b"),
    };

    // Words that say nothing about WHICH record an item concerns: grammar, the verbs above,
    // the nouns above, and the project vocabulary every title carries.
    private static readonly HashSet<string> Noise = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "from", "with", "once", "to", "of", "a", "an", "on", "in", "at", "by",
        "up", "is", "be", "as", "or", "not", "all", "any", "per", "our", "we", "it", "this", "that",
        "then", "re", "via", "into", "out", "off", "over", "after", "before", "when", "if", "so",
        "raise", "raised", "raising", "create", "chase", "chased", "chasing", "order", "ordered",
        "send", "sent", "email", "confirm", "confirmed", "check", "book", "arrange", "follow",
        "get", "ask", "call", "ring", "sort", "do", "make", "need", "needs", "needed", "please",
        "update", "review", "issue", "issued", "prepare", "submit", "pay", "invoice", "invoices",
        "variation", "variations", "quote", "quotes", "quotation", "tender", "tenders", "package",
        "work", "works", "wo", "po", "rfi", "request", "requests", "defect", "defects", "todo",
        "site", "manager", "project", "client", "architect", "supplier", "company", "spec",
        "new", "once", "also", "still", "again", "asap", "urgent", "today", "tomorrow", "week",
    };

    private static readonly Regex Word = new(@"[a-z][a-z0-9'&-]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>What the title says the work is. The subject is the title with the verb and the
    /// record noun stripped — the words the reader would use to say what it is about.</summary>
    public static Intent ReadIntent(string? title)
    {
        var text = (title ?? "").Trim();
        var lower = text.ToLowerInvariant();

        string? verb = null;
        foreach (var (name, pattern) in Verbs)
        {
            if (Regex.IsMatch(lower, pattern, RegexOptions.CultureInvariant)) { verb = name; break; }
        }

        RecordType? expects = null;
        foreach (var (type, pattern) in Nouns)
        {
            if (Regex.IsMatch(lower, pattern, RegexOptions.CultureInvariant)) { expects = type; break; }
        }

        // "order radiators from BTU" produces nothing in the portal until a work order is raised;
        // an ORDER verb with no noun means a work order.
        if (expects is null && verb == "order") expects = RecordType.WorkOrder;

        var subject = string.Join(' ', SubjectWords(text));
        return new Intent(verb, expects, subject);
    }

    /// <summary>The distinctive words of a title — everything that is not grammar or the
    /// portal's own vocabulary — lightly stemmed so "radiators" meets "radiator".</summary>
    public static IReadOnlyList<string> SubjectWords(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return Array.Empty<string>();
        var words = new List<string>();
        foreach (Match match in Word.Matches(title))
        {
            var word = match.Value.Trim('\'', '-', '&');
            if (word.Length < 3 || Noise.Contains(word)) continue;
            // A reference ("V31", "BPI-0056") is handled by RecordReferenceScan, not as a word.
            if (Regex.IsMatch(word, @"^[a-z]{1,4}-?\d+$", RegexOptions.IgnoreCase)) continue;
            var lower = word.ToLowerInvariant();
            if (!words.Contains(lower, StringComparer.OrdinalIgnoreCase)) words.Add(lower);
        }
        return words;
    }

    private static string Stem(string word)
    {
        var lower = word.ToLowerInvariant();
        if (lower.Length > 5 && lower.EndsWith("ing", StringComparison.Ordinal)) return lower[..^3];
        if (lower.Length > 4 && lower.EndsWith("ies", StringComparison.Ordinal)) return lower[..^3] + "y";
        if (lower.Length > 4 && lower.EndsWith("es", StringComparison.Ordinal) && !lower.EndsWith("ses", StringComparison.Ordinal)) return lower[..^2];
        if (lower.Length > 3 && lower.EndsWith("s", StringComparison.Ordinal) && !lower.EndsWith("ss", StringComparison.Ordinal)) return lower[..^1];
        return lower;
    }

    private static bool WordsMeet(string left, string right)
    {
        var a = Stem(left);
        var b = Stem(right);
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return true;
        // "soften" ~ "softener": one is a prefix of the other and both are long enough to mean
        // something. Never for short words — "ord" would meet "order".
        var shorter = a.Length <= b.Length ? a : b;
        var longer = ReferenceEquals(shorter, a) ? b : a;
        return shorter.Length >= 5 && longer.StartsWith(shorter, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The project records the item names (by reference in its title or notes) or
    /// concerns (by the title's words against the records' titles), named ones first, then the
    /// strongest word matches. At most <paramref name="take"/> inferred matches; every named
    /// one is kept. A record whose title shares only one short word with the item is not a match.</summary>
    public static IReadOnlyList<RelatedRecord> Relate(
        TodoItemEntity item, IEnumerable<LinkableRecord> candidates, int take = 3)
    {
        var named = new HashSet<string>(StringComparer.Ordinal);
        named.UnionWith(RecordReferenceScan.Keys(item.Title));
        named.UnionWith(RecordReferenceScan.Keys(item.Notes));

        var words = SubjectWords(item.Title);
        var intent = ReadIntent(item.Title);

        var results = new List<(RelatedRecord Record, int Score)>();
        foreach (var candidate in candidates)
        {
            var key = RecordReferenceScan.KeyOfReference(candidate.Reference);
            if (key is not null && named.Contains(key))
            {
                results.Add((new RelatedRecord(candidate, Array.Empty<string>(), Named: true), int.MaxValue));
                continue;
            }

            if (words.Count == 0) continue;
            var candidateWords = SubjectWords(candidate.Title);
            if (candidateWords.Count == 0) continue;
            var matched = words.Where(word => candidateWords.Any(other => WordsMeet(word, other))).ToList();
            if (matched.Count == 0) continue;
            // One word carries a match only when it is distinctive enough to be the subject
            // ("window", "radiator" — never "floor" or "wall" alone).
            if (matched.Count == 1 && matched[0].Length < 6) continue;

            var score = matched.Sum(word => word.Length);
            // The record kind the wording expects outranks a same-score match of another kind;
            // a live record outranks a finished one.
            if (intent.Expects is { } expected && SameFamily(candidate.Type, expected)) score += 10;
            if (candidate.IsActive) score += 1;
            results.Add((new RelatedRecord(candidate, matched, Named: false), score));
        }

        var namedRecords = results.Where(pair => pair.Record.Named)
            .OrderBy(pair => pair.Record.Record.Reference, StringComparer.OrdinalIgnoreCase)
            .Select(pair => pair.Record);
        var inferred = results.Where(pair => !pair.Record.Named)
            .OrderByDescending(pair => pair.Score)
            .ThenByDescending(pair => ReferenceNumber(pair.Record.Record.Reference))
            .ThenBy(pair => pair.Record.Record.Reference, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .Select(pair => pair.Record);
        return namedRecords.Concat(inferred).ToList();
    }

    // "BPI-0056" → 56, "V3" → 3: of two equally good matches the newer record is the likelier one.
    private static int ReferenceNumber(string? reference)
    {
        var key = RecordReferenceScan.KeyOfReference(reference);
        return key is not null && int.TryParse(key[(key.IndexOf(':') + 1)..], out var number) ? number : 0;
    }

    /// <summary>The condition the wording puts on the work — "once confirmed spec", "when the
    /// client agrees" — or null. A conditional item is not simply overdue: the condition may be
    /// what is outstanding.</summary>
    public static string? Condition(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;
        var match = ConditionClause.Match(title);
        return match.Success ? match.Value.Trim() : null;
    }

    private static readonly Regex ConditionClause = new(
        @"\b(once|when|after|pending|awaiting|subject to|if|unless|as soon as)\b.+$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Other open items on the same project that share a distinctive word of the
    /// subject with this one — "Order radiators from BTU" beside "Raise Radiator supply
    /// Variation" — so the reader sees the pair and can tell which waits on which.</summary>
    public static IReadOnlyList<TodoItemEntity> RelatedItems(TodoItemEntity item, IEnumerable<TodoItemEntity> others)
    {
        var words = SubjectWords(item.Title).Where(word => word.Length >= 6).ToList();
        if (words.Count == 0) return Array.Empty<TodoItemEntity>();
        return others
            .Where(other => other.TodoItemId != item.TodoItemId && other.ProjectId == item.ProjectId)
            .Where(other => SubjectWords(other.Title).Any(word => words.Any(mine => WordsMeet(mine, word))))
            .OrderBy(other => other.Number)
            .ToList();
    }

    /// <summary>A variation is one document whichever identity the link layer hands it out
    /// under (VariationQuote before approval, Variation after).</summary>
    public static bool SameFamily(RecordType a, RecordType b)
    {
        static RecordType Family(RecordType type) =>
            type == RecordType.VariationQuote ? RecordType.Variation : type;
        return Family(a) == Family(b);
    }

    /// <summary>The one-line reading of what clears the item, from the facts above. Deterministic,
    /// so the same board reads the same way in every chat; the model may expand on it but should
    /// not contradict it. <paramref name="about"/> is the record the item is about, when set and
    /// resolved; <paramref name="related"/> is <see cref="Relate"/>'s answer.</summary>
    public static string NextStep(
        TodoItemEntity item, Intent intent, LinkableRecord? about, IReadOnlyList<RelatedRecord> related)
    {
        var step = NextStepCore(item, intent, about, related);
        return Condition(item.Title) is { } condition
            ? $"{step} First confirm the condition in the wording is met (\"{condition}\")."
            : step;
    }

    private static string NextStepCore(
        TodoItemEntity item, Intent intent, LinkableRecord? about, IReadOnlyList<RelatedRecord> related)
    {
        var expected = intent.Expects;
        var kindName = expected is { } kind ? KindName(kind) : null;

        // A record the item is ABOUT is the strongest fact: read its status.
        if (about is not null)
        {
            return about.IsActive
                ? $"About {about.Reference} ({about.StatusLabel}) — progress that record; the item clears when it does."
                : $"About {about.Reference}, which is {about.StatusLabel.ToLowerInvariant()} — this item is probably done; confirm and mark it done.";
        }

        var named = related.Where(r => r.Named).ToList();
        if (named.Count > 0)
        {
            var record = named[0].Record;
            return record.IsActive
                ? $"Names {record.Reference} ({record.StatusLabel}) — progress that record; the item clears when it does."
                : $"Names {record.Reference}, which is {record.StatusLabel.ToLowerInvariant()} — this item is probably done; confirm and mark it done.";
        }

        if (expected is { } expectedKind)
        {
            var ofKind = related.Where(r => SameFamily(r.Record.Type, expectedKind)).ToList();
            if (ofKind.Count == 0)
            {
                return intent.Verb switch
                {
                    "raise" or "order" or null => $"No {kindName} on this project matches \"{intent.Subject}\" — raise it.",
                    "chase" => $"No {kindName} on this project matches \"{intent.Subject}\" — nothing in the portal records what is being chased; raise or link the record, then chase from it.",
                    _ => $"No {kindName} on this project matches \"{intent.Subject}\" — raise or link one so the item can be tracked.",
                };
            }

            var best = ofKind[0].Record;
            var status = best.StatusLabel;
            var lowerStatus = status.ToLowerInvariant();
            return expectedKind switch
            {
                RecordType.Variation => lowerStatus switch
                {
                    "quoting" or "draft" => $"{best.Reference} \"{best.Title}\" exists and is still {lowerStatus} — price and issue it, then mark this done. (Inferred from the title — confirm it is the same variation.)",
                    "issued" => $"{best.Reference} \"{best.Title}\" is issued and awaiting the architect — chase the instruction; the raise is done. (Inferred from the title.)",
                    "approved" or "rejected" => $"{best.Reference} \"{best.Title}\" is {lowerStatus} — this item looks done; confirm and mark it done. (Inferred from the title.)",
                    _ => $"{best.Reference} \"{best.Title}\" is {lowerStatus}. (Inferred from the title.)",
                },
                RecordType.BidPackageInvite => lowerStatus switch
                {
                    "draft" => $"{best.Reference} \"{best.Title}\" is still Draft — no invite has gone out through the portal, so there is nothing to chase yet: send the invites from the package, or record the quote against it if it arrived by other means. (Inferred from the title.)",
                    "inviting" => $"{best.Reference} \"{best.Title}\" is Inviting — chase the recipients who have not responded (get_bid_package_context lists them). (Inferred from the title.)",
                    "quotesreceived" => $"Quotes are in on {best.Reference} \"{best.Title}\" — review and award; this chase may be done. (Inferred from the title.)",
                    "awarded" or "closed" => $"{best.Reference} \"{best.Title}\" is {lowerStatus} — this item looks done; confirm and mark it done. (Inferred from the title.)",
                    _ => $"{best.Reference} \"{best.Title}\" is {lowerStatus}. (Inferred from the title.)",
                },
                RecordType.WorkOrder => best.IsActive
                    ? $"{best.Reference} \"{best.Title}\" is {lowerStatus} — the order exists; this item clears when it is released/acknowledged. (Inferred from the title.)"
                    : $"{best.Reference} \"{best.Title}\" is {lowerStatus} — this item looks done; confirm and mark it done. (Inferred from the title.)",
                _ => best.IsActive
                    ? $"{best.Reference} \"{best.Title}\" ({status}) probably is this — progress it; the item clears when it does. (Inferred from the title.)"
                    : $"{best.Reference} \"{best.Title}\" is {lowerStatus} — this item looks done; confirm and mark it done. (Inferred from the title.)",
            };
        }

        return "Nothing in the portal tracks this — it clears by doing what the title says and marking it done. Link it to a record if one exists.";
    }

    public static string KindName(RecordType type) => type switch
    {
        RecordType.Variation or RecordType.VariationQuote => "variation",
        RecordType.BidPackageInvite => "bid package",
        RecordType.WorkOrder => "work order",
        RecordType.Request => "request",
        RecordType.Defect => "defect",
        _ => type.ToString().ToLowerInvariant(),
    };

    /// <summary>The item's standing against the calendar: days overdue (positive), due today
    /// (zero), or days until due (negative); null with no due date.</summary>
    public static int? DaysOverdue(TodoItemEntity item, DateOnly today)
    {
        if (item.DueAt is not { } due) return null;
        var dueDay = DateOnly.FromDateTime(due.ToUniversalTime().Date);
        return today.DayNumber - dueDay.DayNumber;
    }

    /// <summary>The short facts a reader wants beside the item, deterministic and in reading
    /// order: the calendar first, then ownership, then what is (not) linked, then activity.</summary>
    public static IReadOnlyList<string> Signals(
        TodoItemEntity item, DateOnly today, LinkableRecord? about, IReadOnlyList<RelatedRecord> related,
        int taggedEmails, DateTimeOffset? lastEmailAt, DateTimeOffset? lastActivityAt,
        IReadOnlyList<TodoItemEntity>? relatedItems = null)
    {
        var signals = new List<string>();

        switch (DaysOverdue(item, today))
        {
            case > 0 and var days: signals.Add($"Overdue by {days} day{(days == 1 ? "" : "s")}."); break;
            case 0: signals.Add("Due today."); break;
            case < 0 and var days: signals.Add($"Due in {-days} day{(days == -1 ? "" : "s")}."); break;
            default: signals.Add("No due date."); break;
        }

        if (Condition(item.Title) is { } condition)
            signals.Add($"Conditional — \"{condition}\": check the condition is met before treating it as late.");
        if (item.AssigneeRole is null) signals.Add("Unassigned — nobody owns it.");
        if (!item.IsComplete && item.StartedAt is { } started)
            signals.Add($"In progress since {started:d MMM}{(string.IsNullOrWhiteSpace(item.StartedByEmail) ? "" : $" ({item.StartedByEmail})")}.");

        if (about is not null) signals.Add($"About {about.Reference} ({about.StatusLabel}).");
        else if (related.Count == 0) signals.Add("Not linked to any record and names none — the wording is all there is.");
        else if (related.All(r => !r.Named)) signals.Add("Not linked to any record — related records are inferred from the title.");

        if (relatedItems is { Count: > 0 })
            signals.Add("Same subject as " + string.Join(", ", relatedItems.Select(other => $"{other.Reference} \"{other.Title}\"")) + " — check which waits on which.");

        if (taggedEmails > 0)
            signals.Add($"{taggedEmails} tagged email{(taggedEmails == 1 ? "" : "s")}{(lastEmailAt is { } last ? $", last {last:d MMM}" : "")}.");

        var since = new[] { lastActivityAt, item.StartedAt, item.CreatedAt }.Where(at => at is not null).Max()!.Value;
        var quietDays = today.DayNumber - DateOnly.FromDateTime(since.ToUniversalTime().Date).DayNumber;
        if (quietDays >= 14) signals.Add($"No activity for {quietDays} days.");

        return signals;
    }
}

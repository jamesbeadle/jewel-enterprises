using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Api.Features.Todos;
using Jewel.JPMS.Contracts.RecordLinks;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// get_todo_brief (2026-09-10, the accountant's ask): the To-do board as a brief — every open
/// item with what clears it. list_todos is the register (titles, owners, dates); this is the
/// register joined, server-side and the same way every time, to the records each item is about,
/// names or plausibly concerns (TodoBrief), its tagged mail and its timeline, with a deterministic
/// next step per item and the signals a reader wants beside it. The model renders; it does not
/// re-derive.
/// </summary>
public static partial class AiToolCatalogue
{
    // The record kinds a to-do's wording can point at. Variation's provider lists every stage
    // (pre-approval rows under their VariationQuote identity), so one read covers the book.
    private static readonly RecordType[] TodoBriefRecordTypes =
    {
        RecordType.Variation, RecordType.BidPackageInvite, RecordType.WorkOrder,
        RecordType.Request, RecordType.Defect,
    };

    // Tagged mail is a Graph call per item; beyond this many open items the brief says so and
    // leaves the mail to read_record_emails rather than running a hundred calls.
    private const int TodoBriefEmailReadCap = 30;

    private static IEnumerable<AiTool> TodoBriefTools()
    {
        var readers = JpmsRoleSets.AllInternal;

        return new List<AiTool>
        {
            new(
                "get_todo_brief",
                "The To-do board as a brief: every OPEN item on a project (or across every project) "
                + "with what clears it. Each item carries its owner, due date, days overdue, the "
                + "record it is about, the records it names or (inferred from its title) concerns "
                + "with their current status, its tagged-email count and last date, its last "
                + "timeline line, a list of short signals (overdue, unassigned, in progress, nothing "
                + "linked, quiet for N days) and ONE deterministic nextStep — what has to happen for "
                + "the item to be ticked off. Call this, not list_todos, when someone asks \"show me "
                + "the to-dos for <project>\", \"what's outstanding\", \"what do I need to clear\". "
                + "ANSWER AS A TABLE, one row per item, columns: Ref · Item · Owner · Due · What "
                + "clears it — overdue rows first and flagged, blocked/unassigned called out, and "
                + "the summary counts above the table. Use nextStep as the last column verbatim or "
                + "tightened, never contradicted; where it says \"inferred\" say so. The done pile "
                + "is left out unless includeDone is true.",
                AiToolSchema.Object(
                    ("projectId", "string",
                        "The project (list_projects returns ids). Omit for every live project plus "
                        + "company-wide items, grouped by project.", false),
                    ("role", "string",
                        "Only items assigned to this role (QuantitySurveyor, OfficeAdmin, "
                        + "FinanceDirector…). \"My to-dos\" → the caller's role from get_current_context.", false),
                    ("includeDone", "boolean",
                        "true adds the items marked done in the last 30 days as a separate list "
                        + "(no analysis — they are finished). Default false.", false)),
                AiToolKind.Read,
                readers,
                ReadTodoBriefAsync)
        };
    }

    private static async Task<string> ReadTodoBriefAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime.Date);

        var projectId = AiToolSchema.Text(input, "projectId")?.Trim();
        ProjectEntity? project = null;
        if (!string.IsNullOrWhiteSpace(projectId))
        {
            project = await ResolveProjectAsync(context, projectId, ct);
            if (project is null) return NotFound($"No project found with id {projectId} (list_projects returns ids).");
        }

        Role? role = null;
        var roleText = AiToolSchema.Text(input, "role")?.Trim();
        if (!string.IsNullOrWhiteSpace(roleText))
        {
            if (!Enum.TryParse<Role>(roleText.Replace(" ", ""), ignoreCase: true, out var parsedRole))
                return NotFound($"Unknown role \"{roleText}\" — use the enum names get_current_context returns.");
            role = parsedRole;
        }
        var includeDone = AiToolSchema.Flag(input, "includeDone") ?? false;

        var query = context.Db.TodoItems.AsNoTracking();
        if (project is not null) query = query.Where(row => row.ProjectId == project.ProjectId);
        if (role is { } wantedRole) query = query.Where(row => row.AssigneeRole == (int)wantedRole);

        var open = await query.Where(row => !row.IsComplete)
            .OrderBy(row => row.DueAt == null).ThenBy(row => row.DueAt).ThenBy(row => row.CreatedAt)
            .Take(200)
            .ToListAsync(ct);

        var doneSince = DateTimeOffset.UtcNow.AddDays(-30);
        var done = includeDone
            ? await query.Where(row => row.IsComplete && row.CompletedAt >= doneSince)
                .OrderByDescending(row => row.CompletedAt).Take(50).ToListAsync(ct)
            : new List<TodoItemEntity>();

        // Every project the items sit on, for the record joins and the labels. A company-wide
        // item (blank ProjectId) has no records to join.
        var projectIds = open.Concat(done).Select(row => row.ProjectId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        var projects = projectIds.Count == 0
            ? new Dictionary<string, ProjectEntity>()
            : await context.Db.Projects.AsNoTracking().Where(row => projectIds.Contains(row.ProjectId)).ToDictionaryAsync(row => row.ProjectId, ct);

        var providers = context.Services.GetRequiredService<RecordProviderRegistry>();
        var candidatesByProject = new Dictionary<string, List<LinkableRecord>>(StringComparer.Ordinal);
        foreach (var id in projectIds)
        {
            var candidates = new List<LinkableRecord>();
            foreach (var type in TodoBriefRecordTypes)
            {
                if (!providers.TryGet(type, out var provider)) continue;
                candidates.AddRange(await provider.ForProjectAsync(id, ct));
            }
            candidatesByProject[id] = candidates;
        }

        // The record each item is ABOUT, resolved through its provider (the same read the page makes).
        var aboutRecords = new Dictionary<string, LinkableRecord>(StringComparer.Ordinal);
        foreach (var item in open.Where(row => row.AboutRecordType is not null && !string.IsNullOrWhiteSpace(row.AboutRecordId)))
        {
            if (!providers.TryGet((RecordType)item.AboutRecordType!.Value, out var provider)) continue;
            var record = await provider.FindAsync(item.AboutRecordId!, ct);
            if (record is not null) aboutRecords[item.TodoItemId] = record;
        }

        // Last timeline line per open item, one query.
        var openIds = open.Select(row => row.TodoItemId).ToList();
        var lastActivity = openIds.Count == 0
            ? new Dictionary<string, TodoItemActivityEntity>()
            : (await context.Db.TodoItemActivities.AsNoTracking()
                    .Where(row => openIds.Contains(row.TodoItemId))
                    .OrderByDescending(row => row.OccurredAt)
                    .ToListAsync(ct))
                .GroupBy(row => row.TodoItemId)
                .ToDictionary(group => group.Key, group => group.First());

        // Tagged mail, best effort and capped — the mailbox must never turn the brief into an error.
        var emailCounts = new Dictionary<string, (int Count, DateTimeOffset? Last, string? Subject)>(StringComparer.Ordinal);
        string? emailNote = null;
        if (open.Count > TodoBriefEmailReadCap)
        {
            emailNote = $"Tagged emails were not read for {open.Count} open items (cap {TodoBriefEmailReadCap}) — "
                        + "read_record_emails record_type todo reads one item's mail.";
        }
        else
        {
            var reader = context.Services.GetRequiredService<RecordEmailReader>();
            foreach (var item in open)
            {
                try
                {
                    var messages = await reader.ForRecordAsync(RecordType.Todo, item.TodoItemId, ct);
                    var last = messages.Count == 0 ? null : messages[^1];
                    emailCounts[item.TodoItemId] = (messages.Count, last?.ReceivedAt, last?.Subject);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    emailNote ??= "Tagged emails could not be read for some items (mailbox unavailable): " + ex.Message;
                }
            }
        }

        var rows = open.Select(item =>
        {
            var candidates = candidatesByProject.TryGetValue(item.ProjectId, out var list) ? list : new List<LinkableRecord>();
            aboutRecords.TryGetValue(item.TodoItemId, out var about);
            var related = TodoBrief.Relate(item, candidates);
            var intent = TodoBrief.ReadIntent(item.Title);
            emailCounts.TryGetValue(item.TodoItemId, out var mail);
            lastActivity.TryGetValue(item.TodoItemId, out var activity);
            var daysOverdue = TodoBrief.DaysOverdue(item, today);
            var relatedItems = TodoBrief.RelatedItems(item, open);

            return new
            {
                item.TodoItemId,
                reference = item.Reference,
                item.Title,
                notes = string.IsNullOrWhiteSpace(item.Notes) ? null : item.Notes,
                status = item.StartedAt is null ? "Open" : "InProgress",
                owner = TodoActivitySummaries.AssigneeLabel(item.AssigneeRole, item.AssigneePersonEmail),
                assigneeRole = item.AssigneeRole is { } assigneeRole ? ((Role)assigneeRole).ToString() : null,
                assigneePerson = string.IsNullOrWhiteSpace(item.AssigneePersonEmail) ? null : item.AssigneePersonEmail,
                due = item.DueAt,
                daysOverdue,
                isOverdue = daysOverdue > 0,
                isDueToday = daysOverdue == 0,
                createdAt = item.CreatedAt,
                project = ProjectLabel(item.ProjectId, projects),
                projectId = string.IsNullOrWhiteSpace(item.ProjectId) ? null : item.ProjectId,
                intent = new { intent.Verb, expects = intent.Expects?.ToString(), intent.Subject },
                aboutRecord = about is null ? null : RecordShape(about, project: projects.GetValueOrDefault(about.ProjectId)),
                relatedRecords = related.Select(r => new
                {
                    r.Named,
                    matchedWords = r.MatchedWords,
                    record = RecordShape(r.Record, projects.GetValueOrDefault(r.Record.ProjectId)),
                }),
                taggedEmails = mail.Count,
                lastEmailAt = mail.Last,
                lastEmailSubject = mail.Subject,
                lastActivity = activity is null ? null : new
                {
                    kind = ((TodoActivityKind)activity.Kind).ToString(),
                    activity.Summary,
                    by = activity.ActorEmail,
                    at = activity.OccurredAt,
                },
                relatedItems = relatedItems.Select(other => new { other.TodoItemId, reference = other.Reference, other.Title, due = other.DueAt }),
                signals = TodoBrief.Signals(item, today, about, related, mail.Count, mail.Last, activity?.OccurredAt, relatedItems),
                nextStep = TodoBrief.NextStep(item, intent, about, related),
                route = $"/todos/{item.TodoItemId}",
            };
        })
        .OrderByDescending(row => row.daysOverdue ?? int.MinValue)
        .ThenBy(row => row.reference, StringComparer.OrdinalIgnoreCase)
        .ToList();

        var byRole = rows.GroupBy(row => row.assigneeRole ?? "Unassigned")
            .OrderByDescending(group => group.Count())
            .ToDictionary(group => group.Key, group => group.Count());

        return Serialise(new
        {
            ok = true,
            today = today.ToString("yyyy-MM-dd"),
            project = project is null ? null : new { project.ProjectId, project.Reference, project.Name },
            scope = project is null ? "every project plus company-wide items" : $"{project.Reference} {project.Name}",
            role = role?.ToString(),
            summary = new
            {
                open = rows.Count,
                overdue = rows.Count(row => row.isOverdue),
                dueToday = rows.Count(row => row.isDueToday),
                inProgress = rows.Count(row => row.status == "InProgress"),
                unassigned = rows.Count(row => row.assigneeRole is null),
                nothingLinked = rows.Count(row => row.aboutRecord is null && !row.relatedRecords.Any()),
                byRole,
                doneInLast30Days = includeDone ? done.Count : (int?)null,
            },
            items = rows,
            done = includeDone
                ? done.Select(item => new
                {
                    item.TodoItemId,
                    reference = item.Reference,
                    item.Title,
                    owner = TodoActivitySummaries.AssigneeLabel(item.AssigneeRole, item.AssigneePersonEmail),
                    completedAt = item.CompletedAt,
                    project = ProjectLabel(item.ProjectId, projects),
                    route = $"/todos/{item.TodoItemId}",
                })
                : null,
            note = string.Join(" ", new[]
            {
                emailNote,
                "relatedRecords with named=false are INFERRED from the title's words — say so when you "
                + "lean on them. nextStep is the portal's reading of what clears the item; render it, "
                + "don't re-derive it. complete_todo / log_todo_progress act on todoItemId.",
            }.Where(text => !string.IsNullOrWhiteSpace(text))),
        });
    }

    private static string ProjectLabel(string projectId, IReadOnlyDictionary<string, ProjectEntity> projects)
    {
        if (string.IsNullOrWhiteSpace(projectId)) return "company-wide";
        return projects.TryGetValue(projectId, out var project) ? $"{project.Reference} {project.Name}" : projectId;
    }

    private static object RecordShape(LinkableRecord record, ProjectEntity? project) => new
    {
        type = record.Type.ToString(),
        recordId = record.RecordId,
        reference = record.Reference,
        title = record.Title,
        status = record.StatusLabel,
        isActive = record.IsActive,
        summary = record.Summary,
        route = RecordRoute(record, project),
    };

    private static string? RecordRoute(LinkableRecord record, ProjectEntity? project)
    {
        var projectId = project?.ProjectId ?? record.ProjectId;
        return record.Type switch
        {
            RecordType.Variation or RecordType.VariationQuote => $"/projects/{projectId}/variations/{record.RecordId}",
            RecordType.BidPackageInvite => $"/projects/{projectId}/bid-package-invites/{record.RecordId}",
            RecordType.WorkOrder => $"/projects/{projectId}/work-orders",
            RecordType.Request => $"/projects/{projectId}/requests/view/{record.RecordId}",
            RecordType.Defect => $"/projects/{projectId}/defects/{record.RecordId}",
            _ => null,
        };
    }
}

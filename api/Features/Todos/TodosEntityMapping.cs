using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Todos;

internal static class TodosEntityMapping
{
    public static TodoItem ToModel(
        this TodoItemEntity entity,
        IReadOnlyDictionary<string, string>? personNames = null,
        IReadOnlyDictionary<string, string>? aboutReferences = null) =>
        new(entity.TodoItemId,
            entity.ProjectId,
            entity.Reference,
            entity.Title,
            entity.Notes,
            entity.AssigneeRole is int role ? (Role?)role : null,
            entity.AssigneePersonEmail,
            AssigneePersonName: entity.AssigneePersonEmail is string email && personNames is not null
                && personNames.TryGetValue(email, out var name) ? name : null,
            entity.CreatedByEmail,
            entity.IsComplete,
            entity.CreatedAt,
            entity.DueAt,
            entity.CompletedAt,
            entity.StartedAt,
            entity.StartedByEmail,
            AboutRecordType: entity.AboutRecordType is int aboutType ? (RecordType?)aboutType : null,
            AboutRecordId: entity.AboutRecordId,
            AboutRecordReference: entity.AboutRecordId is string aboutId && aboutReferences is not null
                && aboutReferences.TryGetValue(TodoAboutRecords.Key(entity.AboutRecordType, aboutId), out var aboutReference)
                ? aboutReference : null);

    public static TodoActivity ToModel(this TodoItemActivityEntity entity) =>
        new(entity.TodoItemActivityId,
            entity.TodoItemId,
            (TodoActivityKind)entity.Kind,
            entity.Summary,
            entity.ActorEmail,
            entity.OccurredAt);

    // The display names for every person pinned on the given rows, keyed by email
    // (case-insensitive), read from the directory in one query. Handlers pass this into ToModel so
    // a pinned item carries the person's NAME to the UI, not just their address.
    public static async Task<IReadOnlyDictionary<string, string>> PersonNamesForAsync(
        this JpmsContext context, IEnumerable<TodoItemEntity> entities, CancellationToken cancellationToken)
    {
        var emails = entities
            .Select(e => e.AssigneePersonEmail)
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (emails.Count == 0) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var users = await context.DirectoryUsers.AsNoTracking()
            .Where(user => emails.Contains(user.Email))
            .Select(user => new { user.Email, user.DisplayName })
            .ToListAsync(cancellationToken);

        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var user in users) names[user.Email] = user.DisplayName;
        return names;
    }
}

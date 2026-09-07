using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class AddTodoItemHandler : ICommandHandler<AddTodoItem, TodoItem>
{
    private readonly JpmsContext context;
    private readonly TodoActivityRecorder activity;
    private readonly TodoAboutRecords aboutRecords;
    public AddTodoItemHandler(JpmsContext context, TodoActivityRecorder activity, TodoAboutRecords aboutRecords)
    { this.context = context; this.activity = activity; this.aboutRecords = aboutRecords; }

    public async Task<TodoItem> HandleAsync(AddTodoItem command, CancellationToken cancellationToken)
    {
        var projectExists = await context.Projects.AnyAsync(p => p.ProjectId == command.ProjectId, cancellationToken);
        if (!projectExists) throw new InvalidOperationException($"Project '{command.ProjectId}' not found.");

        // A pinned person must currently hold the assigned role in the directory.
        await TodoAssigneeGuard.EnsurePersonHoldsRoleAsync(
            context, command.AssigneeRole, command.AssigneePersonEmail, cancellationToken);

        // The record the item is about (if any) must exist and sit on this project.
        var about = await aboutRecords.VerifyAsync(
            command.AboutRecordType, command.AboutRecordId, command.ProjectId, cancellationToken);

        var nextNumber = (await context.TodoItems.MaxAsync(t => (int?)t.Number, cancellationToken) ?? 0) + 1;

        var entity = new TodoItemEntity
        {
            TodoItemId = TodosIdentifierFactory.Next(),
            ProjectId = command.ProjectId,
            Number = nextNumber,
            Title = Clamp(command.Title.Trim(), 256),
            Notes = Clamp(command.Notes?.Trim() ?? "", 2048),
            AssigneeRole = (int?)command.AssigneeRole,
            AssigneePersonEmail = TodoAssigneeGuard.NormalisePersonEmail(command.AssigneePersonEmail),
            CreatedByEmail = command.CreatedByEmail,
            IsComplete = false,
            CreatedAt = DateTimeOffset.UtcNow,
            DueAt = command.DueAt,
            AboutRecordType = about is null ? null : (int)about.Type,
            AboutRecordId = about?.RecordId
        };

        context.TodoItems.Add(entity);
        activity.Record(entity, TodoActivityKind.Created, TodoActivitySummaries.CreatedSummary(entity), command.CreatedByEmail);
        await context.SaveChangesAsync(cancellationToken);
        var aboutReferences = about is null
            ? null
            : new Dictionary<string, string> { [TodoAboutRecords.Key((int)about.Type, about.RecordId)] = about.Reference };
        return entity.ToModel(await context.PersonNamesForAsync(new[] { entity }, cancellationToken), aboutReferences);
    }

    private static string Clamp(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}

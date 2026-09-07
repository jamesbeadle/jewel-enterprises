using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

// The to-dos about one record — the record page's "To-dos" panel. Same list order as the project
// list (open items in number order, then the done pile newest-first).
public sealed class ListTodoItemsAboutRecordHandler : IQueryHandler<ListTodoItemsAboutRecord, IReadOnlyList<TodoItem>>
{
    private readonly JpmsContext context;
    private readonly TodoAboutRecords aboutRecords;
    public ListTodoItemsAboutRecordHandler(JpmsContext context, TodoAboutRecords aboutRecords) { this.context = context; this.aboutRecords = aboutRecords; }

    public async Task<IReadOnlyList<TodoItem>> HandleAsync(ListTodoItemsAboutRecord query, CancellationToken cancellationToken)
    {
        var type = (int)query.RecordType;
        var entities = await context.TodoItems.AsNoTracking()
            .Where(t => t.AboutRecordType == type && t.AboutRecordId == query.RecordId)
            .ToListAsync(cancellationToken);

        var personNames = await context.PersonNamesForAsync(entities, cancellationToken);
        var aboutReferences = await aboutRecords.ReferencesForAsync(entities, cancellationToken);
        return entities
            .InListOrder()
            .Select(t => t.ToModel(personNames, aboutReferences))
            .ToList()
            .AsReadOnly();
    }
}

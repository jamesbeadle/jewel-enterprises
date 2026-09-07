using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

public sealed class ListTodoItemsForProjectHandler : IQueryHandler<ListTodoItemsForProject, IReadOnlyList<TodoItem>>
{
    private readonly JpmsContext context;
    private readonly TodoAboutRecords aboutRecords;
    public ListTodoItemsForProjectHandler(JpmsContext context, TodoAboutRecords aboutRecords) { this.context = context; this.aboutRecords = aboutRecords; }

    public async Task<IReadOnlyList<TodoItem>> HandleAsync(ListTodoItemsForProject query, CancellationToken cancellationToken)
    {
        // TodosOrdering.InListOrder: open items in number order, then the done pile newest-first.
        var entities = await context.TodoItems.AsNoTracking()
            .Where(t => t.ProjectId == query.ProjectId)
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

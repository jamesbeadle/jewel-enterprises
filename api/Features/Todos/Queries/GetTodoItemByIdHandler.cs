using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

// One to-do item by id, for the item's own page. Null when it doesn't exist — the page says so
// rather than the endpoint erroring, because a stale link is an everyday event, not a fault.
public sealed class GetTodoItemByIdHandler : IQueryHandler<GetTodoItemById, TodoItem?>
{
    private readonly JpmsContext context;
    private readonly TodoAboutRecords aboutRecords;

    public GetTodoItemByIdHandler(JpmsContext context, TodoAboutRecords aboutRecords) { this.context = context; this.aboutRecords = aboutRecords; }

    public async Task<TodoItem?> HandleAsync(GetTodoItemById query, CancellationToken cancellationToken)
    {
        var entity = await context.TodoItems.AsNoTracking()
            .FirstOrDefaultAsync(item => item.TodoItemId == query.TodoItemId, cancellationToken);
        if (entity is null) return null;

        var personNames = await context.PersonNamesForAsync(new[] { entity }, cancellationToken);
        var aboutReferences = await aboutRecords.ReferencesForAsync(new[] { entity }, cancellationToken);
        return entity.ToModel(personNames, aboutReferences);
    }
}

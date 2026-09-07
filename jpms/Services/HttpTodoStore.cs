using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Services;

public sealed class HttpTodoStore : ITodoStore
{
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpTodoStore(IQueryClient queries, ICommandSender commands)
    {
        this.queries = queries;
        this.commands = commands;
    }

    public Task<IReadOnlyList<TodoItem>> ListForProjectAsync(string projectId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListTodoItemsForProject(projectId), cancellationToken);

    public Task<IReadOnlyList<TodoItem>> ListMineAsync(CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListMyTodoItems(), cancellationToken);

    public Task<IReadOnlyList<TodoItem>> ListAllAsync(CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListAllTodoItems(), cancellationToken);

    public Task<IReadOnlyList<Role>> ListAssignableRolesAsync(CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListTodoAssignableRoles(), cancellationToken);

    public Task<IReadOnlyList<TodoAssignablePerson>> ListAssignablePeopleAsync(CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListTodoAssignablePeople(), cancellationToken);

    public Task<IReadOnlyList<MailboxMessage>> ListEmailsAsync(string todoItemId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListTodoEmails(todoItemId), cancellationToken);

    public Task<TodoItem?> GetAsync(string todoItemId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new GetTodoItemById(todoItemId), cancellationToken);

    public Task<IReadOnlyList<TodoItem>> ListLinkedAsync(string todoItemId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListLinkedTodoItems(todoItemId), cancellationToken);

    public Task<IReadOnlyList<TodoItem>> ListAboutRecordAsync(RecordType recordType, string recordId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListTodoItemsAboutRecord(recordType, recordId), cancellationToken);

    public Task<TodoItem> AddAsync(AddTodoItem command, CancellationToken cancellationToken = default) =>
        commands.SendAsync(command, cancellationToken);

    public Task<TodoItem> AddGeneralAsync(AddGeneralTodoItem command, CancellationToken cancellationToken = default) =>
        commands.SendAsync(command, cancellationToken);

    public Task<TodoItem> UpdateAsync(UpdateTodoItem command, CancellationToken cancellationToken = default) =>
        commands.SendAsync(command, cancellationToken);

    public Task<TodoItem> MoveAsync(MoveTodoItem command, CancellationToken cancellationToken = default) =>
        commands.SendAsync(command, cancellationToken);

    public Task<Acknowledgement> DeleteAsync(string todoItemId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new DeleteTodoItem(todoItemId), cancellationToken);

    public Task<IReadOnlyList<TodoActivity>> ListActivityAsync(string todoItemId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListTodoActivity(todoItemId), cancellationToken);

    public Task<TodoItem> LogProgressAsync(LogTodoProgress command, CancellationToken cancellationToken = default) =>
        commands.SendAsync(command, cancellationToken);

    public Task<IReadOnlyList<MailboxMessage>> ListUnfiledRepliesAsync(string todoItemId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListUnfiledReplies(RecordType.Todo, todoItemId), cancellationToken);
}

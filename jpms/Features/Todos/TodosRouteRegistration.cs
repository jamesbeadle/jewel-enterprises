using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Features.Todos;

// Client routes for project to-do items. Mirrors the api endpoints in Features/Todos: list + add are
// project-scoped, update/delete address the item, and create-from-message is the triage-stage path
// that captures several items from one email (tagging the email per item).
public static class TodosRouteRegistration
{
    public static void RegisterTodosRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListTodoItemsForProject, IReadOnlyList<TodoItem>>(
            new QueryRoute("/api/projects/{projectId}/todos",
                query => $"/api/projects/{((ListTodoItemsForProject)query).ProjectId}/todos"));

        // The to-dos ABOUT one record — a record page's To-dos panel (a defect's first).
        queries.Register<ListTodoItemsAboutRecord, IReadOnlyList<TodoItem>>(
            new QueryRoute("/api/records/{recordType}/{recordId}/todos",
                query => $"/api/records/{((ListTodoItemsAboutRecord)query).RecordType}/{((ListTodoItemsAboutRecord)query).RecordId}/todos"));

        queries.Register<ListTodoAssignableRoles, IReadOnlyList<Role>>(
            QueryRoute.Static("/api/todo-assignable-roles"));

        // The person half of the assignee pickers: directory holders of the assignable roles, one
        // row per (role, holder) pair, for the optional pin-to-a-person on an assignment.
        queries.Register<ListTodoAssignablePeople, IReadOnlyList<TodoAssignablePerson>>(
            QueryRoute.Static("/api/todo-assignable-people"));

        // The signed-in user's own items (their roles stamped server-side) — dashboard panel + the
        // browser for non-admin roles.
        queries.Register<ListMyTodoItems, IReadOnlyList<TodoItem>>(
            QueryRoute.Static("/api/my/todos"));

        // Every item in the system — the MD's / administrators' To-dos browser read.
        queries.Register<ListAllTodoItems, IReadOnlyList<TodoItem>>(
            QueryRoute.Static("/api/todos"));

        // A to-do item's linked emails ("JPMS/TODO-####"-tagged mail, read live) — the item's page.
        queries.Register<ListTodoEmails, IReadOnlyList<MailboxMessage>>(
            new QueryRoute("/api/todo-items/{todoItemId}/emails",
                query => $"/api/todo-items/{((ListTodoEmails)query).TodoItemId}/emails"));

        // The item's page (/todos/{id}): the item itself, and the to-dos linked to it — the items
        // sharing tagged mail with it.
        queries.Register<GetTodoItemById, TodoItem?>(
            new QueryRoute("/api/todo-items/{todoItemId}",
                query => $"/api/todo-items/{((GetTodoItemById)query).TodoItemId}"));

        // The item's timeline (created / started / chased / … / email sent), newest first.
        queries.Register<ListTodoActivity, IReadOnlyList<TodoActivity>>(
            new QueryRoute("/api/todo-items/{todoItemId}/activity",
                query => $"/api/todo-items/{((ListTodoActivity)query).TodoItemId}/activity"));

        queries.Register<ListLinkedTodoItems, IReadOnlyList<TodoItem>>(
            new QueryRoute("/api/todo-items/{todoItemId}/linked-todos",
                query => $"/api/todo-items/{((ListLinkedTodoItems)query).TodoItemId}/linked-todos"));

        commands.Register<AddTodoItem, TodoItem>(
            new CommandRoute("POST", "/api/projects/{projectId}/todos",
                command => $"/api/projects/{((AddTodoItem)command).ProjectId}/todos"));

        // General (company-wide, no-project) items added directly from the /todos browser page.
        commands.Register<AddGeneralTodoItem, TodoItem>(
            new CommandRoute("POST", "/api/todos", _ => "/api/todos"));

        commands.Register<UpdateTodoItem, TodoItem>(
            new CommandRoute("PUT", "/api/todo-items/{todoItemId}",
                command => $"/api/todo-items/{((UpdateTodoItem)command).TodoItemId}"));

        // Progress logged by hand from the item's page: Working on it, a chase, or a note.
        commands.Register<LogTodoProgress, TodoItem>(
            new CommandRoute("POST", "/api/todo-items/{todoItemId}/progress",
                command => $"/api/todo-items/{((LogTodoProgress)command).TodoItemId}/progress"));

        // Re-file an item under a different project (blank ProjectId = company-wide, MD/admin only).
        commands.Register<MoveTodoItem, TodoItem>(
            new CommandRoute("POST", "/api/todo-items/{todoItemId}/move",
                command => $"/api/todo-items/{((MoveTodoItem)command).TodoItemId}/move"));

        commands.Register<DeleteTodoItem, Acknowledgement>(
            new CommandRoute("DELETE", "/api/todo-items/{todoItemId}",
                command => $"/api/todo-items/{((DeleteTodoItem)command).TodoItemId}"));

        commands.Register<CreateTodoItemsFromMessage, IReadOnlyList<TodoItem>>(
            new CommandRoute("POST", "/api/mailbox/message/create-todos", _ => "/api/mailbox/message/create-todos"));
    }
}

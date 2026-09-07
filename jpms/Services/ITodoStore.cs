using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Services;

// To-do items: listed and managed on the project's To-do tab, the /todos browser page, and the
// "My to-dos" dashboard panel. Items created from an email at the triage stage arrive through
// IIntakeQueue.CreateTodoItemsFromMessageAsync and show up here. General (company-wide) items
// carry a blank ProjectId. Items are assigned to a ROLE — whoever holds the role sees them, so
// assignments survive staff changes — optionally pinned to a named holder of that role, which
// narrows the item to that one person's list (and falls back to the role if they move on).
public interface ITodoStore
{
    Task<IReadOnlyList<TodoItem>> ListForProjectAsync(string projectId, CancellationToken cancellationToken = default);
    /// <summary>Every item assigned to any role the signed-in user holds — general and project
    /// items alike. Backs the "My to-dos" dashboard panel and the browser for non-admin roles.</summary>
    Task<IReadOnlyList<TodoItem>> ListMineAsync(CancellationToken cancellationToken = default);
    /// <summary>Every item in the system — the MD's / administrators' browser read (403 for
    /// anyone else; see TodoRoles.AllowedToSeeAllTodos in the api).</summary>
    Task<IReadOnlyList<TodoItem>> ListAllAsync(CancellationToken cancellationToken = default);
    /// <summary>The roles a to-do can be assigned to — feeds the assignee role pickers.</summary>
    Task<IReadOnlyList<Role>> ListAssignableRolesAsync(CancellationToken cancellationToken = default);
    /// <summary>The people a to-do can be pinned to — directory holders of the assignable roles,
    /// one row per (role, holder) pair. Feeds the same pickers, under each role.</summary>
    Task<IReadOnlyList<TodoAssignablePerson>> ListAssignablePeopleAsync(CancellationToken cancellationToken = default);
    /// <summary>The emails currently tagged to one item ("JPMS/TODO-####"), read live from the
    /// mailbox — the linked-communications list on the item's page.</summary>
    Task<IReadOnlyList<MailboxMessage>> ListEmailsAsync(string todoItemId, CancellationToken cancellationToken = default);
    /// <summary>One item by id — the read behind the item's own page. Null when the item doesn't
    /// exist (deleted, or a stale link).</summary>
    Task<TodoItem?> GetAsync(string todoItemId, CancellationToken cancellationToken = default);
    /// <summary>The items linked to one item — every other item sharing a tagged email with it,
    /// derived live from the same mail tags as <see cref="ListEmailsAsync"/>.</summary>
    Task<IReadOnlyList<TodoItem>> ListLinkedAsync(string todoItemId, CancellationToken cancellationToken = default);
    /// <summary>The to-dos ABOUT one record (TodoItem.AboutRecordType/AboutRecordId) — a record
    /// page's To-dos panel.</summary>
    Task<IReadOnlyList<TodoItem>> ListAboutRecordAsync(RecordType recordType, string recordId, CancellationToken cancellationToken = default);
    Task<TodoItem> AddAsync(AddTodoItem command, CancellationToken cancellationToken = default);
    /// <summary>Add a general (company-wide, no-project) item from the /todos browser page.</summary>
    Task<TodoItem> AddGeneralAsync(AddGeneralTodoItem command, CancellationToken cancellationToken = default);
    Task<TodoItem> UpdateAsync(UpdateTodoItem command, CancellationToken cancellationToken = default);
    /// <summary>Re-file an item under a different project — or company-wide (blank ProjectId),
    /// which is the MD's / administrators' destination only. Everything else about the item,
    /// linked emails included, moves with it.</summary>
    Task<TodoItem> MoveAsync(MoveTodoItem command, CancellationToken cancellationToken = default);
    Task<Acknowledgement> DeleteAsync(string todoItemId, CancellationToken cancellationToken = default);
    /// <summary>The item's timeline, newest first — every change, logged chase and email sent
    /// from its page.</summary>
    Task<IReadOnlyList<TodoActivity>> ListActivityAsync(string todoItemId, CancellationToken cancellationToken = default);
    /// <summary>Log progress by hand: Working on it, a chase with a note, or a plain note.
    /// Started and Chased move an Open item to In progress; nothing here completes it.</summary>
    Task<TodoItem> LogProgressAsync(LogTodoProgress command, CancellationToken cancellationToken = default);
    /// <summary>Replies that arrived on the item's threads after its last filed email and are not
    /// tagged to it yet — what the communications list is blind to until someone files them.</summary>
    Task<IReadOnlyList<MailboxMessage>> ListUnfiledRepliesAsync(string todoItemId, CancellationToken cancellationToken = default);
}

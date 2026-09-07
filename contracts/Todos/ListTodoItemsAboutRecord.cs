using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// The to-dos ABOUT one record (TodoItem.AboutRecordType/AboutRecordId) — the record page's
// "To-dos" panel (a defect's first). Canonical list order: open items in number order, then the
// done pile newest-first. Internal-only read, like the project list.
public sealed record ListTodoItemsAboutRecord(RecordType RecordType, string RecordId) : IQuery<IReadOnlyList<TodoItem>>;

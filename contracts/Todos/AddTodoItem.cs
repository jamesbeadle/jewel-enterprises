using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// Add a single to-do item to a project from the Overview tab. Assignment is to a ROLE (null =
// unassigned), optionally pinned to a named person who holds it — see TodoItem. CreatedByEmail is
// stamped from the signed-in user server-side — never trusted from the client body.
// AboutRecordType/AboutRecordId (both or neither) make the item ABOUT a record on the same
// project — raised from that record's page (a defect's "New to-do"), or by the connector's
// add_todo with aboutRecordType/aboutRecordId. The server checks the record exists and belongs
// to the project.
public sealed record AddTodoItem(
    string ProjectId,
    string Title,
    string? Notes = null,
    Role? AssigneeRole = null,
    string? AssigneePersonEmail = null,
    DateTimeOffset? DueAt = null,
    string CreatedByEmail = "",
    RecordType? AboutRecordType = null,
    string? AboutRecordId = null) : ICommand<TodoItem>;

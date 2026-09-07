using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class AddTodoItemValidation
{
    public ValidationOutcome Check(AddTodoItem command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("Title is required.");
        if (command.AssigneeRole is Role role && !TodoRoles.AssignableAsTodoAssignee.Includes(role))
            errors.Add("To-do items can't be assigned to that role.");
        // A person is only ever pinned WITH a role (see TodoAssignee) — that the person actually
        // holds the role is the handler's directory check; this is the shape rule.
        if (!string.IsNullOrWhiteSpace(command.AssigneePersonEmail) && command.AssigneeRole is null)
            errors.Add("A to-do can only be pinned to a person together with their role.");
        // About-a-record is both halves or neither; that the record exists on the project is the
        // handler's check (TodoAboutRecords).
        if ((command.AboutRecordType is null) != string.IsNullOrWhiteSpace(command.AboutRecordId))
            errors.Add("A to-do about a record needs both the record type and the record id.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}

using Jewel.JPMS.Api.Features.Todos.Commands;
using Jewel.JPMS.Api.Features.Todos.Queries;
using Jewel.JPMS.Contracts.Todos;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Todos;

public static class TodosFeatureRegistration
{
    public static IServiceCollection AddTodosFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListTodoItemsForProject, IReadOnlyList<TodoItem>>, ListTodoItemsForProjectHandler>();
        // The record page's "to-dos about this record" read (a defect's first), and the
        // record-type-agnostic verify/resolve helper the handlers share.
        services.AddScoped<IQueryHandler<ListTodoItemsAboutRecord, IReadOnlyList<TodoItem>>, ListTodoItemsAboutRecordHandler>();
        services.AddScoped<TodoAboutRecords>();
        services.AddScoped<IQueryHandler<ListTodoAssignableRoles, IReadOnlyList<Role>>, ListTodoAssignableRolesHandler>();

        // The person half of the assignee pickers: directory holders of the assignable roles, one
        // row per (role, holder) pair, for the optional pin-to-a-person on an assignment.
        services.AddScoped<IQueryHandler<ListTodoAssignablePeople, IReadOnlyList<TodoAssignablePerson>>, ListTodoAssignablePeopleHandler>();

        // The To-dos browser + "My to-dos" dashboard panel: the MD / administrators read everything,
        // everyone else reads the items assigned to them.
        services.AddScoped<IQueryHandler<ListMyTodoItems, IReadOnlyList<TodoItem>>, ListMyTodoItemsHandler>();
        services.AddScoped<IQueryHandler<ListAllTodoItems, IReadOnlyList<TodoItem>>, ListAllTodoItemsHandler>();

        // A to-do item's linked emails ("JPMS/TODO-####"-tagged mail, read live by tag via the
        // record-link layer) — the linked-mail list on the item's page.
        services.AddScoped<IQueryHandler<ListTodoEmails, IReadOnlyList<MailboxMessage>>, ListTodoEmailsHandler>();

        // The item's own page (/todos/{id}): the item itself. Its linked to-dos read is registered
        // with the rest of the linked-to-dos block below.
        services.AddScoped<IQueryHandler<GetTodoItemById, TodoItem?>, GetTodoItemByIdHandler>();

        services.AddScoped<ICommandHandler<AddTodoItem, TodoItem>, AddTodoItemHandler>();
        services.AddScoped<AddTodoItemAuthorisation>();
        services.AddScoped<AddTodoItemValidation>();

        // General (company-wide, no-project) items added directly from the To-dos browser page.
        services.AddScoped<ICommandHandler<AddGeneralTodoItem, TodoItem>, AddGeneralTodoItemHandler>();
        services.AddScoped<AddGeneralTodoItemAuthorisation>();
        services.AddScoped<AddGeneralTodoItemValidation>();

        // The timeline: every command above and below writes its line through the recorder (one
        // save with the change); the page reads it back newest-first; progress logged by hand
        // (Working on it / a chase / a note) is its own command. The email recorder is the
        // mailbox compose handler's bridge — an email sent from the item's page is a line too.
        services.AddScoped<TodoActivityRecorder>();
        services.AddScoped<TodoEmailActivityRecorder>();
        services.AddScoped<TodoCompletionRecordTagger>();
        services.AddScoped<IQueryHandler<ListTodoActivity, IReadOnlyList<TodoActivity>>, ListTodoActivityHandler>();
        services.AddScoped<ICommandHandler<LogTodoProgress, TodoItem>, LogTodoProgressHandler>();
        services.AddScoped<LogTodoProgressAuthorisation>();
        services.AddScoped<LogTodoProgressValidation>();

        services.AddScoped<ICommandHandler<UpdateTodoItem, TodoItem>, UpdateTodoItemHandler>();
        services.AddScoped<UpdateTodoItemAuthorisation>();
        services.AddScoped<UpdateTodoItemValidation>();

        // Re-file an item under a different project — or company-wide, which is the MD's /
        // administrators' destination only (see MoveTodoItemAuthorisation).
        services.AddScoped<ICommandHandler<MoveTodoItem, TodoItem>, MoveTodoItemHandler>();
        services.AddScoped<MoveTodoItemAuthorisation>();
        services.AddScoped<MoveTodoItemValidation>();

        // Linked to-dos: the flat two-way "these belong together" association — the detail
        // modal's linked list (+ add/remove there), and the link-picker pool for the Control
        // Centre's create pane and the modal alike.
        services.AddScoped<IQueryHandler<ListLinkedTodoItems, IReadOnlyList<TodoItem>>, ListLinkedTodoItemsHandler>();
        services.AddScoped<IQueryHandler<ListTodoLinkCandidates, IReadOnlyList<TodoItem>>, ListTodoLinkCandidatesHandler>();
        services.AddScoped<ICommandHandler<LinkTodoItems, Acknowledgement>, LinkTodoItemsHandler>();
        services.AddScoped<LinkTodoItemsAuthorisation>();
        services.AddScoped<LinkTodoItemsValidation>();
        services.AddScoped<ICommandHandler<UnlinkTodoItems, Acknowledgement>, UnlinkTodoItemsHandler>();
        services.AddScoped<UnlinkTodoItemsAuthorisation>();
        services.AddScoped<UnlinkTodoItemsValidation>();

        services.AddScoped<ICommandHandler<DeleteTodoItem, Acknowledgement>, DeleteTodoItemHandler>();
        services.AddScoped<DeleteTodoItemAuthorisation>();
        services.AddScoped<DeleteTodoItemValidation>();

        // Triage: create one or more to-dos from a mailbox message, tagging the email per item.
        services.AddScoped<ICommandHandler<CreateTodoItemsFromMessage, IReadOnlyList<TodoItem>>, CreateTodoItemsFromMessageHandler>();
        services.AddScoped<CreateTodoItemsFromMessageAuthorisation>();
        services.AddScoped<CreateTodoItemsFromMessageValidation>();

        return services;
    }
}

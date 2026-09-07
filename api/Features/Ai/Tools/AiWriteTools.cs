using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Requests.Commands;
using Jewel.JPMS.Api.Features.Todos;
using Jewel.JPMS.Api.Features.Todos.Commands;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.Todos;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The connector's write tools — the deliberately small day-one set (2026-08-27): a note on a
/// request's conversation, to-dos, and the skill store. Each one composes the SAME pieces its
/// HTTP endpoint does — Authorisation.Allows, Validation.Check, then the command handler — with
/// the actor stamped server-side from the authenticated user, so a write from Claude is
/// indistinguishable in the record from a write made by clicking. Financial actions (approvals,
/// invoicing, releasing orders), deletion and email sending are deliberately absent — those stay
/// in the portal until the connector has earned them.
/// </summary>
internal static class AiWriteTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });
    private static string Refused() => Serialise(new { ok = false, error = "Your portal roles do not allow this action." });
    private static string Invalid(ValidationOutcome outcome) => Serialise(new { ok = false, errors = outcome.Errors });

    public static IReadOnlyList<AiTool> Build()
    {
        return new List<AiTool>
        {
            new(
                "post_request_message",
                "WRITE: add a message to a request's own conversation thread in the portal — a note the "
                + "whole team sees on the request page, recorded under the signed-in user's name. Use it "
                + "for summaries, decisions and follow-ups the user asks you to put on the record. It "
                + "posts immediately — confirm the wording with the user before calling. Internal by "
                + "default; visibility \"shared\" makes it visible to the request's external participants "
                + "(architect, client, subcontractor), so pass that only when the user says so. This "
                + "never sends an email.",
                AiToolSchema.Object(
                    ("requestId", "string", "The request's id — find_by_reference or list_requests resolves a reference.", true),
                    ("body", "string", "The message exactly as it should appear on the thread.", true),
                    ("visibility", "string", "\"internal\" (default) or \"shared\".", false)),
                AiToolKind.Write,
                JpmsRoleSets.InternalAndArchitect,
                async (context, input, ct) =>
                {
                    var visibilityText = (AiToolSchema.Text(input, "visibility") ?? "internal").Trim().ToLowerInvariant();
                    if (visibilityText is not ("internal" or "shared"))
                        return Fail("visibility must be \"internal\" or \"shared\".");

                    var command = new PostRequestMessage(
                        AiToolSchema.Text(input, "requestId") ?? "",
                        AiToolSchema.Text(input, "body") ?? "",
                        visibilityText == "shared" ? MessageVisibility.Shared : MessageVisibility.Internal,
                        AuthorEmail: context.User.Email,
                        AuthorName: context.User.DisplayName);

                    var authorisation = context.Services.GetRequiredService<PostRequestMessageAuthorisation>();
                    if (!authorisation.Allows(context.User, command)) return Refused();
                    var validation = context.Services.GetRequiredService<PostRequestMessageValidation>().Check(command);
                    if (validation.HasFailed) return Invalid(validation);

                    var handler = context.Services
                        .GetRequiredService<ICommandHandler<PostRequestMessage, RequestMessage>>();
                    var posted = await handler.HandleAsync(command, ct);

                    await WriteAudit(context, AuditEventType.NotePosted,
                        $"Posted a message on a request via the AI connector.",
                        recordType: RecordType.Request, recordId: command.RequestId, ct: ct);

                    return Serialise(new { ok = true, posted.MessageId, posted.PostedAt,
                        note = "The message is on the request's conversation now — tell the user it is posted, quoting nothing back." });
                }),

            new(
                "add_todo",
                "WRITE: add a to-do item — on a project (pass projectId) or company-wide (leave it out). "
                + "It is created immediately, exactly as the user would from the To-dos page, recorded "
                + "under the signed-in user's name. Assignment is to a ROLE (ManagingDirector, "
                + "FinanceDirector, ProjectManager, QuantitySurveyor, SiteManager, Accounts…), "
                + "optionally pinned to one named person holding it by their portal email.",
                AiToolSchema.Object(
                    ("title", "string", "What is to be done, as the to-do list will show it.", true),
                    ("projectId", "string", "The project it belongs to (list_projects resolves a name). Omit for company-wide.", false),
                    ("notes", "string", "Optional detail — say which record or email it concerns.", false),
                    ("assigneeRole", "string", "The role it is assigned to, exactly as the portal names it — e.g. \"ProjectManager\", \"QuantitySurveyor\". Omit for unassigned.", false),
                    ("assigneeEmail", "string", "Pin to one holder of that role — their portal email. Only with assigneeRole.", false),
                    ("due", "string", "Due date, yyyy-MM-dd. Omit for none.", false),
                    ("aboutRecordType", "string", "Make the item ABOUT a record on that project — the record type as the portal names it (\"Defect\"). Project items only; give aboutRecordId too.", false),
                    ("aboutRecordId", "string", "The record's id (list_defects / find_by_reference give it). Only with aboutRecordType.", false)),
                AiToolKind.Write,
                TodoRoles.AllowedToManageTodos,
                async (context, input, ct) =>
                {
                    RecordType? aboutType = null;
                    var aboutTypeText = AiToolSchema.Text(input, "aboutRecordType");
                    if (!string.IsNullOrWhiteSpace(aboutTypeText))
                    {
                        if (!Enum.TryParse<RecordType>(aboutTypeText, ignoreCase: true, out var parsedAbout))
                            return Fail($"\"{aboutTypeText}\" is not a record type. Types: "
                                        + string.Join(", ", Enum.GetNames<RecordType>()) + ".");
                        aboutType = parsedAbout;
                    }
                    var aboutId = AiToolSchema.Text(input, "aboutRecordId");

                    Role? assigneeRole = null;
                    var roleText = AiToolSchema.Text(input, "assigneeRole");
                    if (!string.IsNullOrWhiteSpace(roleText))
                    {
                        if (!Enum.TryParse<Role>(roleText, ignoreCase: true, out var parsed))
                            return Fail($"\"{roleText}\" is not a portal role. Roles: "
                                        + string.Join(", ", Enum.GetNames<Role>()) + ".");
                        assigneeRole = parsed;
                    }

                    DateTimeOffset? due = null;
                    var dueText = AiToolSchema.Text(input, "due");
                    if (!string.IsNullOrWhiteSpace(dueText))
                    {
                        if (!DateTimeOffset.TryParse(dueText, out var parsedDue))
                            return Fail("due must be a date, yyyy-MM-dd.");
                        due = parsedDue;
                    }

                    var title = AiToolSchema.Text(input, "title") ?? "";
                    var notes = AiToolSchema.Text(input, "notes");
                    var assigneeEmail = AiToolSchema.Text(input, "assigneeEmail");
                    var projectId = AiToolSchema.Text(input, "projectId");

                    if ((aboutType is not null || !string.IsNullOrWhiteSpace(aboutId)) && string.IsNullOrWhiteSpace(projectId))
                        return Fail("A to-do about a record is a project item — pass the record's projectId.");

                    TodoItem created;
                    if (string.IsNullOrWhiteSpace(projectId))
                    {
                        var command = new AddGeneralTodoItem(title, notes, assigneeRole, assigneeEmail, due,
                            CreatedByEmail: context.User.Email);
                        var authorisation = context.Services.GetRequiredService<AddGeneralTodoItemAuthorisation>();
                        if (!authorisation.Allows(context.User, command)) return Refused();
                        var validation = context.Services.GetRequiredService<AddGeneralTodoItemValidation>().Check(command);
                        if (validation.HasFailed) return Invalid(validation);
                        created = await context.Services
                            .GetRequiredService<ICommandHandler<AddGeneralTodoItem, TodoItem>>()
                            .HandleAsync(command, ct);
                    }
                    else
                    {
                        var command = new AddTodoItem(projectId!, title, notes, assigneeRole, assigneeEmail, due,
                            CreatedByEmail: context.User.Email,
                            AboutRecordType: aboutType, AboutRecordId: aboutId);
                        var authorisation = context.Services.GetRequiredService<AddTodoItemAuthorisation>();
                        if (!authorisation.Allows(context.User, command)) return Refused();
                        var validation = context.Services.GetRequiredService<AddTodoItemValidation>().Check(command);
                        if (validation.HasFailed) return Invalid(validation);
                        created = await context.Services
                            .GetRequiredService<ICommandHandler<AddTodoItem, TodoItem>>()
                            .HandleAsync(command, ct);
                    }

                    await WriteAudit(context, AuditEventType.TodoCreated,
                        $"Added the to-do \"{title}\" via the AI connector.",
                        projectId: projectId, recordType: RecordType.Todo, recordId: created.TodoItemId, ct: ct);

                    return Serialise(new { ok = true, created.TodoItemId, created.Reference, created.Title, created.DueAt, aboutRecord = created.AboutRecordReference });
                }),

            new(
                "complete_todo",
                "WRITE: mark a to-do item done (or reopen it with complete false) — the same act as "
                + "ticking it off on the To-dos page, immediately. Completing also files the item's "
                + "tagged email(s) to every record on its project that the item or the email names "
                + "(\"Variation V31\", \"VO32\", \"WO-0045\"), so the record shows its source email "
                + "— no separate file_email_to_record needed. Everything else on the item stays as "
                + "it stands. list_todos and find_by_reference give the todoItemId.",
                AiToolSchema.Object(
                    ("todoItemId", "string", "The item's id, from list_todos or find_by_reference.", true),
                    ("complete", "boolean", "true (default) marks it done; false reopens it.", false)),
                AiToolKind.Write,
                // The manage gate plus current assignees — the assignee widening is checked inside,
                // against the stored row, exactly as the endpoint does.
                JpmsRoleSets.AllInternal,
                async (context, input, ct) =>
                {
                    var todoItemId = AiToolSchema.Text(input, "todoItemId") ?? "";
                    var row = await context.Db.TodoItems.AsNoTracking()
                        .FirstOrDefaultAsync(candidate => candidate.TodoItemId == todoItemId, ct);
                    if (row is null) return Fail($"No to-do item with id \"{todoItemId}\".");

                    var complete = AiToolSchema.Flag(input, "complete") ?? true;
                    var command = new UpdateTodoItem(
                        row.TodoItemId, row.Title,
                        string.IsNullOrWhiteSpace(row.Notes) ? null : row.Notes,
                        row.AssigneeRole is int role ? (Role)role : null,
                        row.AssigneePersonEmail, row.DueAt, complete);

                    var authorisation = context.Services.GetRequiredService<UpdateTodoItemAuthorisation>();
                    if (!authorisation.Allows(context.User, command)
                        && !await authorisation.AllowsAsAssigneeAsync(context.User, command, ct))
                        return Refused();
                    var validation = context.Services.GetRequiredService<UpdateTodoItemValidation>().Check(command);
                    if (validation.HasFailed) return Invalid(validation);

                    var updated = await context.Services
                        .GetRequiredService<ICommandHandler<UpdateTodoItem, TodoItem>>()
                        .HandleAsync(command, ct);

                    await WriteAudit(context, AuditEventType.TodoCompleted,
                        $"{(complete ? "Completed" : "Reopened")} the to-do \"{row.Title}\" via the AI connector.",
                        projectId: row.ProjectId, recordType: RecordType.Todo, recordId: row.TodoItemId, ct: ct);

                    return Serialise(new { ok = true, updated.TodoItemId, updated.IsComplete });
                }),

            new(
                "log_todo_progress",
                "WRITE: log progress on a to-do's timeline — \"started\" (working on it), \"chased\" "
                + "(an email, call or visit, with a note saying who), or a plain \"note\". Started and "
                + "chased move an open item to In progress; nothing here completes it. Immediate, under "
                + "the signed-in user's name.",
                AiToolSchema.Object(
                    ("todoItemId", "string", "The item's id, from list_todos or find_by_reference.", true),
                    ("kind", "string", "\"started\", \"chased\" or \"note\".", true),
                    ("note", "string", "The line as the timeline should read it. Required for chased and note.", false)),
                AiToolKind.Write,
                JpmsRoleSets.AllInternal,
                async (context, input, ct) =>
                {
                    var kindText = (AiToolSchema.Text(input, "kind") ?? "").Trim().ToLowerInvariant();
                    TodoActivityKind kind = kindText switch
                    {
                        "started" => TodoActivityKind.Started,
                        "chased" => TodoActivityKind.Chased,
                        "note" => TodoActivityKind.Note,
                        _ => (TodoActivityKind)(-1)
                    };
                    if ((int)kind == -1) return Fail("kind must be \"started\", \"chased\" or \"note\".");

                    var command = new LogTodoProgress(
                        AiToolSchema.Text(input, "todoItemId") ?? "",
                        kind,
                        AiToolSchema.Text(input, "note"),
                        ActorEmail: context.User.Email);

                    var authorisation = context.Services.GetRequiredService<LogTodoProgressAuthorisation>();
                    if (!authorisation.Allows(context.User, command)
                        && !await authorisation.AllowsAsAssigneeAsync(context.User, command, ct))
                        return Refused();
                    var validation = context.Services.GetRequiredService<LogTodoProgressValidation>().Check(command);
                    if (validation.HasFailed) return Invalid(validation);

                    var updated = await context.Services
                        .GetRequiredService<ICommandHandler<LogTodoProgress, TodoItem>>()
                        .HandleAsync(command, ct);

                    return Serialise(new { ok = true, updated.TodoItemId, status = updated.IsComplete ? "complete" : "open" });
                }),

            new(
                "save_skill",
                "WRITE: create or update one of the portal's stored skills — the working knowledge the "
                + "team teaches the AI (doctrine, house style, fact patterns), edited exactly as on the "
                + "AI Skills page. Saving replaces the skill's body and details in one write (revisions "
                + "are kept server-side). Read the current skill first (load_skill) and carry forward "
                + "what should not change. Restricted to the same people the AI Skills page admits.",
                AiToolSchema.Object(
                    ("skillKey", "string", "The skill's key — an existing one updates it, a new lowercase-hyphen key creates it.", true),
                    ("agentKey", "string", "The discipline group it belongs to, or \"shared\" for every discipline.", true),
                    ("displayName", "string", "The name the Skills page shows.", true),
                    ("description", "string", "One sentence on when the skill applies.", true),
                    ("body", "string", "The skill's full text, markdown.", true),
                    ("pinned", "boolean", "true keeps it always in force for its discipline; false loads on demand. Default false.", false),
                    ("active", "boolean", "false retires it without deleting. Default true.", false)),
                AiToolKind.Write,
                Skills.SkillRoles.ManageSkills,
                async (context, input, ct) =>
                {
                    var command = new SaveAiSkill(
                        AiToolSchema.Text(input, "skillKey") ?? "",
                        AiToolSchema.Text(input, "agentKey") ?? "",
                        AiToolSchema.Text(input, "displayName") ?? "",
                        AiToolSchema.Text(input, "description") ?? "",
                        AiToolSchema.Text(input, "body") ?? "",
                        AiToolSchema.Flag(input, "pinned") ?? false,
                        AiToolSchema.Flag(input, "active") ?? true,
                        SavedByEmail: context.User.Email);

                    var authorisation = context.Services.GetRequiredService<Skills.SaveAiSkillAuthorisation>();
                    if (!authorisation.Allows(context.User, command)) return Refused();
                    var validation = context.Services.GetRequiredService<Skills.SaveAiSkillValidation>().Check(command);
                    if (validation.HasFailed) return Invalid(validation);

                    await context.Services
                        .GetRequiredService<ICommandHandler<SaveAiSkill, Acknowledgement>>()
                        .HandleAsync(command, ct);

                    return Serialise(new { ok = true, command.SkillKey,
                        note = "Saved. The skill is live for the whole team from the next conversation." });
                }),

            new(
                "save_skill_reference",
                "WRITE: create or update one REFERENCE DOCUMENT under a stored skill — the larger "
                + "source material (clause maps, methodologies, precedent libraries) a skill names "
                + "but does not inline. Saving replaces the reference whole. Load the current one "
                + "first (load_skill_reference) and carry forward what should not change. Same "
                + "audience as save_skill.",
                AiToolSchema.Object(
                    ("skillKey", "string", "The owning skill's key — it must already exist.", true),
                    ("refKey", "string", "The reference's key — an existing one updates it, a new lowercase-hyphen key creates it.", true),
                    ("displayName", "string", "The name shown on the Skills page.", true),
                    ("description", "string", "One or two clauses on when this reference is worth loading.", true),
                    ("body", "string", "The reference's full text, markdown.", true)),
                AiToolKind.Write,
                Skills.SkillRoles.ManageSkills,
                async (context, input, ct) =>
                {
                    var command = new SaveAiSkillReference(
                        AiToolSchema.Text(input, "skillKey") ?? "",
                        AiToolSchema.Text(input, "refKey") ?? "",
                        AiToolSchema.Text(input, "displayName") ?? "",
                        AiToolSchema.Text(input, "description") ?? "",
                        AiToolSchema.Text(input, "body") ?? "",
                        SavedByEmail: context.User.Email);

                    var authorisation = context.Services.GetRequiredService<Skills.SaveAiSkillReferenceAuthorisation>();
                    if (!authorisation.Allows(context.User, command)) return Refused();
                    var validation = context.Services.GetRequiredService<Skills.SaveAiSkillReferenceValidation>().Check(command);
                    if (validation.HasFailed) return Invalid(validation);

                    await context.Services
                        .GetRequiredService<ICommandHandler<SaveAiSkillReference, Acknowledgement>>()
                        .HandleAsync(command, ct);

                    return Serialise(new { ok = true, command.SkillKey, command.RefKey,
                        note = "Saved. load_skill lists it; load_skill_reference returns it." });
                }),
        };
    }

    /// <summary>Best-effort audit row, written AFTER the command has succeeded — the same contract
    /// as the endpoints' own audit writes. The AuditActor was set by the MCP endpoint, so the row
    /// carries the token's user even if actorEmail defaulting ever changes.</summary>
    private static async Task WriteAudit(
        AiToolContext context, AuditEventType eventType, string detail,
        string? projectId = null, RecordType? recordType = null, string? recordId = null,
        CancellationToken ct = default)
    {
        var audit = context.Services.GetService<AuditTrail>();
        if (audit is null) return;
        await audit.WriteAsync(eventType, detail,
            projectId: projectId, recordType: recordType, recordId: recordId,
            actorEmail: context.User.Email, cancellationToken: ct);
    }
}

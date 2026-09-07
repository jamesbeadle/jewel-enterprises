namespace Jewel.JPMS.Models;

// A to-do item. Created directly on a project's To-do tab, from an email at the triage stage
// (several at once), or as a GENERAL company-wide item that belongs to no project (ProjectId is ""
// then) — captured from a company-wide email at triage or added on the To-dos browser page. Each
// item owns a sequential "TODO-0001" reference which is also its mailbox tag stem, so an email
// tagged "JPMS/TODO-0001" is the item's linked mail — the same live-read link mechanism the
// Request / Bid Package families use.
//
// Items are assigned to a ROLE first (null = unassigned), and OPTIONALLY pinned to a named person
// who holds that role. The role is what makes assignments survive staff changes: everyone holding
// the role sees an unpinned item and may tick it off, and when someone leaves, a new starter
// taking over the role simply inherits the open items. A pin narrows the item to one person's list
// without giving up that safety — a person can only be pinned WITH their role, and if they leave
// the directory (or lose the role) the pin is cleared and the item falls back to the role.
public sealed record TodoItem(
    string TodoItemId,
    string ProjectId,        // "" = general (company-wide) item with no project
    string Reference,        // sequential human reference, e.g. "TODO-0001" (also the tag stem)
    string Title,
    string Notes,
    Role? AssigneeRole,      // null = unassigned; otherwise a TodoRoles.AssignableAsTodoAssignee role
    string? AssigneePersonEmail, // optional pin to one holder of AssigneeRole; null = the whole role
    string? AssigneePersonName,  // the pinned person's directory display name, resolved server-side
    string CreatedByEmail,
    bool IsComplete,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DueAt,
    DateTimeOffset? CompletedAt,
    // In progress: set by "Working on it", by logging a chase, or by sending an email from the
    // item's page; cleared when the item is reopened. Open = neither started nor complete.
    DateTimeOffset? StartedAt = null,
    string? StartedByEmail = null,
    // The record this to-do is ABOUT (2026-09-07) — a defect today ("chase the tiler about
    // DEF-0012"), any record type tomorrow: the record's page lists its to-dos and the to-do's
    // page links back. Explicit and stored, unlike the linked-to-dos association, which rides
    // on shared mail tags — a to-do raised from a record's page has no email to share. Null =
    // not about any particular record. AboutRecordReference is resolved at read time
    // ("DEF-0012"), never stored.
    RecordType? AboutRecordType = null,
    string? AboutRecordId = null,
    string? AboutRecordReference = null)
{
    public bool IsInProgress => !IsComplete && StartedAt is not null;
    public bool IsAboutRecord => AboutRecordType is not null && !string.IsNullOrWhiteSpace(AboutRecordId);
}

// One assignee a to-do can be raised for (or moved to): a ROLE, optionally pinned to a named
// person who holds it. There is deliberately no person-without-role shape — the role is what an
// assignment falls back to when the person moves on.
public sealed record TodoAssignee(Role Role, string? PersonEmail = null);

// One pickable pin for the assignee pickers: a directory user under one of the assignable roles
// they hold, one row per (role, holder) pair. Served by ListTodoAssignablePeople.
public sealed record TodoAssignablePerson(Role Role, string Email, string DisplayName);

// One row of the triage "create to-dos from this email" form. The command carries a list of these
// so several items can be captured from a single email in one action.
//
// Assignees names EVERY role (each optionally pinned to a person) the row is meant for, and the
// row FANS OUT into one TodoItem per assignee — same title, detail and due date, but its own
// TODO-#### reference, its own mail tag and its own tick-box. One internal email that needs the QS
// to price something and the site manager to book access is therefore two independent to-dos
// raised in one action, either completable without closing the other. Empty or null = a single
// unassigned item. Duplicates are collapsed.
// LinkedTodoItemIds names EXISTING to-do items the new one is related to ("linked to-dos" — a
// flat two-way association, no hierarchy; see TodoItemLinks in contracts/Todos). Every item the
// row fans out into is linked to every named existing item, so a QS copy and a site-manager copy
// of the same task both point back at the same prior work. Empty or null = no links.
public sealed record TodoItemDraft(
    string Title,
    string? Notes = null,
    IReadOnlyList<TodoAssignee>? Assignees = null,
    DateTimeOffset? DueAt = null,
    IReadOnlyList<string>? LinkedTodoItemIds = null);

// One item the fan-out will create: the row it came from and the single assignee it lands on.
public sealed record TodoItemFanOut(TodoItemDraft Draft, TodoAssignee? Assignee)
{
    public Role? AssigneeRole => Assignee?.Role;
    public string? AssigneePersonEmail => Assignee?.PersonEmail;
}

// The fan-out rule itself, deliberately shared by the triage form — which promises a count on its
// "Create N to-do items from email" button — and the server, which actually creates the rows. Both
// call this, so the promise and the outcome cannot drift apart.
//
// Order is stable (drafts as given, assignees as picked) because the items are numbered TODO-####
// in exactly this sequence, and a triager reading the new references back should find them in the
// order they filled the form in.
public static class TodoItemDrafts
{
    public static IReadOnlyList<TodoItemFanOut> FanOutByAssignee(IEnumerable<TodoItemDraft> drafts) =>
        drafts.SelectMany(draft =>
        {
            // Distinct: the picker already stops an assignee being added twice, but a hand-rolled
            // request must not be able to raise the same item twice either. "Same" ignores email
            // case — jane@ and Jane@ are one person — but a role alone and the same role pinned to
            // a person are two different items on purpose (one for the pool, one for the person).
            var assignees = (draft.Assignees ?? Array.Empty<TodoAssignee>())
                .Aggregate(new List<TodoAssignee>(), (kept, next) =>
                {
                    if (!kept.Any(seen => seen.Role == next.Role
                            && string.Equals(seen.PersonEmail, next.PersonEmail, StringComparison.OrdinalIgnoreCase)))
                        kept.Add(next);
                    return kept;
                });
            return assignees.Count == 0
                ? new[] { new TodoItemFanOut(draft, null) }
                : assignees.Select(assignee => new TodoItemFanOut(draft, assignee)).ToArray();
        }).ToList();
}

using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// A project to-do item. Rows are created from the project's Overview tab or from an email at the
// triage stage. The sequential Number renders as "TODO-0001", which doubles as the mailbox tag stem
// ("JPMS/TODO-0001") — the link between an item and its emails is the tag, never a stored copy.
public sealed class TodoItemEntity
{
    [Key, MaxLength(64)] public string TodoItemId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    [MaxLength(2048)]    public string Notes { get; set; } = "";

    // The ROLE the item is assigned to (a Models.Role value stored as int, same convention as
    // DirectoryUserRoleEntity.Role; null = unassigned). Items belong to a role first, so they
    // survive staff changes: whoever holds the role sees them, and a new starter taking over the
    // role inherits the open items with no re-assignment.
    public int? AssigneeRole { get; set; }

    // Optional pin to ONE holder of AssigneeRole (a DirectoryUsers email; null = the whole role).
    // A pinned item is on that person's list only. The pin never outlives the person's hold on the
    // role: the directory commands clear it when the person is removed or loses the role, and the
    // item falls back to the role — the survive-staff-changes property is kept by construction.
    // Never set without AssigneeRole.
    [MaxLength(256)] public string? AssigneePersonEmail { get; set; }

    [MaxLength(256)]     public string CreatedByEmail { get; set; } = "";
    public bool IsComplete { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    // In progress: stamped by "Working on it", by a logged chase, or by an email sent from the
    // item's page; cleared on reopen. Open = neither started nor complete. See TodoActivity for
    // the timeline these stamps summarise.
    public DateTimeOffset? StartedAt { get; set; }
    [MaxLength(256)] public string? StartedByEmail { get; set; }

    // The record this item is ABOUT (a Models.RecordType stored as int + the record's id) — set
    // when the item is raised from a record's page (a defect's "New to-do") or by the connector.
    // Both or neither. Loose string id, no FK (house style); the record's page lists the items
    // about it via ListTodoItemsAboutRecord. Distinct from TodoItemLinks (to-do ↔ to-do) and from
    // the mail-tag association. Added by AddDefectSupplierAndTodoAboutRecord (2026-09-07).
    public int? AboutRecordType { get; set; }
    [MaxLength(64)] public string? AboutRecordId { get; set; }

    // Sequential, human-readable item number (rendered as TODO-0001). Global — like request and bid
    // package numbers — so the tag stem is unique across the flat JPMS mailbox-category space.
    public int Number { get; set; }

    // The canonical reference this item's emails are tagged with ("TODO-0001" -> "JPMS/TODO-0001").
    // Computed, not stored.
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Reference => $"TODO-{Number:0000}";
}

/// <summary>
/// An undirected LINK between two to-do items — "these belong together", no hierarchy, no
/// completion rules (contracts/Todos/TodoItemLinks.cs). Stored ONCE per pair with the two ids in
/// canonical order (TodoItemAId &lt; TodoItemBId, ordinal — TodoItemLinkPairs.Normalise), so A→B
/// and B→A cannot exist as two rows. Follows the house style of loose string ids with no FK
/// constraints; DeleteTodoItemHandler sweeps the rows that name a deleted item.
/// </summary>
public sealed class TodoItemLinkEntity
{
    [Key, MaxLength(64)] public string TodoItemLinkId { get; set; } = "";
    [MaxLength(64)]      public string TodoItemAId { get; set; } = "";
    [MaxLength(64)]      public string TodoItemBId { get; set; } = "";
    public DateTimeOffset LinkedAt { get; set; }
    [MaxLength(256)]     public string LinkedByEmail { get; set; } = "";
}

/// <summary>
/// One line of a to-do item's timeline (contracts/Models/TodoActivity.cs): what happened, the
/// sentence shown on the page, who did it and when. Written by every to-do command and by the
/// mailbox compose handler when an email is sent from the item's page. Loose string ids, no FK —
/// DeleteTodoItemHandler sweeps the rows that name a deleted item.
/// </summary>
public sealed class TodoItemActivityEntity
{
    [Key, MaxLength(64)] public string TodoItemActivityId { get; set; } = "";
    [MaxLength(64)]      public string TodoItemId { get; set; } = "";
    public int Kind { get; set; }
    [MaxLength(512)]     public string Summary { get; set; } = "";
    [MaxLength(256)]     public string ActorEmail { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
}

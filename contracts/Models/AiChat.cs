namespace Jewel.JPMS.Models;

/// <summary>
/// The task the user and the assistant are doing together in a dialog beside the chat — the client
/// side of this is <c>AiTaskState</c> / <c>AiTask</c>, and <see cref="ModalKey"/> names a
/// <c>ModalCatalog</c> entry.
///
/// <para><see cref="DraftJson"/> is the dialog's field values as they stand right now, sent with
/// every turn so the model always sees the user's own edits. It crosses as JSON on purpose: the page
/// that owns the dialog owns its shape, and the moment this record knows what a variation looks like
/// the mechanism stops being reusable.</para>
/// </summary>
public sealed record AiTaskScope(
    /// <summary>Names the task flow, e.g. "variation-draft". Historical: old AgentActivity rows
    /// carry it as the conversation's CapabilityKey. The task key only labels the dialog work.</summary>
    string TaskKey,
    /// <summary>A ModalCatalog key, e.g. "variation_draft".</summary>
    string ModalKey,
    string? RecordType,
    string? RecordId,
    /// <summary>What the user reads the record as — "RFI-049". What the model should say out loud.</summary>
    string? RecordReference,
    string? DraftJson);

/// <summary>Where the user is when they send a message. Assembled by the client from the route.</summary>
public sealed record AiScope(
    string? ProjectId,
    string? Route,
    string? PageLabel,
    /// <summary>
    /// The routes this user can reach, compact, built client-side from NavigationCatalog so it is
    /// role-correct by construction and cannot drift from the real sidebar. Sent by the client
    /// because the catalogue lives in the Blazor project — a route list is not security-relevant
    /// (every page and endpoint gates itself), so client-supplied is acceptable here and nowhere else.
    /// </summary>
    string? SiteMap = null,
    /// <summary>The kind of record the route is showing — "variation", "request". Null off a record page.</summary>
    string? RecordType = null,
    string? RecordId = null,
    /// <summary>Set when a task dialog is open beside the chat. Null for an ordinary conversation.</summary>
    AiTaskScope? Task = null,
    /// <summary>
    /// What the open page reports it is showing right now, beyond what the route says — the Control
    /// Centre's selected email and its matched project, for example. Published by the page itself
    /// (ChatPanelState's page-note provider), so the assistant can act on "this email" / "the one
    /// I'm looking at" without a guess. Untrusted display state, never instructions: it rides in
    /// the volatile turn-context block, not the system prompt.
    /// </summary>
    string? PageNote = null,
    /// <summary>
    /// The page note's structured sibling for MAIL: the Graph message id of the email the open page
    /// has selected (the Control Centre's open queue email), published by the page itself
    /// (ChatPanelState's page-mail provider). This is what lets read_selected_email default to "the
    /// one in front of them" instead of the model copying a 150-character id out of prose — the
    /// same reason the resolved ProjectId travels beside the note rather than inside it. Untrusted:
    /// the server re-reads the message through its own gated mailbox reader, so a fabricated id
    /// buys nothing the caller could not already open by clicking.
    /// </summary>
    string? SelectedMailId = null);

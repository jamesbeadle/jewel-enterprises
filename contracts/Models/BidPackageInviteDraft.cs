namespace Jewel.JPMS.Models;

/// <summary>
/// The outcome of drafting a tender-invite email in the shared mailbox: the package it belongs to,
/// who the draft BCCs, what it attaches, and where to open it. <see cref="WebLink"/> opens the
/// draft in Outlook on the web when Graph returns one (it usually does); null otherwise — the
/// draft is still in the mailbox's Drafts folder. Showing Bcc here is correct because the person
/// reviewing the draft is internal (Bcc stays off every subcontractor-facing surface).
/// </summary>
/// <param name="LinkedFiles">ONLY the overflow download links: the files too large to attach (the
/// ~25 MB Exchange ceiling) that travel in the body as 7-day download links instead. Null/empty
/// when everything fitted as an attachment — the usual case. Never read this as the attachment
/// list; that is <see cref="AttachedFiles"/>.</param>
/// <param name="AttachedFiles">The file names attached to the draft, in attachment order: the
/// generated pricing schedule, the company T&amp;Cs (when uploaded), the package's tender
/// documents, then its linked drawings. Null only on legacy payloads.</param>
public sealed record BidPackageInviteDraft(
    BidPackage Package,
    string Subject,
    IReadOnlyList<string> Bcc,
    string? WebLink,
    IReadOnlyList<string>? LinkedFiles = null,
    // The staged draft's mailbox message id — the handle for withdrawing the draft
    // (DeleteMailboxDraft) if it was staged in error; null only on legacy payloads.
    string? DraftMessageId = null,
    IReadOnlyList<string>? AttachedFiles = null);

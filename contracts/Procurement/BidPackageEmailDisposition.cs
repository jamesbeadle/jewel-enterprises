using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// What became of one of a bid package's tagged emails on the tender-response leg (2026-09-07).
// The mailbox tag is the only LINK (the Emails tab reads it live and this never changes it); the
// disposition is the package's own verdict on that email, held in the database because it is
// the team's shared state, not the mailbox's. Pending is the absence of a row.
public enum BidPackageEmailOutcome
{
    /// <summary>No verdict yet — listed under Tender responses, ready to extract.</summary>
    Pending = 0,
    /// <summary>Not a tender (a chase, an acknowledgement, a question) — folded away under
    /// "Discarded" on the Submissions tab; the email stays tagged to the package.</summary>
    Discarded = 1,
    /// <summary>Extracted and saved as a submission — <see cref="BidPackageEmailDisposition.QuoteId"/>
    /// names the quote it became.</summary>
    Extracted = 2
}

// One email's verdict. MessageId is the Graph id the list showed; InternetMessageId is the stable
// RFC id, so the verdict still matches the email if its Graph id changes (moved folder, re-sync).
public sealed record BidPackageEmailDisposition(
    string BidPackageId,
    string MessageId,
    string? InternetMessageId,
    BidPackageEmailOutcome Outcome,
    string? QuoteId,
    string Note,
    string SetByEmail,
    DateTimeOffset SetAt)
{
    /// <summary>Whether this verdict is about the given listed email — by Graph id, else by the
    /// stable internet id when both sides carry one.</summary>
    public bool Matches(MailboxMessage email) => Matches(email.Id, email.InternetMessageId);

    public bool Matches(string messageId, string? internetMessageId) =>
        string.Equals(MessageId, messageId, StringComparison.Ordinal)
        || (!string.IsNullOrWhiteSpace(InternetMessageId)
            && !string.IsNullOrWhiteSpace(internetMessageId)
            && string.Equals(InternetMessageId, internetMessageId, StringComparison.OrdinalIgnoreCase));
}

// Every verdict recorded on the package's emails — joined client-side to the live tagged list
// (ListBidPackageEmails), which stays the single source of what is tagged.
public sealed record ListBidPackageEmailDispositions(string BidPackageId)
    : IQuery<IReadOnlyList<BidPackageEmailDisposition>>;

// Set (or clear) the verdict on one tagged email. Outcome Discarded records "not a tender";
// Pending clears the row (Restore). Extracted is NOT set through here — SaveExtractedQuote stamps
// it when a submission is saved from an email, so the verdict and the quote can't disagree.
// SetByEmail is stamped server-side from the signed-in user. Returns the package's full list.
public sealed record SetBidPackageEmailDisposition(
    string BidPackageId,
    string MessageId,
    string? InternetMessageId,
    BidPackageEmailOutcome Outcome,
    string Note = "",
    string SetByEmail = "") : ICommand<IReadOnlyList<BidPackageEmailDisposition>>;

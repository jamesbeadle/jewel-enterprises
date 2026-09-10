using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Creates the tender-invite email as a DRAFT in the shared mailbox — nothing is sent. The mailbox
// itself is the To (subcontractors must not see each other); BCC is every recipient still in the
// running (on the list or Responded — Declined and Won are skipped) with a directory email, or
// exactly the RecipientIds given (2026-09-10, after eleven firms got the invite twice). What
// travels with it — the pricing schedule, the company T&Cs, the package's tender documents and
// its linked drawings — is planned by the assembler, and the draft carries the package's tag
// ("JPMS/BPI-0001") so the sent copy — and replies triaged onto the same tag — group under the
// package. A person reviews and sends the draft from Outlook; the caller composes Subject/HtmlBody
// in the UI first and this command drafts exactly what it is given.
public sealed record PrepareBidPackageInviteDraft(
    string BidPackageId,
    string Subject,
    string HtmlBody,
    // The tender-list rows (BidPackageRecipient.RecipientId) to BCC — null/empty means the default
    // set above. An id that does not resolve to a recipient with a directory email is ignored;
    // when none resolve the handler refuses with a readable message.
    IReadOnlyList<string>? RecipientIds = null) : ICommand<BidPackageInviteDraft>;

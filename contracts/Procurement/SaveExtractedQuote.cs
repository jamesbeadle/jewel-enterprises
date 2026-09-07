using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Commit a reviewed tender submission: creates the Quote (Value = sum of line totals) and its
// per-line pricing, marks the subcontractor's recipient row Responded, and moves an Inviting
// package to QuotesReceived. Re-submitting for the same (package, subcontractor) replaces their
// previous quote and its lines — a subbie has one live submission per package. Returns the Quote.
// SourceMessageId (+ SourceInternetMessageId) names the tagged email the submission was extracted
// from, when there was one: the handler then records that email's verdict as Extracted → this
// quote (BidPackageEmailDisposition), so the Submissions tab stops offering to extract it again.
// Both null for a tender keyed by hand. SavedByEmail is stamped server-side from the signed-in user.
public sealed record SaveExtractedQuote(
    string BidPackageId,
    string SubcontractorId,
    string Notes,
    IReadOnlyList<QuoteExtractionLine> Lines,
    string? SourceMessageId = null,
    string? SourceInternetMessageId = null,
    string SavedByEmail = "") : ICommand<Quote>;

// One priced line of the submission. Lines align to the package's line items via
// BidPackageLineItemId where the tender matched them; null marks an extra line the
// subcontractor priced that isn't on the package.
public sealed record QuoteExtractionLine(
    string? BidPackageLineItemId,
    string Description,
    string Unit,
    decimal Quantity,
    decimal Rate,
    decimal Total);

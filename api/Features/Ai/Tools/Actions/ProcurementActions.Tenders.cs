using Jewel.JPMS.Api.Features.Procurement.Commands;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class ProcurementActions
{
    private static IEnumerable<AiAction> TendersActions() => new AiAction[]
    {
        new AiAction(
            Name: "extract_tender_from_message",
            Area: "Procurement",
            Description: "Reads a subcontractor's tender email (body plus any returned "
                + "pricing-schedule spreadsheet) and proposes the submission with AI: priced lines "
                + "mapped to the package's line items, the subcontractor identified from the "
                + "sender, and every gap named. NOTHING is saved — commit the reviewed proposal "
                + "with save_extracted_quote.",
            CommandType: typeof(ExtractTenderFromMessage),
            ResultType: typeof(TenderExtraction),
            AuthorisationType: typeof(ExtractTenderFromMessageAuthorisation),
            ValidationType: typeof(ExtractTenderFromMessageValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "bidPackageId comes from list_bid_packages; messageId is a mailbox message id "
                + "from the package's correspondence."),

        new AiAction(
            Name: "save_extracted_quote",
            Area: "Procurement",
            Description: "Commits a reviewed tender submission: creates the Quote (value = sum of "
                + "line totals) and its per-line pricing, marks the subcontractor's recipient row "
                + "Responded, and moves an Inviting package to QuotesReceived. Re-submitting for "
                + "the same package and subcontractor REPLACES their previous quote and its lines. "
                + "Returns the Quote.",
            CommandType: typeof(SaveExtractedQuote),
            ResultType: typeof(Quote),
            AuthorisationType: typeof(SaveExtractedQuoteAuthorisation),
            ValidationType: typeof(SaveExtractedQuoteValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: new[] { "SavedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "Have the user review the lines (e.g. from extract_tender_from_message) before "
                + "committing. Lines align to package line items via bidPackageLineItemId; null "
                + "marks an extra line the subcontractor priced that is not on the package. Pass "
                + "sourceMessageId (and sourceInternetMessageId) when the submission came from a "
                + "tagged email: the email is then recorded as Extracted on the package's "
                + "Submissions tab instead of being offered for extraction again."),

        new AiAction(
            Name: "set_bid_package_email_disposition",
            Area: "Procurement",
            Description: "Records the package's verdict on one of its tagged emails on the "
                + "Submissions tab: outcome Discarded marks it \"not a tender\" (folded away under "
                + "Discarded, still tagged to the package — the Emails tab is unchanged); outcome "
                + "Pending clears the verdict (Restore). Never changes mailbox tags. Returns the "
                + "package's full list of verdicts.",
            CommandType: typeof(SetBidPackageEmailDisposition),
            ResultType: typeof(IReadOnlyList<BidPackageEmailDisposition>),
            AuthorisationType: typeof(SetBidPackageEmailDispositionAuthorisation),
            ValidationType: typeof(SetBidPackageEmailDispositionValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: new[] { "SetByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "messageId (and internetMessageId) come from the package's correspondence "
                + "(read_record_emails). Extracted is not set here — save_extracted_quote with "
                + "sourceMessageId stamps it. Reversible either way, so no confirmation is needed."),

        new AiAction(
            Name: "record_tender_response",
            Area: "Procurement",
            Description: "Marks the bid package recipient matching a sender email as Responded — "
                + "used when an email carrying a subcontractor's tender has been filed to the "
                + "package. Matches by exact directory email, else by a unique company domain; no "
                + "match is a quiet no-op, not a failure. Returns the package's full recipient "
                + "list.",
            CommandType: typeof(RecordTenderResponse),
            ResultType: typeof(IReadOnlyList<BidPackageRecipient>),
            AuthorisationType: typeof(RecordTenderResponseAuthorisation),
            ValidationType: typeof(RecordTenderResponseValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "This never links mail — the filing (tag) is done elsewhere; it only updates the "
                + "recipient's status."),

        new AiAction(
            Name: "submit_quote_for_bid_package",
            Area: "Procurement",
            Description: "Records a headline quote (single value plus notes, no priced lines) from "
                + "a subcontractor on a bid package. Returns the Quote.",
            CommandType: typeof(SubmitQuoteForBidPackage),
            ResultType: typeof(Quote),
            AuthorisationType: typeof(SubmitQuoteForBidPackageAuthorisation),
            ValidationType: typeof(SubmitQuoteForBidPackageValidation),
            VisibleTo: QuoteWriters,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "bidPackageId comes from list_bid_packages; subcontractorId from the tender "
                + "list's row (get_bid_package_context, tenderList[].subcontractorId). "
                + "save_extracted_quote is the richer path when per-line pricing is known."),

        new AiAction(
            Name: "revise_quote",
            Area: "Procurement",
            Description: "Revises an existing quote's value and notes in place. Returns the "
                + "updated Quote.",
            CommandType: typeof(ReviseQuote),
            ResultType: typeof(Quote),
            AuthorisationType: typeof(ReviseQuoteAuthorisation),
            ValidationType: typeof(ReviseQuoteValidation),
            VisibleTo: QuoteWriters,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "quoteId comes from the bid package's quotes (get_bid_package_context, "
                + "quotes[].quoteId)."),

    };
}

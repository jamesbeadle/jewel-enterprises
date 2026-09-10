namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiRecordTools
{
    private static AiTool BidPackageContextTool(RoleSet readers) => new(
        "get_bid_package_context",
        "Everything held ON a bid package record, in one call: title, trade, status, the "
        + "specification summary, the current line-item schedule (each with its lineItemId, cost "
        + "code and coverage), the tender list (each row with its recipientId and subcontractorId "
        + "— the handles decline_bid_package_recipient, remove_bid_package_recipient, "
        + "submit_quote_for_bid_package and award_bid_package take), the quotes received (each "
        + "with its quoteId for revise_quote), and its tender documents and linked project "
        + "documents (drawingId for set_bid_package_documents). Call this FIRST when building a "
        + "package out, changing its tender list or answering questions about one; the tagged "
        + "emails are separate — read_record_emails (record_type bid_package) has those, and "
        + "read_email_attachment opens their files. Defaults to the bid package on the page in view.",
        AiToolSchema.Object(
            ("bidPackageId", "string",
                "The bid package's id. Defaults to the record in view when the user is on "
                + "its page.", false)),
        AiToolKind.Read,
        readers,
        ReadBidPackageContextAsync);

    private static async Task<string> ReadBidPackageContextAsync(
        AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var bidPackageId = AiToolSchema.Text(input, "bidPackageId") ?? BidPackageIdInView(context);
        if (string.IsNullOrWhiteSpace(bidPackageId))
            return Fail("Say which bid package: pass bidPackageId, or have the user open its page.");

        var package = await context.Db.BidPackages.AsNoTracking()
            .FirstOrDefaultAsync(row => row.BidPackageId == bidPackageId, ct);
        if (package is null) return Fail($"No bid package found with id {bidPackageId}.");

        return Serialise(new
        {
            ok = true,
            reference = package.Reference,
            package.Title,
            package.Trade,
            status = ((BidPackageStatus)package.Status).ToString(),
            package.MaterialsApplicable,
            specificationSummary = package.SpecificationSummary,
            lineItems = await BidPackageContextReads.LineItemsOf(context.Db, bidPackageId, ct),
            tenderList = await BidPackageContextReads.TenderListOf(context.Db, bidPackageId, ct),
            quotes = await BidPackageContextReads.QuotesOf(context.Db, bidPackageId, ct),
            tenderDocuments = await BidPackageContextReads.TenderDocumentsOf(context.Db, bidPackageId, ct),
            linkedDocuments = await BidPackageContextReads.LinkedDocumentsOf(context.Db, bidPackageId, ct),
            note = "Tagged emails are separate — read_record_emails (record_type "
                   + "bid_package) returns them with full bodies and attachment ids."
        });
    }

    private static string? BidPackageIdInView(AiToolContext context)
    {
        var isOnABidPackagePage = TryMapRecordType(context.Scope?.RecordType ?? "", out var scopeType)
            && scopeType == RecordType.BidPackageInvite;
        return isOnABidPackagePage ? context.Scope?.RecordId : null;
    }
}

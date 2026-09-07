using System.Text.Json.Nodes;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{
    public async Task<XeroBillRecodeResult> RecodeBillAsync(XeroBillCodingRequest request, CancellationToken ct)
    {
        if (!_options.IsConfigured) return XeroBillRecodeResult.Failed(NotConnected);
        if (request.Lines.Count == 0)
            return XeroBillRecodeResult.Failed("Nothing to code — the schedule has no lines.");
        try
        {
            var token = await GetAccessTokenAsync(ct);

            // Always from a fresh read — the decision (editable? what VAT? what total?) must be
            // made on what Xero holds NOW, not on a ledger row synced last night.
            var fresh = await ReadFreshAsync(token, InvoicesUrl, "Invoices", request.InvoiceId, ct);
            using var doc = fresh.Document;
            if (fresh.Invoice is not { } invoice)
                return XeroBillRecodeResult.Failed("Xero returned no bill for this id — it may have been deleted.");

            var before = ReadBillSummary(invoice);
            if (!before.IsRecodable)
                return XeroBillRecodeResult.Failed(
                    $"Bill {before.InvoiceNumber ?? before.InvoiceId} can't be recoded — {before.NotRecodableReason}. "
                    + "Xero only allows line edits while nothing is paid or credited against a bill.");

            var (categories, error) = await PrepareScheduleTrackingAsync(token, request.Lines, ct);
            if (categories is null) return XeroBillRecodeResult.Failed(error!);
            var weights = request.Lines.Select(line => line.Net).ToList();
            if (weights.Sum() == 0m)
                return XeroBillRecodeResult.Failed("The schedule's lines sum to £0.00 — nothing to split the bill across.");

            // No Status in the payload: the bill keeps whatever status it has — a draft stays
            // draft for the accountant, an authorised bill stays authorised. LineAmountTypes is
            // echoed so the amounts mean what they meant.
            var payload = new JsonObject
            {
                ["InvoiceID"] = request.InvoiceId,
                ["LineAmountTypes"] = before.LineAmountTypes,
                ["LineItems"] = ProRatedLineItems(request.Lines, before, weights, categories)
            };
            using var response = await SendJsonAsync(HttpMethod.Post, token, $"{InvoicesUrl}/{request.InvoiceId}", payload, "recode bill", ct);
            ForgetSnapshot();
            return RecodeResultOf(response, before);
        }
        catch (XeroCallFailedException failure)
        {
            return XeroBillRecodeResult.Failed(failure.Message);
        }
    }

    /// <summary>
    /// The schedule is the SPLIT; the bill is the MONEY. Pro-rate the bill's own figures across
    /// the schedule's net weights: an Inclusive bill's LineAmount carries the VAT, an
    /// Exclusive/NoTax bill's doesn't, and the TaxAmount is pro-rated alongside so Xero's
    /// per-line recalculation can't drift the total by a penny.
    /// </summary>
    private static JsonArray ProRatedLineItems(
        IReadOnlyList<XeroScheduleLine> lines, XeroBillSummary before, List<decimal> weights, TrackingCategoryLookup categories)
    {
        var inclusive = before.LineAmountTypes.Equals("Inclusive", StringComparison.OrdinalIgnoreCase);
        var amounts = XeroSplitMaths.ProportionalShares(inclusive ? before.Total : before.SubTotal, weights);
        var taxes = XeroSplitMaths.ProportionalShares(before.TotalTax, weights);
        var lineItems = new JsonArray();
        for (var i = 0; i < lines.Count; i++)
            lineItems.Add(ScheduleLineItem(lines[i], amounts[i], taxes[i], before.TaxType, categories));
        return lineItems;
    }

    /// <summary>Xero answers with the bill as it now stands — the fresh LineItemIDs are what the
    /// caller re-points the timesheet cover onto, and the totals are the proof the recode moved
    /// nothing.</summary>
    private XeroBillRecodeResult RecodeResultOf(JsonDocument response, XeroBillSummary before)
    {
        var written = new List<XeroRecodedLine>();
        var after = before;
        if (FirstOf(response, "Invoices") is { } updated)
        {
            after = ReadBillSummary(updated);
            if (updated.TryGetProperty("LineItems", out var freshLines) && freshLines.ValueKind == JsonValueKind.Array)
                foreach (var line in freshLines.EnumerateArray())
                    written.Add(new XeroRecodedLine(
                        StringOf(line, "LineItemID") ?? "",
                        StringOf(line, "Description") ?? "",
                        DecimalOf(line, "LineAmount"),
                        DecimalOf(line, "TaxAmount"),
                        StringOf(line, "AccountCode") ?? "",
                        TrackingOptionOf(line, _options.SiteTrackingCategory),
                        TrackingOptionOf(line, _options.CostCodeTrackingCategory)));
        }
        return new XeroBillRecodeResult(true, null, after.Status, after.LineAmountTypes, before.TaxType,
            after.SubTotal, after.TotalTax, after.Total, written);
    }
}

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{
    // -- tracking clear (the undo of a Work Order bill approval, 2026-09-08) ---------------

    public async Task<XeroApprovalResult> ClearTrackingAsync(string invoiceId, bool isCreditNote, CancellationToken ct)
    {
        if (!_options.IsConfigured) return XeroApprovalResult.Failed(NotConnected);
        try
        {
            var token = await GetAccessTokenAsync(ct);
            var (baseUrl, collection, idProperty) = InvoiceOrCreditNote(isCreditNote);
            var fresh = await ReadFreshAsync(token, baseUrl, collection, invoiceId, ct);
            using var doc = fresh.Document;
            if (fresh.Invoice is not { } invoice)
                return XeroApprovalResult.Failed("Xero returned no invoice for this id — it may have been deleted.");

            var status = StringOf(invoice, "Status") ?? "UNKNOWN";
            if (status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
                return XeroApprovalResult.SkippedAlreadyApproved(status);
            if (!status.Equals("DRAFT", StringComparison.OrdinalIgnoreCase)
                && !status.Equals("SUBMITTED", StringComparison.OrdinalIgnoreCase)
                && !status.Equals("AUTHORISED", StringComparison.OrdinalIgnoreCase))
                return XeroApprovalResult.Failed($"The invoice is {status} in Xero — its tracking can't be updated.");
            if (HasMoneyAgainstIt(invoice))
                return XeroApprovalResult.Failed("The bill has a payment or credit against it in Xero — its lines are locked.");

            // Every line, tracking emptied: Xero treats an empty Tracking array as "no tracking".
            var payload = new JsonObject { [idProperty] = invoiceId, ["LineItems"] = LinesWithoutTracking(invoice) };
            using var response = await SendJsonAsync(HttpMethod.Post, token, $"{baseUrl}/{invoiceId}", payload, "clear tracking", ct);
            ForgetSnapshot();
            return XeroApprovalResult.Ok(status);
        }
        catch (XeroCallFailedException failure)
        {
            return XeroApprovalResult.Failed(failure.Message);
        }
    }

    private static JsonArray LinesWithoutTracking(JsonElement invoice)
    {
        var lineItems = new JsonArray();
        if (invoice.TryGetProperty("LineItems", out var originals) && originals.ValueKind == JsonValueKind.Array)
            foreach (var line in originals.EnumerateArray())
            {
                var copy = CopyLine(line, keepLineItemId: true, keepTracking: false, includeTaxAmount: true);
                copy["Tracking"] = new JsonArray();
                lineItems.Add(copy);
            }
        return lineItems;
    }
}

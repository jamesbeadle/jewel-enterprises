using System.Text.Json.Nodes;

namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{
    public async Task<XeroApprovalResult> ApproveInvoiceAsync(XeroApprovalRequest request, CancellationToken ct)
    {
        if (!_options.IsConfigured) return XeroApprovalResult.Failed(NotConnected);
        try
        {
            var token = await GetAccessTokenAsync(ct);
            var (baseUrl, collection, idProperty) = InvoiceOrCreditNote(request.IsCreditNote);
            var fresh = await ReadFreshAsync(token, baseUrl, collection, request.InvoiceId, ct);
            using var doc = fresh.Document;
            if (fresh.Invoice is not { } invoice)
                return XeroApprovalResult.Failed("Xero returned no invoice for this id — it may have been deleted.");

            var status = StringOf(invoice, "Status") ?? "UNKNOWN";
            if (status.Equals("AUTHORISED", StringComparison.OrdinalIgnoreCase)
                || status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
                return XeroApprovalResult.SkippedAlreadyApproved(status);
            if (!status.Equals("DRAFT", StringComparison.OrdinalIgnoreCase)
                && !status.Equals("SUBMITTED", StringComparison.OrdinalIgnoreCase))
                return XeroApprovalResult.Failed($"The invoice is {status} in Xero and can't be approved.");

            var lineItems = await ApprovalLineItemsAsync(token, invoice, request, ct);
            if (lineItems.Error is not null) return XeroApprovalResult.Failed(lineItems.Error);

            var payload = new JsonObject
            {
                [idProperty] = request.InvoiceId,
                ["Status"] = "AUTHORISED",
                ["LineItems"] = lineItems.Items
            };
            using var response = await SendJsonAsync(HttpMethod.Post, token, $"{baseUrl}/{request.InvoiceId}", payload, "approve invoice", ct);
            ForgetSnapshot();
            return XeroApprovalResult.Ok("AUTHORISED");
        }
        catch (XeroCallFailedException failure)
        {
            return XeroApprovalResult.Failed(failure.Message);
        }
    }

    /// <summary>
    /// The approved line list, validated against drift BEFORE touching Xero — creating tracking
    /// options for an approval that then fails would mutate Xero for nothing. A cost-code option
    /// Xero doesn't hold is refused, not created (decision 2026-09-03: the portal is deliberately
    /// not granted Xero's settings write scope); the message names the options to create by hand.
    /// </summary>
    private async Task<(JsonArray? Items, string? Error)> ApprovalLineItemsAsync(
        string token, JsonElement invoice, XeroApprovalRequest request, CancellationToken ct)
    {
        var categories = await GetTrackingCategoriesAsync(token, ct);
        var shares = request.Lines.SelectMany(line => line.Shares).ToList();
        if (MissingSitesError(shares.Select(share => share.SiteOption), categories, "project's Xero site mapping") is { } missingSites)
            return (null, missingSites);
        var lineItems = BuildLineItems(invoice, request, categories, out var buildError);
        if (lineItems is null) return (null, buildError!);
        if (MissingCostCodeOptionsError(shares.Select(share => share.CostCenterCode), categories) is { } missingCodes)
            return (null, missingCodes);
        return (lineItems, null);
    }
}

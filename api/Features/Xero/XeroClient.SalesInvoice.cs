using System.Text.Json.Nodes;

namespace Jewel.JPMS.Api.Features.Xero;

// The sales invoice raised from the portal (2026-09-09, the accountant's ask). Mirrors the staged
// draft bill's rules on the other side of the ledger: the tax type is never assumed (the contact's
// default SALES tax type, else their most recent sales invoice, else Xero's account default — the
// note says which), the Sites option must already exist in Xero, the contact is the one mapped on
// the project by ContactID (never matched by name, never created — 2026-09-10), and the invoice
// lands AUTHORISED because raising it IS the issue step.
public sealed partial class XeroClient
{
    public async Task<XeroSalesInvoiceResult> CreateSalesInvoiceAsync(XeroSalesInvoiceRequest request, CancellationToken ct)
    {
        if (!_options.IsConfigured) return XeroSalesInvoiceResult.Failed(NotConnected);
        try
        {
            var token = await GetAccessTokenAsync(ct);
            var categories = await GetTrackingCategoriesAsync(token, ct);
            if (MissingSitesError(new[] { request.SiteOption }, categories, "project's Xero site mapping") is { } missingSite)
                return XeroSalesInvoiceResult.Failed(missingSite);

            var (contactStatus, _, taxType, taxNote) = await ResolveSalesContactAsync(token, request.ContactId, ct);
            if (contactStatus == XeroSalesContactStatus.NotFound)
                return XeroSalesInvoiceResult.Failed($"The Xero contact mapped on the project was not found in Xero — re-map it in Project settings. {taxNote}");
            var payload = SalesInvoicePayload(request, SalesLineItem(request, taxType, categories));
            using var response = await SendJsonAsync(HttpMethod.Put, token, InvoicesUrl, payload, "raise sales invoice", ct);

            if (FirstOf(response, "Invoices") is not { } created)
                return XeroSalesInvoiceResult.Failed("Xero accepted the invoice but returned nothing to identify it — check Xero before retrying.");
            var summary = ReadBillSummary(created);
            ForgetSnapshot();
            return new XeroSalesInvoiceResult(
                true, summary.InvoiceId, summary.InvoiceNumber ?? "", summary.SubTotal, summary.TotalTax, summary.Total,
                $"{taxNote} Raised net £{summary.SubTotal:N2}, VAT £{summary.TotalTax:N2}, total £{summary.Total:N2}.", null);
        }
        catch (XeroCallFailedException failure)
        {
            return XeroSalesInvoiceResult.Failed(failure.Message);
        }
    }

    public async Task<XeroSalesContactLookup> LookupSalesContactAsync(string contactId, CancellationToken ct)
    {
        if (!_options.IsConfigured) return XeroSalesContactLookup.Unavailable(NotConnected);
        try
        {
            var token = await GetAccessTokenAsync(ct);
            var (status, xeroName, _, taxNote) = await ResolveSalesContactAsync(token, contactId, ct);
            return status switch
            {
                XeroSalesContactStatus.Found => XeroSalesContactLookup.Found(xeroName ?? "", taxNote),
                XeroSalesContactStatus.NotFound => XeroSalesContactLookup.NotFound(taxNote),
                _ => XeroSalesContactLookup.Unavailable(taxNote)
            };
        }
        catch (XeroCallFailedException failure)
        {
            return XeroSalesContactLookup.Unavailable($"Couldn't read the contact from Xero ({failure.Message}).");
        }
    }

    /// <summary>Always by ContactID — the payload never carries a name, so Xero can never create a contact from it.</summary>
    private static JsonObject SalesInvoicePayload(XeroSalesInvoiceRequest request, JsonObject lineItem)
    {
        var payload = new JsonObject
        {
            ["Type"] = "ACCREC",
            ["Contact"] = new JsonObject { ["ContactID"] = request.ContactId },
            ["Date"] = request.Date.ToString("yyyy-MM-dd"),
            ["Reference"] = request.Reference,
            ["Status"] = "AUTHORISED",
            ["LineAmountTypes"] = "Exclusive",
            ["LineItems"] = new JsonArray(lineItem)
        };
        if (request.DueDate is { } dueDate) payload["DueDate"] = dueDate.ToString("yyyy-MM-dd");
        return payload;
    }

    /// <summary>One line, Sites tracking only — a sales invoice carries no cost code.</summary>
    private static JsonObject SalesLineItem(XeroSalesInvoiceRequest request, string? taxType, TrackingCategoryLookup categories)
    {
        var item = new JsonObject
        {
            ["Description"] = request.Description,
            ["Quantity"] = 1m,
            ["UnitAmount"] = request.Net,
            ["LineAmount"] = request.Net,
            ["AccountCode"] = request.AccountCode,
            ["Tracking"] = new JsonArray(
                new JsonObject { ["TrackingCategoryID"] = categories.SiteCategoryId, ["Option"] = request.SiteOption })
        };
        if (!string.IsNullOrWhiteSpace(taxType)) item["TaxType"] = taxType;
        return item;
    }
}

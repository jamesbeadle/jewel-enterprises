using System.Text.Json.Nodes;

namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{
    // -- site-only tracking update (SetProject half-step, no approval) -----------------

    public async Task<XeroApprovalResult> SetSiteTrackingAsync(XeroSiteTrackingRequest request, CancellationToken ct)
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

            // Approved-but-unpaid bills accept the tracking update (decision 2026-08-14: a cost
            // moved between projects after approval must follow through to Xero's Sites
            // tracking). Paid bills are locked — Xero refuses line edits once payments are
            // applied — so those are skipped as a silent success, same as the approval path:
            // the JPMS move stands, Xero keeps its record.
            var status = StringOf(invoice, "Status") ?? "UNKNOWN";
            if (status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
                return XeroApprovalResult.SkippedAlreadyApproved(status);
            if (!status.Equals("DRAFT", StringComparison.OrdinalIgnoreCase)
                && !status.Equals("SUBMITTED", StringComparison.OrdinalIgnoreCase)
                && !status.Equals("AUTHORISED", StringComparison.OrdinalIgnoreCase))
                return XeroApprovalResult.Failed($"The invoice is {status} in Xero — its tracking can't be updated.");

            var categories = await GetTrackingCategoriesAsync(token, ct);
            var sites = request.Lines.Select(line => line.SiteOption).Where(site => site is not null).Select(site => site!);
            if (MissingSitesError(sites, categories, "project's Xero site mapping") is { } missingSites)
                return XeroApprovalResult.Failed(missingSites);

            var (lineItems, unmatched) = LinesWithSitesReplaced(invoice, request, categories);
            if (unmatched > 0)
                return XeroApprovalResult.Failed(
                    "The bill's lines have changed in Xero since they were synced "
                    + $"({unmatched} line(s) no longer exist). Sync from Xero and try again.");

            // No Status in the payload: the bill keeps whatever status it has (draft bills stay
            // draft, approved bills stay approved) — approval only ever happens through the
            // full write-back once every line is allocated.
            var payload = new JsonObject { [idProperty] = request.InvoiceId, ["LineItems"] = lineItems };
            using var response = await SendJsonAsync(HttpMethod.Post, token, $"{baseUrl}/{request.InvoiceId}", payload, "set site tracking", ct);
            ForgetSnapshot();
            return XeroApprovalResult.Ok(status);
        }
        catch (XeroCallFailedException failure)
        {
            return XeroApprovalResult.Failed(failure.Message);
        }
    }
}

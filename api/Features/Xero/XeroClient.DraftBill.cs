using System.Text.Json.Nodes;

namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{
    public async Task<XeroApprovalResult> CreateDraftBillAsync(XeroDraftBillRequest request, CancellationToken ct)
    {
        if (!_options.IsConfigured) return XeroApprovalResult.Failed(NotConnected);
        try
        {
            var token = await GetAccessTokenAsync(ct);
            var (categories, error) = await PrepareScheduleTrackingAsync(token, request.Lines, ct);
            if (categories is null) return XeroApprovalResult.Failed(error!);

            // The tax type is never assumed. Read the contact; take its default purchases tax
            // type; failing that, what they charged on their last bill; failing that, leave it
            // to Xero's account default and SAY so — the run relays the note.
            var (contactId, taxType, taxNote) = await ResolveContactTaxTypeAsync(token, request.ContactName, ct);

            var lineItems = new JsonArray();
            foreach (var line in request.Lines)
                lineItems.Add(ScheduleLineItem(line, line.Net, null, taxType, categories));
            var payload = DraftBillPayload(request, contactId, lineItems);
            using var response = await SendJsonAsync(HttpMethod.Put, token, InvoicesUrl, payload, "stage draft bill", ct);

            var billId = "";
            var note = taxNote;
            if (FirstOf(response, "Invoices") is { } created)
            {
                billId = StringOf(created, "InvoiceID") ?? "";
                var summary = ReadBillSummary(created);
                note += $" Staged net £{summary.SubTotal:N2}, VAT £{summary.TotalTax:N2}, total £{summary.Total:N2}.";
            }
            ForgetSnapshot();
            return XeroApprovalResult.Ok(billId, note);
        }
        catch (XeroCallFailedException failure)
        {
            return XeroApprovalResult.Failed(failure.Message);
        }
    }

    private static JsonObject DraftBillPayload(XeroDraftBillRequest request, string? contactId, JsonArray lineItems) => new()
    {
        ["Type"] = "ACCPAY",
        ["Contact"] = contactId is null
            ? new JsonObject { ["Name"] = request.ContactName }
            : new JsonObject { ["ContactID"] = contactId },
        ["Date"] = request.Date.ToString("yyyy-MM-dd"),
        ["DueDate"] = request.DueDate.ToString("yyyy-MM-dd"),
        ["Reference"] = request.Reference,
        ["Status"] = "DRAFT",
        ["LineAmountTypes"] = "Exclusive",
        ["LineItems"] = lineItems
    };
}

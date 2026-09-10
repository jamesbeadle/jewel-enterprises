namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{
    /// <summary>
    /// The mapped contact as Xero holds it — by ContactID only, never by name (2026-09-10: a name
    /// match created a duplicate contact) — and the tax type to raise their sales invoice with:
    /// (Found / NotFound / Unavailable, Xero's name for the contact, TaxType or null, a sentence
    /// saying where the tax type came from). The sales-side mirror of ResolveContactTaxTypeAsync:
    /// AccountsReceivableTaxType, then the most recent ACCREC invoice, then Xero's account default
    /// — said, never assumed.
    /// </summary>
    private async Task<(XeroSalesContactStatus Status, string? XeroName, string? TaxType, string Note)> ResolveSalesContactAsync(
        string token, string contactId, CancellationToken ct)
    {
        try
        {
            var contact = await FindContactByIdAsync(token, contactId, ct);
            if (contact is null)
                return (XeroSalesContactStatus.NotFound, null, null,
                    "Xero has no contact with the id mapped on the project — it may have been merged or "
                    + "archived in Xero. Re-map the Xero contact in Project settings.");

            var xeroName = StringOf(contact.Value, "Name") ?? "";
            var contactDefault = StringOf(contact.Value, "AccountsReceivableTaxType");
            if (!string.IsNullOrWhiteSpace(contactDefault))
                return (XeroSalesContactStatus.Found, xeroName, contactDefault, $"Tax type {contactDefault} from the contact's default sales tax type.");
            if (await LastSalesInvoiceTaxTypeAsync(token, contactId, ct) is { } last)
                return (XeroSalesContactStatus.Found, xeroName, last.TaxType, last.Note);
            return (XeroSalesContactStatus.Found, xeroName, null,
                "The contact has no default sales tax type and no previous sales invoice — Xero's "
                + "account default applies: check the VAT on the invoice and set the contact's default.");
        }
        catch (XeroCallFailedException failure)
        {
            return (XeroSalesContactStatus.Unavailable, null, null,
                $"Couldn't read the contact's tax type ({failure.Message}) — Xero's account default "
                + "applies: check the VAT on the invoice.");
        }
    }

    /// <summary>The contact by id as a filtered list read — an unknown or archived id comes back
    /// as an empty list (a "not found" the caller can name), where GET /Contacts/{id} is a 404.</summary>
    private async Task<JsonElement?> FindContactByIdAsync(string token, string contactId, CancellationToken ct)
    {
        var url = $"{ContactsUrl}?where={Uri.EscapeDataString($"ContactID==Guid(\"{contactId}\")")}";
        using var doc = await GetJsonAsync(token, url, "contacts", ct);
        return FirstOf(doc, "Contacts") is { } contact ? contact.Clone() : null;
    }

    /// <summary>The tax type on the contact's most recent live sales invoice, with the sentence that says so.</summary>
    private async Task<(string TaxType, string Note)?> LastSalesInvoiceTaxTypeAsync(string token, string contactId, CancellationToken ct)
    {
        var url = $"{InvoicesUrl}?where={Uri.EscapeDataString($"Type==\"ACCREC\"&&Contact.ContactID==Guid(\"{contactId}\")")}"
            + $"&order={Uri.EscapeDataString("Date DESC")}&page=1";
        using var doc = await GetJsonAsync(token, url, "invoices", ct);
        if (!doc.RootElement.TryGetProperty("Invoices", out var invoices) || invoices.ValueKind != JsonValueKind.Array)
            return null;
        foreach (var invoice in invoices.EnumerateArray())
        {
            var status = StringOf(invoice, "Status") ?? "";
            if (status.Equals("VOIDED", StringComparison.OrdinalIgnoreCase)
                || status.Equals("DELETED", StringComparison.OrdinalIgnoreCase)) continue;
            if (!invoice.TryGetProperty("LineItems", out var lines) || lines.ValueKind != JsonValueKind.Array) continue;
            var previous = DominantTaxType(lines);
            if (previous is null) continue;
            return (previous,
                $"Tax type {previous} from the contact's most recent sales invoice "
                + $"({StringOf(invoice, "InvoiceNumber") ?? StringOf(invoice, "Reference") ?? "unnumbered"}, "
                + $"{DateOf(invoice, "DateString", "Date"):dd MMM yyyy}) — the contact has no default set.");
        }
        return null;
    }
}

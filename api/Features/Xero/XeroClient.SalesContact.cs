namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{
    /// <summary>
    /// The client as Xero holds it — by ContactID when the directory's link supplies one, else
    /// by exact name — and the tax type to raise their sales invoice with: (ContactID or null,
    /// TaxType or null, a sentence saying where the tax type came from). The sales-side mirror
    /// of ResolveContactTaxTypeAsync: AccountsReceivableTaxType, then the most recent ACCREC
    /// invoice, then Xero's account default — said, never assumed.
    /// </summary>
    private async Task<(string? ContactId, string? TaxType, string Note)> ResolveSalesContactAsync(
        string token, string? contactId, string contactName, CancellationToken ct)
    {
        try
        {
            var contact = contactId is null
                ? await FindContactByNameAsync(token, contactName, ct)
                : await FindContactByIdAsync(token, contactId, ct);
            if (contact is null)
                return (null, null,
                    $"Xero has no contact named \"{contactName}\" — one is created with the invoice, and "
                    + "Xero's account default sales tax type applies: check the VAT on the invoice and set "
                    + "the contact's default sales tax type.");

            var foundId = StringOf(contact.Value, "ContactID");
            var contactDefault = StringOf(contact.Value, "AccountsReceivableTaxType");
            if (!string.IsNullOrWhiteSpace(contactDefault))
                return (foundId, contactDefault, $"Tax type {contactDefault} from the contact's default sales tax type.");
            if (foundId is not null && await LastSalesInvoiceTaxTypeAsync(token, foundId, ct) is { } last)
                return (foundId, last.TaxType, last.Note);
            return (foundId, null,
                "The contact has no default sales tax type and no previous sales invoice — Xero's "
                + "account default applies: check the VAT on the invoice and set the contact's default.");
        }
        catch (XeroCallFailedException failure)
        {
            return (contactId, null,
                $"Couldn't read the contact's tax type ({failure.Message}) — Xero's account default "
                + "applies: check the VAT on the invoice.");
        }
    }

    private async Task<JsonElement?> FindContactByNameAsync(string token, string contactName, CancellationToken ct)
    {
        var escapedName = contactName.Replace("\\", "\\\\").Replace("\"", "\\\"");
        var url = $"{ContactsUrl}?where={Uri.EscapeDataString($"Name==\"{escapedName}\"")}";
        using var doc = await GetJsonAsync(token, url, "contacts", ct);
        return FirstOf(doc, "Contacts") is { } contact ? contact.Clone() : null;
    }

    private async Task<JsonElement?> FindContactByIdAsync(string token, string contactId, CancellationToken ct)
    {
        using var doc = await GetJsonAsync(token, $"{ContactsUrl}/{contactId}", "contacts", ct);
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

namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{
    /// <summary>
    /// The contact as Xero holds it (by exact name) and the tax type to stage their bill with:
    /// (ContactID or null, TaxType or null, a sentence saying where the tax type came from).
    /// A read failure here is not fatal to the staging — the bill still goes in, with the
    /// account default and a note that says the contact couldn't be read.
    /// </summary>
    private async Task<(string? ContactId, string? TaxType, string Note)> ResolveContactTaxTypeAsync(
        string token, string contactName, CancellationToken ct)
    {
        try
        {
            var escapedName = contactName.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var contactsUrl = $"{ContactsUrl}?where={Uri.EscapeDataString($"Name==\"{escapedName}\"")}";
            using var contactsDoc = await GetJsonAsync(token, contactsUrl, "contacts", ct);
            if (FirstOf(contactsDoc, "Contacts") is not { } contact)
                return (null, null,
                    $"Xero has no contact named \"{contactName}\" — one was created with the bill, "
                    + "and Xero's account default tax type applied: check the VAT on the bill and set "
                    + "the contact's default purchases tax type.");

            var contactId = StringOf(contact, "ContactID");
            var contactDefault = StringOf(contact, "AccountsPayableTaxType");
            if (!string.IsNullOrWhiteSpace(contactDefault))
                return (contactId, contactDefault, $"Tax type {contactDefault} from the contact's default.");
            if (contactId is not null && await LastBillTaxTypeAsync(token, contactId, ct) is { } lastBill)
                return (contactId, lastBill.TaxType, lastBill.Note);
            return (contactId, null,
                "The contact has no default purchases tax type and no previous bill — Xero's "
                + "account default applied: check the VAT on the bill and set the contact's default.");
        }
        catch (XeroCallFailedException failure)
        {
            return (null, null,
                $"Couldn't read the contact's tax type ({failure.Message}) — Xero's account default "
                + "applied: check the VAT on the bill.");
        }
    }

    /// <summary>The tax type on the contact's most recent live bill, with the sentence that says so.</summary>
    private async Task<(string TaxType, string Note)?> LastBillTaxTypeAsync(string token, string contactId, CancellationToken ct)
    {
        var billsUrl = $"{InvoicesUrl}?where={Uri.EscapeDataString($"Type==\"ACCPAY\"&&Contact.ContactID==Guid(\"{contactId}\")")}"
            + $"&order={Uri.EscapeDataString("Date DESC")}&page=1";
        using var billsDoc = await GetJsonAsync(token, billsUrl, "invoices", ct);
        if (!billsDoc.RootElement.TryGetProperty("Invoices", out var bills) || bills.ValueKind != JsonValueKind.Array)
            return null;
        foreach (var bill in bills.EnumerateArray())
        {
            var status = StringOf(bill, "Status") ?? "";
            if (status.Equals("VOIDED", StringComparison.OrdinalIgnoreCase)
                || status.Equals("DELETED", StringComparison.OrdinalIgnoreCase)) continue;
            if (!bill.TryGetProperty("LineItems", out var lines) || lines.ValueKind != JsonValueKind.Array) continue;
            var previous = DominantTaxType(lines);
            if (previous is null) continue;
            return (previous,
                $"Tax type {previous} from the contact's most recent bill "
                + $"({StringOf(bill, "InvoiceNumber") ?? StringOf(bill, "Reference") ?? "unnumbered"}, "
                + $"{DateOf(bill, "DateString", "Date"):dd MMM yyyy}) — the contact has no default set.");
        }
        return null;
    }
}

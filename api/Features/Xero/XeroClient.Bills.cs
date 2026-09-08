namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{
    public async Task<XeroBillSummary?> GetBillAsync(string invoiceId, CancellationToken ct)
    {
        if (!_options.IsConfigured) throw new XeroCallFailedException(NotConnected);
        var token = await GetAccessTokenAsync(ct);
        (JsonDocument Document, JsonElement? Invoice) fresh;
        try
        {
            fresh = await ReadFreshAsync(token, InvoicesUrl, "Invoices", invoiceId, ct);
        }
        catch (XeroCallFailedException failure) when (failure.Message.Contains("HTTP 404"))
        {
            return null; // Xero has no bill by that id — deleted, or never existed.
        }
        using (fresh.Document)
        {
            return fresh.Invoice is { } invoice ? ReadBillSummary(invoice) : null;
        }
    }

    private static XeroBillSummary ReadBillSummary(JsonElement invoice)
    {
        var lineCount = 0;
        string? taxType = null;
        if (invoice.TryGetProperty("LineItems", out var lines) && lines.ValueKind == JsonValueKind.Array)
        {
            lineCount = lines.GetArrayLength();
            taxType = DominantTaxType(lines);
        }
        return new XeroBillSummary(
            StringOf(invoice, "InvoiceID") ?? "",
            StringOf(invoice, "Status") ?? "UNKNOWN",
            StringOf(invoice, "InvoiceNumber"),
            StringOf(invoice, "Reference"),
            invoice.TryGetProperty("Contact", out var contact) ? StringOf(contact, "Name") : null,
            DateOf(invoice, "DateString", "Date"),
            StringOf(invoice, "LineAmountTypes") ?? "Exclusive",
            DecimalOf(invoice, "SubTotal"),
            DecimalOf(invoice, "TotalTax"),
            DecimalOf(invoice, "Total"),
            DecimalOf(invoice, "AmountPaid"),
            DecimalOf(invoice, "AmountCredited"),
            DecimalOf(invoice, "AmountDue"),
            lineCount,
            taxType,
            DateOf(invoice, "UpdatedDateUTCString", "UpdatedDateUTC"));
    }

    /// <summary>The tax type the bill's lines carry — the one on the largest line when they
    /// differ (a Dext bill is one line, so this is simply its tax type). Null when none is set.</summary>
    private static string? DominantTaxType(JsonElement lines)
    {
        string? best = null;
        var bestAmount = decimal.MinValue;
        foreach (var line in lines.EnumerateArray())
        {
            var taxType = StringOf(line, "TaxType");
            if (string.IsNullOrWhiteSpace(taxType)) continue;
            var amount = Math.Abs(DecimalOf(line, "LineAmount"));
            if (amount > bestAmount) { best = taxType; bestAmount = amount; }
        }
        return best;
    }
}

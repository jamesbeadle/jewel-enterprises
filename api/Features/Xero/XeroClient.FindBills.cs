namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{
    public async Task<IReadOnlyList<XeroBillSummary>> FindBillsByNumberAsync(string invoiceNumber, CancellationToken ct)
    {
        if (!_options.IsConfigured) throw new XeroCallFailedException(NotConnected);
        var token = await GetAccessTokenAsync(ct);
        var where = $"Type==\"ACCPAY\"&&InvoiceNumber==\"{EscapedForWhere(invoiceNumber)}\"";
        var url = $"{InvoicesUrl}?where={Uri.EscapeDataString(where)}&order={Uri.EscapeDataString("Date DESC")}&page=1";
        using var doc = await GetJsonAsync(token, url, "invoices", ct);
        if (!doc.RootElement.TryGetProperty("Invoices", out var bills) || bills.ValueKind != JsonValueKind.Array)
            return Array.Empty<XeroBillSummary>();
        return bills.EnumerateArray().Select(ReadBillSummary).ToList();
    }

    /// <summary>A string literal inside a Xero where clause: backslashes and double quotes escaped.</summary>
    private static string EscapedForWhere(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}

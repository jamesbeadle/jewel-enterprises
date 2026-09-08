using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

/// <summary>How the coding run names things in the outcomes it records.</summary>
internal static class XeroCodingWording
{
    public const string NotSignedOff = "Not every week with approved time is signed off — sign the month off first.";

    public const string NoSettlementIdentity = "The worker has no settlement identity — link a subcontractor company or flag "
        + "them a sole trader (Workers page, or the allocation page's inline fix) so the run knows whose bill to look for.";

    public static bool IsGone(string status) =>
        status.Equals("VOIDED", StringComparison.OrdinalIgnoreCase)
        || status.Equals("DELETED", StringComparison.OrdinalIgnoreCase);

    public static string BillLabel(XeroBillSummary bill) =>
        !string.IsNullOrWhiteSpace(bill.InvoiceNumber) ? $"\"{bill.InvoiceNumber}\""
        : !string.IsNullOrWhiteSpace(bill.Reference) ? $"\"{bill.Reference}\""
        : bill.InvoiceId;

    public static string LedgerBillLabel(XeroLedgerLineEntity line) =>
        !string.IsNullOrWhiteSpace(line.InvoiceNumber) ? $"\"{line.InvoiceNumber}\""
        : !string.IsNullOrWhiteSpace(line.Reference) ? $"\"{line.Reference}\""
        : line.XeroInvoiceId;

    public static string LinesSummary(IReadOnlyList<XeroScheduleLine> lines, string label = "Lines") =>
        $"{label}: " + string.Join("; ", lines.Select(line => $"{line.SiteOption} / {line.CostCodeOption} → {line.AccountCode} £{line.Net:N2}")) + ".";

    public static string NatureLabel(SettlementLineNature nature) => nature switch
    {
        SettlementLineNature.CisLabour => "labour",
        SettlementLineNature.CisMaterials => "materials",
        _ => "travel",
    };

    public static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];
}

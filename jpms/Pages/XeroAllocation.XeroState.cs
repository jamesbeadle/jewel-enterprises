using static Jewel.JPMS.Features.Xero.XeroLedgerDisplay;

namespace Jewel.JPMS.Pages;

public partial class XeroAllocation
{
    // -- Allocated tab: where each bill stands in Xero (2026-09-08, the accountant's ask) ------
    // The Allocated tab used to say only what the PORTAL did (WriteBackStatus), and None covered
    // both "approved outside JPMS" and "still draft, nobody has written it". The chips narrow the
    // tab by what XERO holds — the bill's status as last synced or stamped by a write — and by
    // the write-backs that are failing right now. Counts come off the tab's loaded lines.

    private const string DraftInXeroFilter = "draft";
    private const string WriteBackFailedFilter = "failed";
    private string allocatedXeroFilter = "";

    private static bool IsWriteBackFailed(XeroLedgerLine line) =>
        line.WriteBackStatus == XeroWriteBackStatus.Failed;

    private bool MatchesAllocatedXeroFilter(XeroLedgerLine line) => allocatedXeroFilter switch
    {
        DraftInXeroFilter => IsAwaitingApproval(line),
        WriteBackFailedFilter => IsWriteBackFailed(line),
        _ => true
    };

    /// <summary>The chip row for the Allocated tab, counted off its loaded lines (null before they land).</summary>
    private IReadOnlyList<TabItem>? AllocatedXeroChips
    {
        get
        {
            var lines = Lines;
            if (lines is null || activeTab != XeroAllocationStatus.Allocated) return null;
            return new[]
            {
                new TabItem("", "All"),
                new TabItem(DraftInXeroFilter, "Draft in Xero", Count: lines.Count(IsAwaitingApproval),
                    Title: "Allocated here but still awaiting approval in Xero — the write-back has not happened or has not worked yet"),
                new TabItem(WriteBackFailedFilter, "Write-back failed", Count: lines.Count(IsWriteBackFailed),
                    Title: "The last attempt to confirm the coding to Xero was refused — the error is on the row, Retry re-attempts it")
            };
        }
    }

    private void SelectAllocatedXeroFilter(string key)
    {
        allocatedXeroFilter = key;
        page = 0;
    }

    /// <summary>The bill's Xero status as the export and the row say it.</summary>
    private static string XeroStatusText(XeroLedgerLine line) => line.InvoiceStatus.ToUpperInvariant() switch
    {
        "DRAFT" => "Draft",
        "SUBMITTED" => "Submitted",
        "AUTHORISED" => "Approved",
        "PAID" => "Paid",
        "VOIDED" => "Voided",
        "DELETED" => "Deleted",
        _ => line.InvoiceStatus
    };

    private static string WriteBackText(XeroLedgerLine line) => line.WriteBackStatus switch
    {
        XeroWriteBackStatus.Approved => "Approved by JPMS" + (line.WriteBackFailedAtUtc is { } failedAt ? $" (after a failure on {DateText(failedAt.ToLocalTime())})" : ""),
        XeroWriteBackStatus.Failed => "Failed",
        _ when IsAwaitingApproval(line) => "Not yet written",
        _ => "Not needed"
    };
}

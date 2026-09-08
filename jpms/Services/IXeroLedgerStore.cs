
namespace Jewel.JPMS.Services;

public interface IXeroLedgerStore
{
    /// <summary>
    /// Stored ledger lines for one allocation status, or null while that status' first load is in
    /// flight. Reads are per status because the allocation page is a tab per status — asking for
    /// one no longer drags the whole ledger into the browser. Calling this starts the load if it
    /// hasn't happened yet, so it is safe to read from render.
    /// </summary>
    IReadOnlyList<XeroLedgerLine>? Lines(XeroAllocationStatus status);

    /// <summary>How many lines sit in each status, for the tab bar. Null before the first load.</summary>
    XeroLedgerCounts? Counts();

    /// <summary>Reloads one status and the counts. Call on entry and on tab switch.</summary>
    Task RefreshAsync(XeroAllocationStatus status, CancellationToken cancellationToken = default);

    /// <summary>Pulls the latest from Xero into the stored ledger, then refreshes the view.</summary>
    Task<XeroLedgerSyncResult> SyncAsync(CancellationToken cancellationToken = default);

    /// <summary>Applies an allocation action to a batch of lines, then refreshes. Returns lines affected.</summary>
    Task<int> ApplyAsync(SetXeroAllocation command, CancellationToken cancellationToken = default);

    /// <summary>Allocates every unallocated line whose project AND cost centre both matched, then refreshes.</summary>
    Task<int> AllocateSuggestedAsync(CancellationToken cancellationToken = default);

    /// <summary>Re-attempts a failed Xero write-back (tracking + approval) for one invoice, then refreshes.</summary>
    Task<XeroWriteBackOutcome> RetryWriteBackAsync(string xeroInvoiceId, CancellationToken cancellationToken = default);

    /// <summary>Approves one Work Order bill — every line allocated from its order, linked, confirmed to Xero — then refreshes.</summary>
    Task<WorkOrderBillApprovalOutcome> ApproveWorkOrderBillAsync(ApproveWorkOrderBill command, CancellationToken cancellationToken = default);

    /// <summary>Reverses a Work Order bill approval (allocation, links, tracking) in one go, then refreshes.</summary>
    Task<WorkOrderBillUndoOutcome> UndoWorkOrderBillApprovalAsync(string xeroInvoiceId, CancellationToken cancellationToken = default);

    event Action? OnChange;
}

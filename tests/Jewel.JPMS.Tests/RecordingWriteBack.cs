using Jewel.JPMS.Api.Features.Xero.Ledger;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Tests;

/// <summary>Stands in for the Xero write-back service: records what each handler asked for.</summary>
internal sealed class RecordingWriteBack : IXeroWriteBackService
{
    public List<string> Calls { get; } = new();
    public XeroWriteBackOutcome WorkOrderBillOutcome { get; set; } = new(true, null);
    public XeroTrackingClearOutcome ClearOutcome { get; set; } = new(true, null, "AUTHORISED");

    public Task TryWriteBackAsync(IReadOnlyCollection<string> xeroInvoiceIds, CancellationToken ct)
    {
        Calls.Add("WriteBack:" + string.Join(",", xeroInvoiceIds));
        return Task.CompletedTask;
    }

    public Task TrySetSiteAsync(IReadOnlyCollection<string> xeroLedgerLineIds, CancellationToken ct)
    {
        Calls.Add("SetSite:" + string.Join(",", xeroLedgerLineIds));
        return Task.CompletedTask;
    }

    public Task<XeroWriteBackOutcome> RetryAsync(string xeroInvoiceId, CancellationToken ct) => throw new NotSupportedException();

    public Task<XeroWriteBackOutcome> WriteBackWorkOrderBillAsync(string xeroInvoiceId, CancellationToken ct)
    {
        Calls.Add("WorkOrderBill:" + xeroInvoiceId);
        return Task.FromResult(WorkOrderBillOutcome);
    }

    public Task<XeroTrackingClearOutcome> TryClearTrackingAsync(string xeroInvoiceId, CancellationToken ct)
    {
        Calls.Add("ClearTracking:" + xeroInvoiceId);
        return Task.FromResult(ClearOutcome);
    }
}

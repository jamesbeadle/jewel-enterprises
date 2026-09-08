using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

public sealed partial class XeroWriteBackService
{
    public async Task<XeroWriteBackOutcome> WriteBackWorkOrderBillAsync(string xeroInvoiceId, CancellationToken ct)
    {
        try
        {
            return await WriteBackInvoiceAsync(xeroInvoiceId, explicitRetry: true, ct, recodeApproved: true);
        }
        catch (Exception unexpected)
        {
            logger.LogError(unexpected, "Xero write-back of Work Order bill {InvoiceId} failed unexpectedly.", xeroInvoiceId);
            return new XeroWriteBackOutcome(false, unexpected.Message);
        }
    }

    public async Task<XeroTrackingClearOutcome> TryClearTrackingAsync(string xeroInvoiceId, CancellationToken ct)
    {
        try
        {
            var line = await context.XeroLedgerLines.AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.XeroInvoiceId == xeroInvoiceId, ct);
            if (line is null)
                return new XeroTrackingClearOutcome(false, "No stored ledger lines for this invoice.", "");

            var result = await xero.ClearTrackingAsync(xeroInvoiceId, line.Type == "ACCPAYCREDIT", ct);
            var status = result.FreshStatus ?? line.InvoiceStatus;
            if (result.Succeeded)
            {
                logger.LogInformation("Xero tracking cleared off invoice {InvoiceId} ({Status}).", xeroInvoiceId, status);
                return new XeroTrackingClearOutcome(true, null, status);
            }
            logger.LogWarning("Xero tracking clear failed for invoice {InvoiceId}: {Error}", xeroInvoiceId, result.Error);
            return new XeroTrackingClearOutcome(false, result.Error ?? "Xero rejected the tracking update.", status);
        }
        catch (Exception unexpected)
        {
            logger.LogError(unexpected, "Xero tracking clear failed unexpectedly for invoice {InvoiceId}.", xeroInvoiceId);
            return new XeroTrackingClearOutcome(false, unexpected.Message, "");
        }
    }
}

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
            return await WriteBackInvoiceAsync(xeroInvoiceId, explicitRetry: true, ct, recodeApproved: true, keepLinesWhole: true);
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
            var lines = await context.XeroLedgerLines
                .Where(candidate => candidate.XeroInvoiceId == xeroInvoiceId)
                .ToListAsync(ct);
            if (lines.Count == 0)
                return new XeroTrackingClearOutcome(false, "No stored ledger lines for this invoice.", "");

            var result = await xero.ClearTrackingAsync(xeroInvoiceId, lines[0].Type == "ACCPAYCREDIT", ct);
            var status = result.FreshStatus ?? lines[0].InvoiceStatus;
            if (StampXeroStatus(lines, result.FreshStatus)) await context.SaveChangesAsync(ct);
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

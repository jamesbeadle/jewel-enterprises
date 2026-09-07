using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Xero;
using static Jewel.JPMS.Api.Features.Labour.Commands.XeroCodingWording;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed partial class RunXeroCodingHandler
{
    /// <summary>The fresh line as the ledger stores it: the bill's facts from Xero's answer, the
    /// old row's facts (type, attachments, first seen) where the answer doesn't carry them.</summary>
    private static XeroLedgerLineEntity RecodedLedgerLine(
        string billId, XeroRecodedLine line, List<XeroLedgerLineEntity> storedLines, XeroBillSummary before, XeroBillRecodeResult recode, DateTimeOffset now)
    {
        var template = storedLines.FirstOrDefault();
        var accountName = storedLines.FirstOrDefault(stored => string.Equals(stored.AccountCode, line.AccountCode, StringComparison.OrdinalIgnoreCase))?.AccountName;
        return new XeroLedgerLineEntity
        {
            XeroLedgerLineId = $"{billId}:{line.LineItemId}",
            XeroInvoiceId = billId,
            XeroLineItemId = line.LineItemId,
            Type = template?.Type ?? "ACCPAY",
            InvoiceNumber = before.InvoiceNumber is null ? template?.InvoiceNumber : Truncate(before.InvoiceNumber, 64),
            Reference = before.Reference is null ? template?.Reference : Truncate(before.Reference, 256),
            ContactName = before.ContactName is null ? template?.ContactName : Truncate(before.ContactName, 256),
            Date = before.Date ?? template?.Date,
            InvoiceStatus = Truncate(recode.Status, 32)!,
            Description = Truncate(line.Description, 1024),
            Net = line.LineAmount,
            Tax = line.TaxAmount,
            InvoiceTotal = recode.Total,
            AmountDue = template?.AmountDue ?? before.AmountDue,
            AccountCode = Truncate(line.AccountCode, 32),
            AccountName = accountName,
            XeroSite = Truncate(line.SiteOption, 128),
            XeroCostCode = Truncate(line.CostCodeOption, 128),
            HasAttachments = template?.HasAttachments ?? false,
            AllocationStatus = (int)XeroAllocationStatus.Unallocated,
            FirstSeenAtUtc = template?.FirstSeenAtUtc ?? now,
            LastSyncedAtUtc = now,
        };
    }
}

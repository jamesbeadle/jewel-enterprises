using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Tests;

/// <summary>Xero as the coding run sees it: bills by id, the bills held under a number, a draft
/// that lands with a given id, a recode that answers with fresh line ids — every call recorded
/// in order.</summary>
internal sealed class RecordingXero : IXeroClient
{
    public List<string> Calls { get; } = new();
    public Dictionary<string, XeroBillSummary?> Bills { get; } = new();
    public Dictionary<string, List<XeroBillSummary>> BillsByNumber { get; } = new();
    public string StagedBillId { get; set; } = "";
    public string[] RecodedLineIds { get; set; } = Array.Empty<string>();
    public XeroDraftBillRequest? Draft { get; private set; }
    public XeroBillCodingRequest? Recode { get; private set; }
    public XeroApprovalRequest? Approval { get; private set; }

    public bool IsConfigured => true;

    public Task<XeroBillSummary?> GetBillAsync(string invoiceId, CancellationToken ct)
    {
        Calls.Add($"GetBill:{invoiceId}");
        return Task.FromResult(Bills.TryGetValue(invoiceId, out var bill) ? bill : null);
    }

    public Task<IReadOnlyList<XeroBillSummary>> FindBillsByNumberAsync(string invoiceNumber, CancellationToken ct)
    {
        Calls.Add($"FindBills:{invoiceNumber}");
        IReadOnlyList<XeroBillSummary> found = BillsByNumber.TryGetValue(invoiceNumber, out var bills) ? bills : new List<XeroBillSummary>();
        return Task.FromResult(found);
    }

    public Task<XeroApprovalResult> CreateDraftBillAsync(XeroDraftBillRequest request, CancellationToken ct)
    {
        Calls.Add("CreateDraftBill");
        Draft = request;
        return Task.FromResult(XeroApprovalResult.Ok(StagedBillId, "Tax from the contact."));
    }

    public Task<XeroBillRecodeResult> RecodeBillAsync(XeroBillCodingRequest request, CancellationToken ct)
    {
        Calls.Add($"RecodeBill:{request.InvoiceId}");
        Recode = request;
        var before = Bills[request.InvoiceId]!;
        var lines = request.Lines.Select((line, index) => new XeroRecodedLine(
            RecodedLineIds[index], line.Description, line.Net, 0m, line.AccountCode, line.SiteOption, line.CostCodeOption)).ToList();
        return Task.FromResult(new XeroBillRecodeResult(true, null, before.Status, before.LineAmountTypes, before.TaxType,
            before.SubTotal, before.TotalTax, before.Total, lines));
    }

    public Task<XeroTransactionsSnapshot> GetPurchaseInvoicesAsync(bool force, CancellationToken ct) => throw new NotSupportedException();
    public Task<XeroCashSummarySnapshot> GetCashSummaryAsync(bool force, CancellationToken ct) => throw new NotSupportedException();
    public Task<XeroAgedPayablesSnapshot> GetAgedPayablesAsync(bool force, CancellationToken ct) => throw new NotSupportedException();
    public Task<XeroAgedReceivablesSnapshot> GetAgedReceivablesAsync(bool force, CancellationToken ct) => throw new NotSupportedException();
    public List<XeroSupplier> Suppliers { get; } = new();

    public Task<XeroSuppliersSnapshot> GetSuppliersAsync(bool force, CancellationToken ct)
    {
        Calls.Add(force ? "GetSuppliers:force" : "GetSuppliers");
        return Task.FromResult(new XeroSuppliersSnapshot(true, null, DateTimeOffset.UtcNow, false, Suppliers.ToList()));
    }
    public Task<XeroTrackingCategoriesSnapshot> GetTrackingCategoriesSnapshotAsync(bool force, CancellationToken ct) => throw new NotSupportedException();
    /// <summary>A fixed answer for the next approval / site write when a test sets one (the
    /// write-back tests); otherwise approval answers as the real client would, off Bills.</summary>
    public XeroApprovalResult? ApprovalResult { get; set; }
    public XeroApprovalResult SiteTrackingResult { get; set; } = XeroApprovalResult.Ok("DRAFT");

    /// <summary>Approval as the real client answers it: an unknown bill fails, an approved or
    /// paid one is acknowledged untouched, a voided one refuses, a draft becomes AUTHORISED.</summary>
    public Task<XeroApprovalResult> ApproveInvoiceAsync(XeroApprovalRequest request, CancellationToken ct)
    {
        Calls.Add($"ApproveInvoice:{request.InvoiceId}");
        Approval = request;
        if (ApprovalResult is { } fixedAnswer) return Task.FromResult(fixedAnswer);
        if (!Bills.TryGetValue(request.InvoiceId, out var bill) || bill is null)
            return Task.FromResult(XeroApprovalResult.Failed("Xero returned no invoice for this id — it may have been deleted."));
        if (bill.Status is "AUTHORISED" or "PAID") return Task.FromResult(XeroApprovalResult.SkippedAlreadyApproved(bill.Status));
        if (bill.Status is "VOIDED" or "DELETED")
            return Task.FromResult(XeroApprovalResult.Failed($"The invoice is {bill.Status} in Xero and can't be approved."));
        Bills[request.InvoiceId] = bill with { Status = "AUTHORISED" };
        return Task.FromResult(XeroApprovalResult.Ok("AUTHORISED"));
    }

    public Task<XeroApprovalResult> SetSiteTrackingAsync(XeroSiteTrackingRequest request, CancellationToken ct)
    {
        Calls.Add($"SetSite:{request.InvoiceId}");
        return Task.FromResult(SiteTrackingResult);
    }
    public Task<XeroApprovalResult> ClearTrackingAsync(string invoiceId, bool isCreditNote, CancellationToken ct)
    {
        Calls.Add($"ClearTracking:{invoiceId}");
        return Task.FromResult(XeroApprovalResult.Ok("AUTHORISED"));
    }
    public Task<IReadOnlyList<XeroInvoiceAttachment>> ListAttachmentsAsync(string invoiceId, bool isCreditNote, CancellationToken ct) => throw new NotSupportedException();
    public Task<XeroAttachmentContent?> GetAttachmentAsync(string invoiceId, bool isCreditNote, string fileName, CancellationToken ct) => throw new NotSupportedException();
    public Task<IReadOnlyList<XeroSitePnlMonthFigures>> GetSiteMonthlyPnlAsync(string siteOption, DateTime fromMonth, DateTime toMonth, CancellationToken ct) => throw new NotSupportedException();
    public Task<XeroSitePnlRangeFigures?> GetSiteRangePnlAsync(string siteOption, DateTime fromDate, DateTime toDate, CancellationToken ct) => throw new NotSupportedException();
}

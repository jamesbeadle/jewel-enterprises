using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero;
/// <summary>No-op used when no Xero client id/secret is configured; reports itself as such.</summary>
public sealed class NullXeroClient : IXeroClient
{
    public bool IsConfigured => false;

    public Task<XeroTransactionsSnapshot> GetPurchaseInvoicesAsync(bool force, CancellationToken ct) =>
        Task.FromResult(XeroTransactionsSnapshot.NotConfigured());

    public Task<XeroCashSummarySnapshot> GetCashSummaryAsync(bool force, CancellationToken ct) =>
        Task.FromResult(XeroCashSummarySnapshot.NotConfigured());

    public Task<XeroAgedPayablesSnapshot> GetAgedPayablesAsync(bool force, CancellationToken ct) =>
        Task.FromResult(XeroAgedPayablesSnapshot.NotConfigured());

    public Task<XeroAgedReceivablesSnapshot> GetAgedReceivablesAsync(bool force, CancellationToken ct) =>
        Task.FromResult(XeroAgedReceivablesSnapshot.NotConfigured());

    public Task<XeroTrackingCategoriesSnapshot> GetTrackingCategoriesSnapshotAsync(bool force, CancellationToken ct) =>
        Task.FromResult(XeroTrackingCategoriesSnapshot.NotConfigured());

    public Task<XeroSuppliersSnapshot> GetSuppliersAsync(bool force, CancellationToken ct) =>
        Task.FromResult(XeroSuppliersSnapshot.NotConfigured());

    public Task<XeroContactPeople?> GetContactPeopleAsync(string contactId, CancellationToken ct) =>
        throw new XeroCallFailedException("Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings.");

    public Task<XeroApprovalResult> SetContactPeopleAsync(XeroContactPeople people, CancellationToken ct) =>
        Task.FromResult(XeroApprovalResult.Failed(
            "Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings."));

    public Task<XeroApprovalResult> ApproveInvoiceAsync(XeroApprovalRequest request, CancellationToken ct) =>
        Task.FromResult(XeroApprovalResult.Failed(
            "Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings."));

    public Task<XeroApprovalResult> SetSiteTrackingAsync(XeroSiteTrackingRequest request, CancellationToken ct) =>
        Task.FromResult(XeroApprovalResult.Failed(
            "Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings."));

    public Task<XeroApprovalResult> ClearTrackingAsync(string invoiceId, bool isCreditNote, CancellationToken ct) =>
        Task.FromResult(XeroApprovalResult.Failed(
            "Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings."));

    public Task<XeroBillSummary?> GetBillAsync(string invoiceId, CancellationToken ct) =>
        throw new XeroCallFailedException(
            "Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings.");

    public Task<IReadOnlyList<XeroBillSummary>> FindBillsByNumberAsync(string invoiceNumber, CancellationToken ct) =>
        throw new XeroCallFailedException(
            "Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings.");

    public Task<XeroBillRecodeResult> RecodeBillAsync(XeroBillCodingRequest request, CancellationToken ct) =>
        Task.FromResult(XeroBillRecodeResult.Failed(
            "Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings."));

    public Task<XeroApprovalResult> CreateDraftBillAsync(XeroDraftBillRequest request, CancellationToken ct) =>
        Task.FromResult(XeroApprovalResult.Failed(
            "Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings."));

    public Task<IReadOnlyList<XeroInvoiceAttachment>> ListAttachmentsAsync(
        string invoiceId, bool isCreditNote, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<XeroInvoiceAttachment>>(Array.Empty<XeroInvoiceAttachment>());

    public Task<XeroAttachmentContent?> GetAttachmentAsync(
        string invoiceId, bool isCreditNote, string fileName, CancellationToken ct) =>
        Task.FromResult<XeroAttachmentContent?>(null);

    private const string NotConnected = "Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings.";

    public Task<XeroSalesInvoiceResult> CreateSalesInvoiceAsync(XeroSalesInvoiceRequest request, CancellationToken ct) =>
        Task.FromResult(XeroSalesInvoiceResult.Failed(NotConnected));

    public Task<XeroSalesContactLookup> LookupSalesContactAsync(string? contactId, string contactName, CancellationToken ct) =>
        Task.FromResult(new XeroSalesContactLookup(null, NotConnected));

    public Task<XeroApprovalResult> AttachToInvoiceAsync(
        string invoiceId, string fileName, string contentType, byte[] content, CancellationToken ct) =>
        Task.FromResult(XeroApprovalResult.Failed(NotConnected));

    public Task<IReadOnlyList<XeroSitePnlMonthFigures>> GetSiteMonthlyPnlAsync(
        string siteOption, DateTime fromMonth, DateTime toMonth, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<XeroSitePnlMonthFigures>>(Array.Empty<XeroSitePnlMonthFigures>());

    public Task<XeroSitePnlRangeFigures?> GetSiteRangePnlAsync(
        string siteOption, DateTime fromDate, DateTime toDate, CancellationToken ct) =>
        Task.FromResult<XeroSitePnlRangeFigures?>(null);
}


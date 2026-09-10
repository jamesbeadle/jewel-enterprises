using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Services;

public interface IValuationInvoiceStore
{
    event Action? OnChange;

    Task<IReadOnlyList<ValuationInvoice>> ListAsync(string projectId, CancellationToken cancellationToken = default);
    Task<ProjectValuationInvoiceSummary> GetSummaryAsync(string projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ValuationInvoiceEvent>> ListEventsAsync(string valuationInvoiceId, CancellationToken cancellationToken = default);

    Task<ValuationInvoice> CreateAsync(string projectId, DateTimeOffset periodMonth, decimal amount, string? valuationClaimId, CancellationToken cancellationToken = default);

    /// <summary>Backdated historic entry, recorded directly as Issued (or Paid when a paid
    /// amount is given) so receipts-to-date can be brought current. Counts toward
    /// "Certified to date" immediately.</summary>
    Task<ValuationInvoice> CreateManualAsync(string projectId, DateTimeOffset periodMonth, decimal amount, decimal? amountPaid, DateTimeOffset? issuedAt, DateTimeOffset? paidAt, string? note, CancellationToken cancellationToken = default);

    Task<ValuationInvoice> UpdateAsync(string valuationInvoiceId, DateTimeOffset periodMonth, decimal amount, decimal? amountPaid = null, DateTimeOffset? issuedAt = null, DateTimeOffset? paidAt = null, string? note = null, CancellationToken cancellationToken = default);

    // Approval workflow: Raised → Submitted → Approved/Rejected; Raised/Rejected → Cancelled.
    // Issue accepts Approved — or Raised/Submitted for projects with no formal approval loop.
    Task<ValuationInvoice> SubmitAsync(string valuationInvoiceId, CancellationToken cancellationToken = default);
    Task<ValuationInvoice> ApproveAsync(string valuationInvoiceId, string? note = null, CancellationToken cancellationToken = default);
    Task<ValuationInvoice> RejectAsync(string valuationInvoiceId, string reason, CancellationToken cancellationToken = default);
    Task<ValuationInvoice> CancelAsync(string valuationInvoiceId, string? note = null, CancellationToken cancellationToken = default);

    /// <summary>Issue without raising in Xero; xeroInvoiceNumber records a hand-raised invoice's Xero number (2026-09-10).</summary>
    Task<ValuationInvoice> IssueAsync(string valuationInvoiceId, string? xeroInvoiceNumber = null, CancellationToken cancellationToken = default);

    /// <summary>The number of a sales invoice raised in Xero by hand, recorded on an invoice already
    /// Issued or Paid (2026-09-10) — the back-fill; nothing is written to Xero.</summary>
    Task<ValuationInvoice> RecordXeroNumberAsync(string valuationInvoiceId, string xeroInvoiceNumber, CancellationToken cancellationToken = default);

    /// <summary>The claim card's "Raise in Xero" (2026-09-09): what Xero would hold, then the raise
    /// itself — the AUTHORISED sales invoice, the certificate attached, the invoice issued. The dates
    /// are the user's for this call (2026-09-10); null means today / the certificate rule.</summary>
    Task<ValuationInvoiceXeroRaisePreview> PreviewXeroRaiseAsync(string valuationInvoiceId, DateTime? invoiceDate = null, DateTime? dueDate = null, CancellationToken cancellationToken = default);
    Task<ValuationInvoiceXeroRaiseOutcome> RaiseInXeroAsync(string valuationInvoiceId, DateTime? invoiceDate = null, DateTime? dueDate = null, CancellationToken cancellationToken = default);
    Task<ValuationInvoice> RecordPaymentAsync(string valuationInvoiceId, decimal amountPaid, CancellationToken cancellationToken = default);
    Task DeleteAsync(string valuationInvoiceId, CancellationToken cancellationToken = default);
}

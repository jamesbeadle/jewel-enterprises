using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ValuationInvoices;

// The sales invoice raised in Xero from the portal (2026-09-09, the accountant's ask: Cert 15 had
// to be raised, tracked and have its PDF attached entirely by hand). Until now the portal's Issue
// step only RECORDED that the client invoice had gone; Xero raised it. Now the same step can raise
// it: one AUTHORISED ACCREC invoice on the Xero contact MAPPED ON THE PROJECT (Project settings →
// Xero contact; the raise is blocked until it is set, and never matches by name or creates one —
// 2026-09-10, the accountant's ask, after a name match created a duplicate) for the invoice's cash
// amount, one line on the sales account, the project's Sites tracking on it, the payment
// certificate's PDF attached when the register holds one — and the portal invoice is issued in the
// same move, with Xero's id and number stamped on it. Both are planned by one rule (the preview a
// person confirms and the write that follows), and nothing is raised twice: an invoice that already
// carries a Xero id or number is refused.

/// <summary>
/// What the raise would do, read fresh from the portal and from Xero. InvoiceDate and DueDate are
/// the user's own dates for this call (2026-09-10): blank means today and the certificate's issue
/// date + the contract's final date for payment days (else Xero's sales default) — the preview
/// shows which applied.
/// </summary>
public sealed record PreviewValuationInvoiceXeroRaise(
    string ValuationInvoiceId,
    DateTime? InvoiceDate = null,
    DateTime? DueDate = null) : IQuery<ValuationInvoiceXeroRaisePreview>;

/// <summary>
/// The invoice as Xero will hold it. ContactName/XeroContactId are the project's mapped Xero
/// contact and ContactStatus says whether Xero still holds it — the raise never matches by name
/// and never creates a contact. TaxNote says where the VAT treatment comes from — the contact's
/// default sales tax type, their most recent sales invoice, or Xero's account default — because
/// it is never assumed. Reference is the portal's (VI-0005); XeroReference is what Xero's
/// Reference field will read ("Valuation 05") and Description the one line's text. Blockers is the list of reasons the raise would be refused; empty means it can go.
/// </summary>
public sealed record ValuationInvoiceXeroRaisePreview(
    string ValuationInvoiceId,
    string Reference,
    string XeroReference,
    string ContactName,
    string? XeroContactId,
    string ContactStatus,
    decimal Net,
    string Description,
    string AccountCode,
    string? SiteOption,
    DateTime Date,
    DateTime? DueDate,
    string DueDateNote,
    string TaxNote,
    string? CertificateFileName,
    string CertificateNote,
    IReadOnlyList<string> Blockers)
{
    public bool CanRaise => Blockers.Count == 0;
}

/// <summary>
/// Raises the AUTHORISED sales invoice in Xero exactly as the preview showed, stamps Xero's id and
/// number on the valuation invoice, attaches the certificate PDF (best effort — the invoice
/// stands without it and the outcome says so), then issues the valuation invoice (the existing
/// Issue move: certified to date moves). InvoiceDate/DueDate are the same per-call dates the
/// preview takes (blank = today / the certificate rule). RaisedBy is stamped server-side from the
/// signed-in user.
/// </summary>
public sealed record RaiseValuationInvoiceInXero(
    string ValuationInvoiceId,
    string? RaisedBy = null,
    DateTime? InvoiceDate = null,
    DateTime? DueDate = null)
    : ICommand<ValuationInvoiceXeroRaiseOutcome>;

public sealed record ValuationInvoiceXeroRaiseOutcome(
    string XeroInvoiceId,
    string XeroInvoiceNumber,
    decimal Net,
    decimal Tax,
    decimal Total,
    string TaxNote,
    bool CertificateAttached,
    string? AttachmentError,
    ValuationInvoice Invoice);

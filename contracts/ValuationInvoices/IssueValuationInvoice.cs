using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ValuationInvoices;

/// <summary>
/// Marks a valuation invoice's client invoice as sent (Approved -> Issued, or Raised -> Issued for
/// projects that skip the formal approval loop). From this point the amount counts toward
/// "Certified to date". The skip path freezes a report snapshot if none is linked yet, so even
/// two-click invoices keep a record of the report behind them.
/// XeroInvoiceNumber (2026-09-10) is for an invoice someone raised in Xero BY HAND: Xero's number
/// (INV-0227) is stamped on the valuation invoice with the time it was recorded, so the row reads
/// as raised and a second raise is refused. Nothing is written to Xero.
/// </summary>
public sealed record IssueValuationInvoice(string ValuationInvoiceId, string? XeroInvoiceNumber = null) : ICommand<ValuationInvoice>;

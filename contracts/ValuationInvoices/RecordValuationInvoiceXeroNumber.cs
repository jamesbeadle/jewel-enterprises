using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ValuationInvoices;

/// <summary>
/// Records the number of a sales invoice raised in Xero BY HAND against a valuation invoice that
/// is already Issued or Paid (2026-09-10, the accountant's ask: VI-0002 to VI-0005 were raised in
/// Xero before the portal could, and their Xero numbers were blank). The narrow back-fill:
/// stamps XeroInvoiceNumber (and XeroRaisedAt when it was blank), leaves XeroInvoiceId null — the
/// portal did not raise it — and writes nothing to Xero. Allowed at any status but Cancelled;
/// refused on an invoice the portal itself raised (it already carries Xero's id). A blank number
/// is refused: use it to record, never to clear. RecordedBy is stamped server-side.
/// </summary>
public sealed record RecordValuationInvoiceXeroNumber(
    string ValuationInvoiceId,
    string XeroInvoiceNumber,
    string? RecordedBy = null) : ICommand<ValuationInvoice>;

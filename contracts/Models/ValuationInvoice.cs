namespace Jewel.JPMS.Models;

// Lifecycle of a valuation invoice (formerly "cash call"). The happy path is one move per
// material stage, driven from the claim card: raised & sent in one click (Raised is a
// transient — a report snapshot is frozen at raise and the claim goes straight to
// Submitted, with the architect/client for approval) → Approved → Issued (client invoice
// sent — counts toward certified/invoiced to date) → Paid (client has paid — rolls into
// the project-level paid total).
// Submitted can come back Rejected, returning the invoice for amendment (→ Raised, now a
// draft state that also covers invoices added directly in the section) or cancellation.
// Raised/Submitted → Issued directly remains permitted for projects that skip the formal
// approval loop.
// Int values 0–2 predate the approval states and are persisted/seeded — never renumber.
public enum ValuationInvoiceStatus
{
    Raised = 0,
    Issued = 1,
    Paid = 2,
    Submitted = 3,
    Approved = 4,
    Rejected = 5,
    Cancelled = 6
}

// Everything that can happen to a valuation invoice, recorded as an audit trail so
// amendments are tracked on the same invoice (no versioning).
public enum ValuationInvoiceEventType
{
    Created = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    Amended = 4,
    Issued = 5,
    PaymentRecorded = 6,
    Cancelled = 7,
    ManualEntry = 8,
    RaisedInXero = 9   // the AUTHORISED sales invoice created in Xero from the portal (2026-09-09)
}

// A valuation invoice: the client invoice raised against the current valuation/CVR. Drawn from a
// valuation claim when one is linked. Issued/Paid invoices drive "Certified to date" on the
// valuation report; when paid, the amount rolls into the project-level total. Submitted/Approved
// invoices are pending — they never count toward certified until issued. Manual invoices are
// backdated historical entries created directly as Issued or Paid; they bypass the approval loop.
//
// Amount is the CASH the client is asked to pay. On a deposit project the invoice also carries
// DepositCredited — the cash-up-front deposit credit embedded in it (stamped from the claim's
// outstanding deduction at raise time) — so the GROSS certificate the invoice represents is
// Amount + DepositCredited. Certification runs gross; the deposit credit is a cash-side line.
public sealed record ValuationInvoice(
    string ValuationInvoiceId,
    string ProjectId,
    string? ValuationClaimId,
    int Number,
    string Reference,             // e.g. "VI-0001"
    DateTimeOffset PeriodMonth,   // the month this invoice covers
    decimal Amount,
    decimal AmountPaid,
    ValuationInvoiceStatus Status,
    DateTimeOffset RaisedAt,
    DateTimeOffset? IssuedAt = null,
    DateTimeOffset? PaidAt = null,
    DateTimeOffset? SubmittedAt = null,
    DateTimeOffset? ApprovedAt = null,
    DateTimeOffset? RejectedAt = null,
    DateTimeOffset? CancelledAt = null,
    string? RejectionReason = null,
    int AmendmentCount = 0,
    bool IsManual = false,
    string? ValuationReportSnapshotId = null,  // latest snapshot backing this invoice
    decimal DepositCredited = 0m,              // deposit credit embedded in Amount; gross certificate = Amount + DepositCredited
    // The AUTHORISED sales invoice this raised in Xero (2026-09-09, the accountant's ask): Xero's
    // InvoiceID, its number (INV-0123) and when. Null on invoices issued without a Xero raise —
    // one raised in Xero by hand, or issued before the portal could raise them.
    string? XeroInvoiceId = null,
    string? XeroInvoiceNumber = null,
    DateTimeOffset? XeroRaisedAt = null)
{
    public string DisplayNumber => Number > 0 ? $"VI-{Number:0000}" : "";

    public bool IsRaisedInXero => !string.IsNullOrWhiteSpace(XeroInvoiceId);

    // The gross certificate this invoice represents (works certified before the deposit credit).
    public decimal CertifiedAmount => Amount + DepositCredited;

    // Pending states: claimed from the client but not yet certifiable.
    public bool IsAwaitingApproval => Status is ValuationInvoiceStatus.Submitted or ValuationInvoiceStatus.Approved;

    // Editable states: amount/period may still change (manual invoices stay editable throughout).
    public bool IsEditable => IsManual || Status is ValuationInvoiceStatus.Raised or ValuationInvoiceStatus.Rejected;
}

// One entry in a valuation invoice's audit trail.
public sealed record ValuationInvoiceEvent(
    string ValuationInvoiceEventId,
    string ValuationInvoiceId,
    ValuationInvoiceEventType EventType,
    DateTimeOffset OccurredAt,
    string Note,                  // e.g. rejection reason, amendment summary
    decimal? AmountBefore = null, // populated for Amended / PaymentRecorded
    decimal? AmountAfter = null);

// Project-level roll-up of valuation invoices. Cancelled invoices are excluded from every figure.
public sealed record ProjectValuationInvoiceSummary(
    string ProjectId,
    decimal TotalRaised,             // sum of all live invoice amounts, any status except Cancelled
    decimal TotalInvoiced,           // sum of Issued + Paid invoice CASH amounts
    decimal TotalPaid,               // sum of amounts the client has paid
    decimal Outstanding,             // invoiced but not yet paid
    decimal TotalAwaitingApproval = 0m,  // sum of Submitted + Approved amounts — pending exposure
    decimal TotalDepositCredited = 0m)   // deposit credits embedded in Issued + Paid invoices
{
    // Gross certification to date — what feeds "Certified to date" on the valuation report.
    public decimal TotalCertified => TotalInvoiced + TotalDepositCredited;
}

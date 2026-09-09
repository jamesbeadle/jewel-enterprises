using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// A monthly valuation invoice (see Jewel.JPMS.Models.ValuationInvoice). When Paid, its AmountPaid is added to
// ProjectEntity.ValuationInvoicePaidTotal.
public sealed class ValuationInvoiceEntity
{
    [Key, MaxLength(64)] public string ValuationInvoiceId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(64)]      public string? ValuationClaimId { get; set; }
    public int Number { get; set; }
    [MaxLength(32)]      public string Reference { get; set; } = "";
    public DateTimeOffset PeriodMonth { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountPaid { get; set; }
    public int Status { get; set; }
    public DateTimeOffset RaisedAt { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    [MaxLength(1024)]    public string? RejectionReason { get; set; }
    public int AmendmentCount { get; set; }
    public bool IsManual { get; set; }
    [MaxLength(64)]      public string? ValuationReportSnapshotId { get; set; }
    // Cash-up-front deposit credit embedded in Amount (stamped from the claim's outstanding
    // deduction at raise time). The gross certificate = Amount + DepositCredited.
    public decimal DepositCredited { get; set; }
    // The AUTHORISED sales invoice raised in Xero from this row (2026-09-09): Xero's InvoiceID,
    // its number and when. Null when the invoice was issued without a Xero raise.
    [MaxLength(64)]      public string? XeroInvoiceId { get; set; }
    [MaxLength(64)]      public string? XeroInvoiceNumber { get; set; }
    public DateTimeOffset? XeroRaisedAt { get; set; }
}

// Audit trail: everything that has happened to a valuation invoice (creation, submission,
// approval/rejection, amendments with before/after amounts, issue, payment, cancellation,
// manual entry). Amendments are tracked on the same invoice — no versioning.
public sealed class ValuationInvoiceEventEntity
{
    [Key, MaxLength(64)] public string ValuationInvoiceEventId { get; set; } = "";
    [MaxLength(64)]      public string ValuationInvoiceId { get; set; } = "";
    public int EventType { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    [MaxLength(1024)]    public string Note { get; set; } = "";
    public decimal? AmountBefore { get; set; }
    public decimal? AmountAfter { get; set; }
}

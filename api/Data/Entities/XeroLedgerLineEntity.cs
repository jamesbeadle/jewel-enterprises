using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// One Xero purchase-invoice line stored in JPMS for cost allocation. Keyed on
/// "{XeroInvoiceId}:{XeroLineItemId}" (both stable Xero identifiers), so syncs
/// upsert deterministically: Xero facts are refreshed on every sync while the
/// allocation fields (status / project / cost centre) belong to JPMS and are
/// never overwritten by a sync. Net is pre-VAT (normalised for VAT-inclusive
/// invoices) and stored positive — Type says whether it adds (ACCPAY) or
/// subtracts (ACCPAYCREDIT) in spend views.
/// </summary>
public sealed class XeroLedgerLineEntity
{
    [Key, MaxLength(140)] public string XeroLedgerLineId { get; set; } = "";
    [MaxLength(64)]       public string XeroInvoiceId { get; set; } = "";
    [MaxLength(64)]       public string XeroLineItemId { get; set; } = "";
    [MaxLength(16)]       public string Type { get; set; } = "";
    [MaxLength(64)]       public string? InvoiceNumber { get; set; }
    [MaxLength(256)]      public string? Reference { get; set; }
    [MaxLength(256)]      public string? ContactName { get; set; }
    public DateTime? Date { get; set; }
    [MaxLength(32)]       public string InvoiceStatus { get; set; } = "";
    [MaxLength(1024)]     public string? Description { get; set; }
    public decimal Net { get; set; }
    public decimal Tax { get; set; }

    // The bill's payment state. Xero holds these per INVOICE, so like InvoiceStatus above they
    // are repeated on every stored line of the bill: InvoiceTotal is the gross as Xero states
    // it, AmountDue what is still outstanding on it. Together they say how much of the bill has
    // been settled, which is what turns a work-order link into a paid position (XeroPaymentMaths).
    // Both read 0 on rows synced before the AddXeroLinePaymentState migration; InvoiceStatus is
    // the fallback for those until the next sync fills them.
    public decimal InvoiceTotal { get; set; }
    public decimal AmountDue { get; set; }
    [MaxLength(32)]       public string? AccountCode { get; set; }
    [MaxLength(256)]      public string? AccountName { get; set; }

    // The line's tracking options as Xero holds them (site + Xero's own cost code).
    [MaxLength(128)]      public string? XeroSite { get; set; }
    [MaxLength(128)]      public string? XeroCostCode { get; set; }

    // Whether Xero holds attachments for this line's invoice (the supplier's document,
    // published by Dext) — arms the invoice viewer on the allocation page.
    public bool HasAttachments { get; set; }

    // Allocation — owned by JPMS, survives syncs.
    // Status: 0 Unallocated, 1 Allocated (project + cost centre), 2 Ignored,
    // 3 Bucketed (cost of sales with no identifiable project — Parking, Fuel, ...).
    // An allocated line carries EITHER CostCenterCode (whole line to one centre)
    // OR rows in XeroCostSplits (value shared across centres) — never both.
    public int AllocationStatus { get; set; }
    [MaxLength(64)]       public string? ProjectId { get; set; }
    [MaxLength(32)]       public string? CostCenterCode { get; set; }
    [MaxLength(64)]       public string? Bucket { get; set; }
    [MaxLength(256)]      public string? AllocatedBy { get; set; }
    public DateTimeOffset? AllocatedAtUtc { get; set; }
    [MaxLength(512)]      public string? Note { get; set; }

    // The work order(s) this purchase line pays against live in XeroLineWorkOrderLinks —
    // one row per order with its share of the net, so one bill can pay several orders.
    // Owned by JPMS, survives syncs. Unlinked value counts as non-work-order cost of sales.

    // Xero write-back (tracking + DRAFT → AUTHORISED approval) — per invoice,
    // stamped on every stored line of the invoice when attempted.
    // 0 None (not attempted / not needed), 1 Approved, 2 Failed (see error).
    // The write-back story (2026-09-08, the accountant's ask): WriteBackStatus is where it stands
    // NOW, WriteBackAtUtc when that was decided. WriteBackError is the LAST failure's text and
    // WriteBackFailedAtUtc when it happened — both kept through a later success, so a bill that
    // failed at 09:12 and approved on retry at 10:40 still says so on the line instead of the
    // failure vanishing. Only a fresh failure rewrites them.
    public int WriteBackStatus { get; set; }
    [MaxLength(1024)]     public string? WriteBackError { get; set; }
    public DateTimeOffset? WriteBackAtUtc { get; set; }
    public DateTimeOffset? WriteBackFailedAtUtc { get; set; }

    public DateTimeOffset FirstSeenAtUtc { get; set; }
    public DateTimeOffset LastSyncedAtUtc { get; set; }
}

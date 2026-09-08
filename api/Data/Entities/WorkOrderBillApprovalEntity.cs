using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// One approval of a supplier bill as a Work Order bill (2026-09-08): the record of who pressed
/// Approve on the allocation page's Work Order bills tab, against which order, by which matching
/// rule, and — when reversed — who undid it and when. The row is the undo's handle (the lines it
/// returns, the links it removes) and the audit's answer to "which rule matched". Kept after an
/// undo so the history reads "approved, then undone"; a re-approval adds a new row. MatchRule
/// maps to <c>WorkOrderMatchRule</c>; MatchDetail is the sentence the card showed.
/// </summary>
public sealed class WorkOrderBillApprovalEntity
{
    [Key, MaxLength(64)] public string WorkOrderBillApprovalId { get; set; } = "";
    [MaxLength(64)]      public string XeroInvoiceId { get; set; } = "";
    [MaxLength(64)]      public string WorkOrderId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public int MatchRule { get; set; }
    [MaxLength(512)]     public string MatchDetail { get; set; } = "";
    public decimal BillNet { get; set; }
    [MaxLength(256)]     public string ApprovedByEmail { get; set; } = "";
    public DateTimeOffset ApprovedAtUtc { get; set; }
    [MaxLength(256)]     public string? UndoneByEmail { get; set; }
    public DateTimeOffset? UndoneAtUtc { get; set; }

    public bool IsStanding => UndoneAtUtc is null;
}

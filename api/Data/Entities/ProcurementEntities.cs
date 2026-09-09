using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class BidPackageEntity
{
    [Key, MaxLength(64)] public string BidPackageId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    [MaxLength(64)]      public string Trade { get; set; } = "";
    public int Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    [MaxLength(256)]     public string OwnerEmail { get; set; } = "";

    // LEGACY parent Variation Order (pre-2026-08-12, when packages could be created under a
    // variation). Never set on new packages — bid packages are standalone records; a line's
    // Coverage is how tendered scope maps to a variation. Kept for old rows only.
    // (Column keeps its historic VariationOrderQuoteId spelling — see VariationEntities.cs.)
    [MaxLength(64)]
    [System.ComponentModel.DataAnnotations.Schema.Column("VariationOrderQuoteId")]
    public string? VariationOrderId { get; set; }

    // Sequential, human-readable package number (rendered BPI-0001). The BPI reference is the stem
    // tagged on the package's emails so RFT responses group under it in the Bid Package Invites section.
    public int Number { get; set; }

    // Materials matter to this scope: the tender invite asks each subcontractor to state whether
    // they will supply their own materials or price labour-only.
    public bool MaterialsApplicable { get; set; }

    // When the package was closed without a winner (BidPackageStatus.Closed). Null unless closed;
    // cleared on reopen. No reason is stored — closing without incident needs no paperwork.
    public DateTimeOffset? ClosedAt { get; set; }

    // The specification summary printed in the tender documents — the short "what this package
    // covers, to what standard" bullets at the top of the pricing schedule workbook. Optional.
    [MaxLength(4000)]    public string SpecificationSummary { get; set; } = "";

    // ---- The invite composer's persisted draft (2026-08-16) — the half-written invite email,
    //      saved on the package so anyone on the team can pick it up later. Cleared on send. ----
    [MaxLength(512)]     public string? InviteDraftSubject { get; set; }
    public string? InviteDraftBody { get; set; }
    [MaxLength(2000)]    public string? InviteDraftTo { get; set; }
    [MaxLength(2000)]    public string? InviteDraftCc { get; set; }
    public string? InviteDraftBcc { get; set; }
    public DateTimeOffset? InviteDraftSavedAt { get; set; }

    // Canonical reference this package's emails are tagged with ("JPMS/BPI-0001"). Falls back to an
    // id-derived stem for legacy rows that predate numbering. Computed, not stored.
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Reference => Number > 0
        ? $"BPI-{Number:0000}"
        : "BPI-" + (BidPackageId.Length >= 8 ? BidPackageId[..8] : BidPackageId).ToUpperInvariant();
}

// A subcontractor invited to tender for a bid package. One row per (package, subcontractor).
public sealed class BidPackageRecipientEntity
{
    [Key, MaxLength(64)] public string RecipientId { get; set; } = "";
    [MaxLength(64)]      public string BidPackageId { get; set; } = "";
    [MaxLength(64)]      public string SubcontractorId { get; set; } = "";
    public int Status { get; set; }
    public DateTimeOffset InvitedAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
}

// A scope line on a bid package, grouped by Trade/speciality. Pricing is captured per response.
public sealed class BidPackageLineItemEntity
{
    [Key, MaxLength(64)] public string LineItemId { get; set; } = "";
    [MaxLength(64)]      public string BidPackageId { get; set; } = "";
    [MaxLength(512)]     public string Description { get; set; } = "";
    [MaxLength(32)]      public string Unit { get; set; } = "";
    public decimal Quantity { get; set; }
    [MaxLength(64)]      public string Trade { get; set; } = "";

    // Cost centre code in the current master list (CostCenterEntity.Code) — the cost-centre home the
    // line's committed value lands on when the package is awarded. Required on every line saved since
    // the rule landed; empty only on legacy rows that predate it.
    [MaxLength(32)]      public string CostCode { get; set; } = "";
    public int SortOrder { get; set; }

    // Commercial home of this line — 0 Unassigned, 1 ContractLine, 2 Variation (BidPackageLineCoverage).
    // Exactly one of BoqLineItemId / VariationOrderId is set to match, enforced by the handler.
    public int Coverage { get; set; }
    [MaxLength(64)]      public string? BoqLineItemId { get; set; }
    [MaxLength(64)]
    [System.ComponentModel.DataAnnotations.Schema.Column("VariationOrderQuoteId")]
    public string? VariationOrderId { get; set; }
}

public sealed class QuoteEntity
{
    [Key, MaxLength(64)] public string QuoteId { get; set; } = "";
    [MaxLength(64)]      public string BidPackageId { get; set; } = "";
    [MaxLength(64)]      public string SubcontractorId { get; set; } = "";
    public decimal Value { get; set; }
    [MaxLength(1024)]    public string Notes { get; set; } = "";
    public DateTimeOffset ReceivedAt { get; set; }
    public bool IsDeclined { get; set; }
}

// A file kept on a bid package as part of its tender documents — a specification extract, a
// schedule of finishes, anything a tenderer needs that isn't a drawing in the register. Supplier-
// facing: PrepareBidPackageInviteDraft attaches these to the invite email alongside the linked
// drawings. Bytes live in the bid-package-attachments blob container (BlobRef); the row is the
// register entry the Documents section reads.
public sealed class BidPackageAttachmentEntity
{
    [Key, MaxLength(64)] public string BidPackageAttachmentId { get; set; } = "";
    [MaxLength(64)]      public string BidPackageId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string FileName { get; set; } = "";
    [MaxLength(128)]     public string ContentType { get; set; } = "";
    public long FileSizeBytes { get; set; }
    [MaxLength(1024)]    public string BlobRef { get; set; } = "";

    // Maps to BidPackageAttachmentSource (0 = uploaded from a computer; 1 = reserved for
    // copy-off-a-triaged-email, not produced yet).
    public int Source { get; set; }
    public DateTimeOffset AddedAt { get; set; }
    [MaxLength(256)]     public string AddedByEmail { get; set; } = "";
}

// A project drawing linked to a bid package — the tender documents. One row per (package, drawing);
// the drawing itself (and its revisions/files) lives in the project's Drawings section, this is just
// the association the invite email attaches from.
public sealed class BidPackageDrawingEntity
{
    [Key, MaxLength(64)] public string BidPackageDrawingId { get; set; } = "";
    [MaxLength(64)]      public string BidPackageId { get; set; } = "";
    [MaxLength(64)]      public string DrawingId { get; set; } = "";
    public DateTimeOffset LinkedAt { get; set; }
}

// A priced line on a subcontractor's quote — their rate against one of the package's line items.
public sealed class QuoteLineItemEntity
{
    [Key, MaxLength(64)] public string QuoteLineItemId { get; set; } = "";
    [MaxLength(64)]      public string QuoteId { get; set; } = "";
    // The package line this quoted line prices; null when the subbie quoted outside the package's
    // scope (lump-sum extra, attendance) — such lines still show in the comparison, unaligned.
    [MaxLength(64)]      public string? BidPackageLineItemId { get; set; }
    [MaxLength(512)]     public string Description { get; set; } = "";
    [MaxLength(32)]      public string Unit { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Total { get; set; }
}

// The purchase-order record raised against a supplier — the business calls these work orders.
// Created by awarding a bid package, raised directly, or seeded from Buildertrend. The money
// detail lives on WorkOrderLines, each carrying its own cost code (one order routinely spans
// several cost centres — e.g. plastering + render + screed on a single drywall order); Value is
// the order total and should equal the sum of the lines' totals.
public sealed class WorkOrderEntity
{
    [Key, MaxLength(64)] public string WorkOrderId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";

    // The tender this order was awarded from. Null for orders raised directly or seeded from
    // Buildertrend, which predate bid packages.
    [MaxLength(64)]      public string? BidPackageId { get; set; }
    [MaxLength(64)]      public string SubcontractorId { get; set; } = "";
    public decimal Value { get; set; }
    [MaxLength(4000)]    public string Scope { get; set; } = "";
    public DateTimeOffset AwardedAt { get; set; }
    [MaxLength(256)]     public string AwardedByEmail { get; set; } = "";

    // Sequential, human-readable order number (rendered WO-0001). Seeded orders keep their
    // Buildertrend PO number so paperwork cross-references hold (PO-32 -> WO-0032).
    public int Number { get; set; }
    [MaxLength(256)]     public string Title { get; set; } = "";
    public int Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ScheduledCompletion { get; set; }

    // External id of the record this order was seeded from (e.g. the Buildertrend PO id), so
    // re-running the seed is idempotent. Null for orders raised in JPMS.
    [MaxLength(64)]      public string? SourceReference { get; set; }

    // Set when this order was issued to instruct an approved variation order. Variations always
    // produce a NEW work order — existing orders are never uplifted (subcontractor-crm-scope §6).
    [MaxLength(64)]      public string? VariationOrderId { get; set; }

    // Programme information printed on the purchase order. ScheduledCompletion above is the
    // target completion date; these add the programme start and free-text notes (e.g. phasing).
    // All optional — the PO's Programme section renders only when at least one is set.
    public DateTimeOffset? ProgrammeStart { get; set; }
    [MaxLength(2000)]    public string ProgrammeNotes { get; set; } = "";

    // Electronic acceptance from the subcontractor portal: stamped once when the supplier's
    // signed-in contact clicks Accept (name/email from their login, never typed by hand).
    public DateTimeOffset? AcceptedAt { get; set; }
    [MaxLength(256)]     public string AcceptedByEmail { get; set; } = "";
    [MaxLength(256)]     public string AcceptedByName { get; set; } = "";

    // Deposit the subcontractor requires on this order, printed at the foot of the purchase
    // order. Recorded as a percentage of the order value only — never a £ figure. Percent is
    // null unless DepositRequired; captured on manually raised orders via the Add/Edit form.
    public bool DepositRequired { get; set; }
    public decimal? DepositPercent { get; set; }

    // Human reference, falling back to an id-derived stem for legacy rows that predate numbering.
    // Computed, not stored — mirrors BidPackageEntity.Reference.
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Reference => Number > 0
        ? $"WO-{Number:0000}"
        : "WO-" + (WorkOrderId.Length >= 8 ? WorkOrderId[..8] : WorkOrderId).ToUpperInvariant();
}

// A priced line on a work order. Cost centre totals aggregate lines, not orders, because each
// line carries its own cost code. PaidToDate is the amount invoiced-and-paid against the line so
// far (carried over from Buildertrend on seeded orders).
public sealed class WorkOrderLineEntity
{
    [Key, MaxLength(64)] public string WorkOrderLineId { get; set; } = "";
    [MaxLength(64)]      public string WorkOrderId { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    [MaxLength(1024)]    public string Description { get; set; } = "";

    // As printed on the order, e.g. Subcontractor / Material / Labour.
    [MaxLength(64)]      public string CostType { get; set; } = "";

    // Cost centre code in the current master list (CostCenterEntity.Code).
    [MaxLength(32)]      public string CostCode { get; set; } = "";

    // The source system's cost code as printed on a seeded order (e.g. "00006-12 Plastering"),
    // kept so the mapping to CostCode stays traceable. Empty for lines raised in JPMS.
    [MaxLength(128)]     public string LegacyCostCode { get; set; } = "";
    public decimal Quantity { get; set; }
    [MaxLength(32)]      public string Unit { get; set; } = "";
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
    public decimal PaidToDate { get; set; }
    public int SortOrder { get; set; }
}

// A file kept on a work order for record keeping -- the quote the order was raised against, a
// signed copy, a photo of the scope. Never sent to the supplier: the purchase-order email and the
// printed PO ignore these entirely. Bytes live in the work-order-attachments blob container
// (BlobRef); the row is the register entry the Work Orders views read.
public sealed class WorkOrderAttachmentEntity
{
    [Key, MaxLength(64)] public string WorkOrderAttachmentId { get; set; } = "";
    [MaxLength(64)]      public string WorkOrderId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string FileName { get; set; } = "";
    [MaxLength(128)]     public string ContentType { get; set; } = "";
    public long FileSizeBytes { get; set; }
    [MaxLength(1024)]    public string BlobRef { get; set; } = "";

    // Maps to WorkOrderAttachmentSource (0 = uploaded from a computer, 1 = copied off the
    // triaged email the order was raised from).
    public int Source { get; set; }
    public DateTimeOffset AddedAt { get; set; }
    [MaxLength(256)]     public string AddedByEmail { get; set; } = "";
}

public sealed class RequestEntity
{
    [Key, MaxLength(64)] public string RequestId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public int Kind { get; set; }
    [MaxLength(64)]      public string Reference { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    [MaxLength(2048)]    public string Description { get; set; } = "";
    public int Status { get; set; }
    public decimal? Value { get; set; }
    [MaxLength(256)]     public string RaisedByEmail { get; set; } = "";
    public DateTimeOffset RaisedAt { get; set; }

    // When the official document (RFI / NOD / EOT) was issued to the correspondent. Distinct from
    // RaisedAt (when the request was raised in the register): set and updated by the user from the
    // detail view — never stamped automatically. Null until the document has actually been issued.
    public DateTimeOffset? IssuedAt { get; set; }

    public DateTimeOffset? RespondedAt { get; set; }
    [MaxLength(2048)]    public string? ResponseText { get; set; }
    [MaxLength(256)]     public string? RespondedByEmail { get; set; }

    // When the request was closed. Chosen by the user at close time (defaults to today, may be a
    // prior date when the closure is recorded after the fact); never in the future. Cleared on reopen.
    public DateTimeOffset? ClosedAt { get; set; }

    public bool ImpliesVariation { get; set; }

    // Critical Path tag: marks an RFI as programme-related — its answer gates work on the
    // programme's critical path. Surfaces the RFI in the project Programme tab's
    // "Critical Path RFIs" view. User-set from the RFI detail page; defaults to off.
    public bool CriticalPath { get; set; }

    // "No" was clicked on the RFI detail page's two-week critical-path nudge. Recorded on the
    // request (not per user, not per session) so the banner never re-asks anyone about this RFI.
    // Tagging the RFI critical path later is unaffected — the toggle and edit modal still work.
    public bool CriticalPathNudgeDismissed { get; set; }

    [MaxLength(256)]     public string? DrawingRef { get; set; }
    public DateTimeOffset? ResponseDue { get; set; }
    [MaxLength(512)]     public string? RelatedDrawingSpec { get; set; }
    [MaxLength(4000)]    public string? InternalNotes { get; set; }
    [MaxLength(4000)]    public string? ClientNotes { get; set; }

    // ---- Official document (RFI sheet) body -----------------------------------------------------
    // The structured sections of the issued RFI document, alongside the itemised queries held in
    // RequestItems. All optional: a simple request needs none of them and renders as before.

    // "Basis of queries" — the emails, drawings and site observations the queries arise from.
    [MaxLength(4000)]    public string? BasisOfQueries { get; set; }

    // "Response / action required" — what written confirmation or instruction is being asked for.
    [MaxLength(4000)]    public string? ResponseActionRequired { get; set; }

    // Impact if the response is not received by the required-by date (programme / cost consequence).
    [MaxLength(2048)]    public string? ImpactIfLate { get; set; }

    // Sequential, human-readable request number (rendered as REQ-0001). Used as the name of the
    // request's Outlook folder in the projects@ mailbox so triaged emails can be grouped per request.
    public int Number { get; set; }

    // True once an RFI has spawned an RFQ. Gates creation of a Variation Order Quote (VOQ).
    public bool HasRfq { get; set; }

    // The party this request is corresponded with: a client account directly (PartyKind 0) or an
    // architect acting on a client's behalf (PartyKind 1, with OnBehalfOfClientId optionally
    // recording that client). The recipient email on RFI promotion resolves from this party first,
    // falling back to the project's party, then the project's Architect contact. PartyId is null
    // until the request is linked to a party.
    public int PartyKind { get; set; }
    [MaxLength(64)]     public string? PartyId { get; set; }
    [MaxLength(64)]     public string? OnBehalfOfClientId { get; set; }

    // Graph id of this request's mailbox folder, set the first time an email is filed against it.
    // Cached so subsequent emails for the same request reuse the folder rather than recreating it.
    [MaxLength(450)]     public string? MailboxFolderId { get; set; }

    // EOT only: the Notice of Delay request this EOT arises from (JCT ICD 2024 cl. 2.19 -> 2.20).
    // Optional — an EOT can stand alone; never set for other kinds.
    [MaxLength(64)]      public string? RelatedNodRequestId { get; set; }

    // EOT only: the extension sought and, once the architect answers, awarded — calendar days.
    // The Programme tab draws the EOT from completion by these; null until entered.
    public int? EotDaysClaimed { get; set; }
    public int? EotDaysGranted { get; set; }

    // Set when this (General) request was merged into another: the survivor's id and when it
    // happened. A merged request is closed at the same time and kept purely as the audit trail —
    // its conversation, items and emails all live on the survivor from then on.
    [MaxLength(64)]      public string? MergedIntoRequestId { get; set; }
    public DateTimeOffset? MergedAt { get; set; }

    // The unqualified reference stem for this record's mailbox tag. Prefers the human reference;
    // falls back to REQ-NNNN when blank so a stem is always well-formed. Computed, not stored.
    // NB: the actual mailbox tag is project-qualified (references are only unique per project, tags
    // share one flat category space) — see RequestTags, which turns this stem into e.g.
    // "JBB-2026-001-RFI-001" -> category "JPMS/JBB-2026-001-RFI-001".
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string TagReference =>
        string.IsNullOrWhiteSpace(Reference) ? $"REQ-{Number:0000}" : Reference.Trim();
}

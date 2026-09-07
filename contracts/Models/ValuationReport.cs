using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Models;

// Where a priced line sits in the bill. Mirrors the three blocks of the By France
// workbook (Contract Sum, PC Sums, Contingency) plus the variations register.
public enum ValuationElementType
{
    ContractWorks = 0,
    PcSum = 1,
    Contingency = 2,
    Variation = 3
}

// The nature of a single priced row.
public enum ValuationLineType
{
    Priced = 0,           // ordinary measured work
    ProvisionalSum = 1,   // PS / provisional allowance
    Omit = 2,             // negative line that removes scoped work (often a PS being replaced)
    Declined = 3,         // recorded but not priced into the total
    Tbc = 4               // to be confirmed; not priced into the total
}

// Claim lifecycle: a Draft is editable; Preapproved means "we are claiming this"
// (amounts locked, awaiting the client); Confirmed means the client has paid and the
// per-row claimed amounts are final, advancing CertifiedToDate for the next claim.
public enum ValuationClaimStatus
{
    Draft = 0,
    Preapproved = 1,
    Confirmed = 2
}

public static class ValuationClaimStatusExtensions
{
    // The one shared status wording (same arrangement as VariationOrderStatusExtensions): the
    // reader's word for a Preapproved claim is "Issued" — it has been locked and put to the
    // client for a decision (accountant 2026-08-26). The enum member keeps its name: it is a
    // persisted identifier (stored as an int, threaded through API routes), never UI copy.
    public static string DisplayName(this ValuationClaimStatus status) => status switch
    {
        ValuationClaimStatus.Draft => "Draft",
        ValuationClaimStatus.Preapproved => "Issued",
        ValuationClaimStatus.Confirmed => "Confirmed",
        _ => status.ToString()
    };
}

// One priced row of the bill — a contract/PC/contingency line, or a variation line.
// Entered manually through the UI; no BoQ seeding.
public sealed record ValuationLineItem(
    string ValuationLineItemId,
    string ProjectId,
    ValuationElementType ElementType,
    string SectionCode,        // works/PC, e.g. "A10"
    string SectionName,        // e.g. "Preliminaries"
    string VariationRef,       // variations, e.g. "V18"
    string VariationTitle,
    ValuationLineType LineType,
    string CostCode,           // e.g. "0001"
    string Description,
    string Unit,
    decimal Quantity,
    decimal Rate,
    decimal LineAmount,        // qty x rate; negative for omits
    string Comments,
    int DisplayOrder,
    // The client's schedule-of-works item number for THIS line ("1.03") — the ref the client
    // reconciles against, line by line. Beats the per-cost-centre ClientCostReferences map at
    // snapshot capture; empty falls back to the map. Trailing default keeps the positional
    // constructor stable for older callers.
    string ClientReference = "") : IVariationBillLine
{
    // Declined / TBC lines are recorded but never priced into any total.
    public bool CountsTowardTotals => LineType is not (ValuationLineType.Declined or ValuationLineType.Tbc);
}

// One valuation period — the "Claim n" funds-request event — with frozen totals.
public sealed record ValuationClaim(
    string ValuationClaimId,
    string ProjectId,
    int ClaimNumber,
    DateTimeOffset ClaimDate,
    ValuationClaimStatus Status,
    decimal RetentionPercent,
    decimal RetentionReleasePercent,
    DateTimeOffset? PreapprovedAt,
    DateTimeOffset? ConfirmedAt,
    // Totals frozen when the claim is Confirmed:
    decimal ContractSum,
    decimal NetVariations,
    decimal RevisedContractSum,
    decimal TotalWorksComplete,
    decimal RetentionHeld,
    decimal RetentionReleased,
    decimal CertifiedToDate,
    decimal PaymentDueExVat,
    // Free-text period name (e.g. "June 2026"); renameable at any status. Empty for
    // claims from before names existed — display falls back to "Claim n".
    string Name = "",
    // Cash-up-front deposit: the % and opening balance stamped from the project's terms
    // when the claim starts (kept live on Drafts when terms change). DepositReleased is
    // the amount actually DEDUCTED from this claim's payment due — the deposit release
    // earned to date less the opening balance settled before tracking — frozen with the
    // other totals when the claim locks. Trailing defaults keep the positional
    // constructor stable for pre-deposit callers.
    decimal DepositPercent = 0m,
    decimal DepositReleased = 0m,
    decimal DepositReleasedOpening = 0m)
{
    // "June 2026" when named, otherwise "Claim 3" — one rule for every claim label.
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? $"Claim {ClaimNumber}" : Name;
}

// Per claim, per line item: the cumulative % complete entered and the resulting amounts.
public sealed record ClaimLine(
    string ClaimLineId,
    string ValuationClaimId,
    string ValuationLineItemId,
    decimal PercentComplete,    // cumulative % entered this claim
    decimal CumulativeClaimed,  // PercentComplete% x LineAmount
    decimal PeriodIncrement);   // CumulativeClaimed - the line's cumulative on the claim immediately
                                // before (whatever its status); the API's ClaimPeriodBaseline rule

// An immutable, line-level copy of the valuation report frozen at a moment in time — the report
// behind a valuation invoice, captured when the invoice is raised (or a period-end record when
// taken on demand). Snapshots are the only client-facing form of the report; the live report tab
// is internal. Unlike a claim (whose Preapproved totals are re-frozen when certified moves), a
// snapshot never changes after capture; amending an invoice flags its snapshot superseded and
// the next submit/issue freezes a NEW one.
public sealed record ValuationReportSnapshot(
    string ValuationReportSnapshotId,
    string ProjectId,
    string? ValuationInvoiceId,   // the invoice this submission backs; null for on-demand snapshots
    string? ValuationClaimId,     // the claim the figures came from, if one was open
    string Label,                 // e.g. "VI-0007 submission" / "June 2026 period end"
    DateTimeOffset TakenAt,
    bool IsSuperseded,            // a later snapshot exists for the same invoice
    // Frozen summary footer:
    decimal ContractSum,
    decimal NetVariations,
    decimal RevisedContractSum,
    decimal TotalWorksComplete,
    decimal RetentionPercent,
    decimal RetentionHeld,
    decimal RetentionReleasePercent,
    decimal RetentionReleased,
    decimal CertifiedToDate,
    decimal PaymentDueExVat,
    // Cash-up-front deposit as frozen at capture: the claim's deposit % and the cumulative
    // amount released back to the client. Trailing defaults for pre-deposit callers.
    decimal DepositPercent = 0m,
    decimal DepositReleased = 0m,
    // Per-project sequential number minted at capture — the stem of the snapshot's mailbox tag
    // ("JPMS/VRS-{projectRef}-{Number}"), which is how a triaged email is associated with the
    // snapshot. Trailing default keeps the positional constructor stable for older callers.
    int Number = 0);

// One frozen row of a snapshot: values copied (not referenced) from the live line item and its
// claim entry at capture time, so later edits/deletions of live data never disturb the snapshot.
public sealed record ValuationReportSnapshotLine(
    string ValuationReportSnapshotLineId,
    string ValuationReportSnapshotId,
    string SourceValuationLineItemId,  // provenance only — not a live FK
    ValuationElementType ElementType,
    string SectionCode,
    string SectionName,
    string VariationRef,
    string VariationTitle,
    ValuationLineType LineType,
    string CostCode,
    string Description,
    string Unit,
    decimal Quantity,
    decimal Rate,
    decimal LineAmount,
    decimal PercentComplete,
    decimal CumulativeClaimed,
    decimal PeriodIncrement,
    string Comments,
    int DisplayOrder,
    // The client's schedule-of-works reference for the line's cost centre, frozen from the
    // project's ClientCostReference map at capture — so a later remap never rewrites what the
    // client was sent. Trailing default keeps the positional constructor stable for older callers.
    string ClientReference = "") : IVariationBillLine
{
    public bool CountsTowardTotals => LineType is not (ValuationLineType.Declined or ValuationLineType.Tbc);
}

// A snapshot with its lines — the payload for the read-only snapshot viewer.
public sealed record ValuationReportSnapshotDetail(
    ValuationReportSnapshot Snapshot,
    IReadOnlyList<ValuationReportSnapshotLine> Lines);

/// <summary>
/// The outcome of drafting a valuation-report email in the shared mailbox: which snapshot the
/// attached PDF froze, who the draft is addressed to (the project's Client and Architect
/// contacts), and where to open it. <see cref="WebLink"/> opens the draft in Outlook on the web
/// when Graph returns one (it usually does); null otherwise — the draft is still in the mailbox's
/// Drafts folder. Mirrors <see cref="SubcontractorStatementEmailDraft"/>.
/// </summary>
public sealed record ValuationReportSnapshotEmailDraft(
    string ValuationReportSnapshotId,
    string Label,
    string Subject,
    IReadOnlyList<string> RecipientEmails,
    string? WebLink,
    // The staged draft's mailbox message id — the handle for withdrawing the draft
    // (DeleteMailboxDraft) if it was staged in error; null only on legacy payloads.
    string? DraftMessageId = null);

using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero;

// Server-side shapes for the Xero write-back: confirming an allocated draft
// bill's Sites / Cost Code tracking back onto the Xero invoice and approving
// it (DRAFT → AUTHORISED). Kept out of the shared contracts project — the
// front end only ever sees the outcome stamped on the ledger lines.

/// <summary>
/// Everything the Xero client needs to confirm and approve one invoice.
/// One instruction per stored ledger line; lines of the invoice that were
/// never queued (non-cost-of-sales) pass through untouched.
/// </summary>
public sealed record XeroApprovalRequest(
    string InvoiceId,
    bool IsCreditNote,
    IReadOnlyList<XeroApprovalLineInstruction> Lines,
    // A Work Order bill re-approved after an undo (2026-09-08) is already AUTHORISED in Xero —
    // Xero never un-approves — so its tracking must be rewritten on an approved bill. Set, an
    // approved bill with nothing paid or credited takes the same line rewrite (status kept);
    // unset, approved bills are skipped as before.
    bool RecodeApproved = false);

/// <summary>
/// The allocation to stamp on one Xero line item. A single share covers the
/// whole line (tracking set in place); multiple shares replace the Xero line
/// with one line per share — a share carries its own site (project) as well as
/// its cost code, so a split can span projects — amounts pro-rated so the
/// invoice total is unchanged to the penny.
/// </summary>
public sealed record XeroApprovalLineInstruction(
    string LineItemId,
    IReadOnlyList<XeroApprovalShare> Shares);

/// <summary>One share of a line: which Sites option and Cost Code option to stamp, and its net weight.</summary>
public sealed record XeroApprovalShare(string SiteOption, string CostCenterCode, decimal Net);

/// <summary>
/// Everything the Xero client needs to stamp Sites tracking onto specific line
/// items of one invoice WITHOUT approving it — the SetProject half-step, taken
/// when a queued line's project is known before its cost centre. Only the named
/// line items are touched; each keeps whatever other tracking it already has.
/// </summary>
public sealed record XeroSiteTrackingRequest(
    string InvoiceId,
    bool IsCreditNote,
    IReadOnlyList<XeroSiteTrackingLine> Lines);

/// <summary>
/// Which Sites option to stamp on one Xero line item. A null option means
/// remove the line's Sites tracking (unsetting an accidentally set project).
/// </summary>
public sealed record XeroSiteTrackingLine(string LineItemId, string? SiteOption);

/// <summary>
/// What happened. AlreadyApproved is a success without any write — the
/// invoice was approved in Xero outside JPMS between allocation and now.
/// FreshStatus is the invoice status as Xero reported it during the attempt.
/// </summary>
/// <summary>
/// One line of a settlement-schedule coding (docs/Labour-Overview-Forecast-and-Xero-Mapping-Scope.md
/// §6a): net amount, the account it lands on, and the Sites / Cost Code tracking options to stamp.
/// </summary>
public sealed record XeroScheduleLine(
    string Description,
    decimal Net,
    string AccountCode,
    string SiteOption,
    string CostCodeOption);

/// <summary>
/// Recode a bill's entire line list to a settlement schedule (2026-09-03: DRAFT, SUBMITTED or
/// AUTHORISED — an authorised bill with nothing paid or credited against it is the cover
/// route's NORMAL state, and Xero permits editing it). The bill's status, LineAmountTypes,
/// tax type and totals are preserved: the schedule supplies the SPLIT (weights, accounts,
/// tracking, descriptions); the bill's own SubTotal / TotalTax / Total are pro-rated across it
/// penny-safe, so the recode never moves the total or the VAT. Refused for PAID / VOIDED /
/// DELETED bills and for anything with a payment or credit note applied.
/// </summary>
public sealed record XeroBillCodingRequest(
    string InvoiceId,
    IReadOnlyList<XeroScheduleLine> Lines);

/// <summary>One line as Xero holds it after a recode — the fresh LineItemID is what the run
/// re-points the timesheet cover onto.</summary>
public sealed record XeroRecodedLine(
    string LineItemId,
    string Description,
    decimal LineAmount,
    decimal TaxAmount,
    string AccountCode,
    string? SiteOption,
    string? CostCodeOption);

/// <summary>What a recode did: the bill's status (unchanged), the VAT treatment it kept, its
/// totals as Xero reports them after the write, and the new lines with their ids.</summary>
public sealed record XeroBillRecodeResult(
    bool Succeeded,
    string? Error,
    string Status,
    string LineAmountTypes,
    string? TaxType,
    decimal SubTotal,
    decimal TotalTax,
    decimal Total,
    IReadOnlyList<XeroRecodedLine> Lines)
{
    public static XeroBillRecodeResult Failed(string error) =>
        new(false, error, "", "", null, 0m, 0m, 0m, Array.Empty<XeroRecodedLine>());
}

/// <summary>
/// One bill as Xero holds it right now — the fresh read the coding run and its dry run make
/// before deciding anything about an existing bill (status, what is paid or credited against
/// it, its VAT treatment and totals). Null from the client means Xero has no bill by that id.
/// </summary>
public sealed record XeroBillSummary(
    string InvoiceId,
    string Status,
    string? InvoiceNumber,
    string? Reference,
    string? ContactName,
    DateTime? Date,
    string LineAmountTypes,
    decimal SubTotal,
    decimal TotalTax,
    decimal Total,
    decimal AmountPaid,
    decimal AmountCredited,
    decimal AmountDue,
    int LineCount,
    string? TaxType,
    /// <summary>When Xero last changed the bill (2026-09-08) — how the coding run tells the newer
    /// of two live bills that share a number.</summary>
    DateTime? UpdatedUtc = null)
{
    /// <summary>Editable in Xero: draft, submitted or authorised with nothing paid or credited.</summary>
    public bool IsRecodable =>
        (Status.Equals("DRAFT", StringComparison.OrdinalIgnoreCase)
         || Status.Equals("SUBMITTED", StringComparison.OrdinalIgnoreCase)
         || Status.Equals("AUTHORISED", StringComparison.OrdinalIgnoreCase))
        && AmountPaid == 0m && AmountCredited == 0m;

    /// <summary>Why it cannot be recoded, in words the accountant can act on.</summary>
    public string NotRecodableReason =>
        Status.Equals("PAID", StringComparison.OrdinalIgnoreCase) ? "it is PAID"
        : Status.Equals("VOIDED", StringComparison.OrdinalIgnoreCase) ? "it is VOIDED"
        : Status.Equals("DELETED", StringComparison.OrdinalIgnoreCase) ? "it is DELETED"
        : AmountPaid > 0m ? $"it is {Status} with £{AmountPaid:N2} paid against it"
        : AmountCredited > 0m ? $"it is {Status} with £{AmountCredited:N2} credited against it"
        : $"it is {Status}";
}

/// <summary>
/// Stage a brand-new DRAFT ACCPAY bill matching a settlement schedule, for a worker-month whose
/// invoice has not arrived yet — reconciled against the real one when it lands.
/// </summary>
public sealed record XeroDraftBillRequest(
    string ContactName,
    DateTime Date,
    DateTime DueDate,
    string Reference,
    IReadOnlyList<XeroScheduleLine> Lines);

// Note (2026-09-03): a sentence the caller should relay — for a staged draft bill, which VAT
// treatment was applied and where it came from (the contact's default, their last bill, or
// Xero's account default when neither answers).
public sealed record XeroApprovalResult(
    bool Succeeded,
    bool AlreadyApproved,
    string? FreshStatus,
    string? Error,
    string? Note = null)
{
    public static XeroApprovalResult Ok(string status) => new(true, false, status, null);
    public static XeroApprovalResult Ok(string status, string note) => new(true, false, status, null, note);
    public static XeroApprovalResult SkippedAlreadyApproved(string status) => new(true, true, status, null);
    public static XeroApprovalResult Failed(string error) => new(false, false, null, error);
}

// Penny-safe pro-rating of a Xero line amount across cost-centre splits lives in
// XeroSplitMaths (contracts project, next to the other tested calculation helpers).

// -- The sales invoice raised from the portal (2026-09-09) ----------------------------------

/// <summary>
/// One AUTHORISED ACCREC invoice: the client by Xero's ContactID — the contact mapped on the
/// project, never matched by name and never created (2026-09-10; ContactName is the name as the
/// portal holds it, for the preview and the audit only) — one line on the sales account with the
/// project's Sites tracking, the tax type left to Xero's own answer (contact default, else their
/// last sales invoice, else the account default — never assumed). DueDate null lets the
/// organisation's sales default apply.
/// </summary>
public sealed record XeroSalesInvoiceRequest(
    string ContactId,
    string ContactName,
    DateTime Date,
    DateTime? DueDate,
    string Reference,
    string Description,
    decimal Net,
    string AccountCode,
    string SiteOption);

/// <summary>What Xero holds after the raise. Note says where the VAT treatment came from.</summary>
public sealed record XeroSalesInvoiceResult(
    bool Succeeded,
    string? InvoiceId,
    string? InvoiceNumber,
    decimal SubTotal,
    decimal TotalTax,
    decimal Total,
    string Note,
    string? Error)
{
    public static XeroSalesInvoiceResult Failed(string error) => new(false, null, null, 0m, 0m, 0m, "", error);
}

/// <summary>
/// The mapped contact as Xero holds it, for the preview: Found with Xero's name for the contact,
/// NotFound when Xero has no contact with that id (re-map it — the raise is blocked), or
/// Unavailable when Xero could not be read at all (not connected, or the call failed — the tax
/// note says why). The tax note says where the VAT treatment would come from.
/// </summary>
public sealed record XeroSalesContactLookup(XeroSalesContactStatus Status, string? XeroName, string TaxNote)
{
    public static XeroSalesContactLookup Found(string xeroName, string taxNote) => new(XeroSalesContactStatus.Found, xeroName, taxNote);
    public static XeroSalesContactLookup NotFound(string taxNote) => new(XeroSalesContactStatus.NotFound, null, taxNote);
    public static XeroSalesContactLookup Unavailable(string taxNote) => new(XeroSalesContactStatus.Unavailable, null, taxNote);
}

public enum XeroSalesContactStatus { Found, NotFound, Unavailable }

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero;
/// <summary>
/// Minimal client for the Xero Accounting API over a custom connection (client-credentials grant).
/// Returns a snapshot rather than throwing so the UI can explain "not configured" and "Xero said no"
/// states instead of surfacing a 500. Reads purchase invoices with line items (Xero includes line
/// items on paged /Invoices responses) plus the chart of accounts for account names, and caches the
/// assembled snapshot briefly — a multi-year read costs dozens of calls against Xero's 60/min limit.
/// </summary>
public interface IXeroClient
{
    bool IsConfigured { get; }

    /// <summary>
    /// Lists purchase invoices (ACCPAY bills) from the configured start date, newest first.
    /// Serves the cached snapshot when fresh enough unless <paramref name="force"/> is set.
    /// </summary>
    Task<XeroTransactionsSnapshot> GetPurchaseInvoicesAsync(bool force, CancellationToken ct);

    /// <summary>
    /// Reads the company's cash position: every bank account's closing balance today (Xero's
    /// bank summary report — needs the accounting.reports.read scope) plus the authorised
    /// sales invoices (ACCREC) with money still due. Serves the cached snapshot when fresh
    /// enough unless <paramref name="force"/> is set.
    /// </summary>
    Task<XeroCashSummarySnapshot> GetCashSummaryAsync(bool force, CancellationToken ct);

    /// <summary>
    /// Reads the aged payables position: every ACCPAY bill with money still due — DRAFT and
    /// SUBMITTED included, because the accounting procedure leaves bills in draft until they are
    /// coded through the portal and Xero's own aged payables report cannot see them — plus every
    /// ACCPAYCREDIT credit note with credit still unapplied. Deliberately no date floor (unlike
    /// the ledger's reporting window): an old unpaid bill still belongs on the report. Serves
    /// the cached snapshot when fresh enough unless <paramref name="force"/> is set.
    /// </summary>
    Task<XeroAgedPayablesSnapshot> GetAgedPayablesAsync(bool force, CancellationToken ct);

    /// <summary>
    /// Reads the aged receivables position: every ACCREC sales invoice with money still due —
    /// DRAFT and SUBMITTED included, mirroring the payables read, so an invoice still being
    /// prepared is visible before Xero's own aged receivables can see it — plus every
    /// ACCRECCREDIT credit note with credit still unapplied. Deliberately no date floor (unlike
    /// the ledger's reporting window): an old unpaid invoice still belongs on the report. Serves
    /// the cached snapshot when fresh enough unless <paramref name="force"/> is set.
    /// </summary>
    Task<XeroAgedReceivablesSnapshot> GetAgedReceivablesAsync(bool force, CancellationToken ct);

    /// <summary>
    /// Lists the suppliers held in Xero (contacts flagged IsSupplier), A–Z by name, for the
    /// directory's "Import from Xero" modal. Serves the cached snapshot when fresh enough unless
    /// <paramref name="force"/> is set. The AlreadyImported/LinkedSubcontractorId stamps are left
    /// at their defaults here — the query handler joins them on from the directory's Xero links.
    /// </summary>
    Task<XeroSuppliersSnapshot> GetSuppliersAsync(bool force, CancellationToken ct);

    /// <summary>
    /// The people on one Xero contact, read fresh by id — the contact's primary person and its
    /// additional persons — for the contact push's "now" (2026-09-09). Null when Xero has no
    /// contact by that id. Throws XeroCallFailedException when Xero cannot be asked.
    /// </summary>
    Task<XeroContactPeople?> GetContactPeopleAsync(string contactId, CancellationToken ct);

    /// <summary>
    /// Writes the people onto one Xero contact: its primary person (FirstName / LastName /
    /// EmailAddress) and its additional persons, which Xero replaces wholesale. Needs the
    /// connection's accounting.contacts scope; a refusal comes back on the result, never thrown.
    /// </summary>
    Task<XeroApprovalResult> SetContactPeopleAsync(XeroContactPeople people, CancellationToken ct);

    /// <summary>
    /// Lists the organisation's tracking categories with their options exactly as Xero holds
    /// them (archived options included and flagged) — the read behind the Cost codes page's
    /// "Xero sites" / "Xero cost codes" tabs, so the exact phrasing of each option can be
    /// checked when linking projects and cost codes to Xero. Needs the custom connection's
    /// accounting.settings scope; a refusal (or a 429) comes back on the snapshot's Error
    /// rather than throwing. Serves the cached snapshot when fresh enough unless
    /// <paramref name="force"/> is set — one Xero call, but it shares the 60/min budget
    /// with everything else.
    /// </summary>
    Task<XeroTrackingCategoriesSnapshot> GetTrackingCategoriesSnapshotAsync(bool force, CancellationToken ct);

    /// <summary>
    /// Confirms an allocated draft (or submitted) bill / credit note back into Xero and
    /// approves it: re-reads the invoice fresh, stamps Sites + Cost Code tracking on each
    /// instructed line — physically splitting a line into one Xero line per cost centre
    /// when the allocation is split, amounts pro-rated so the invoice total is unchanged —
    /// then sets the status to AUTHORISED in the same update. Missing Cost Code tracking
    /// options are created in Xero; a missing Sites option fails loudly instead (sites are
    /// an explicit per-project mapping, never invented here). Returns a result rather than
    /// throwing so callers can stamp the outcome onto the stored ledger lines.
    /// </summary>
    Task<XeroApprovalResult> ApproveInvoiceAsync(XeroApprovalRequest request, CancellationToken ct);

    /// <summary>
    /// Writes the Sites tracking option onto specific line items of a draft,
    /// submitted or approved bill / credit note WITHOUT changing its status —
    /// the SetProject half-step (a queued line's project decided before its
    /// cost centre) and the post-approval change of mind (a line moved between
    /// projects after the bill was approved, decision 2026-08-14). Untargeted
    /// lines pass through untouched; targeted lines keep any other tracking
    /// (Xero's own cost code) they already carry. Returns AlreadyApproved (a
    /// silent success) only for PAID invoices — Xero locks their lines once
    /// payments are applied, so those keep flowing portal-side only.
    /// </summary>
    Task<XeroApprovalResult> SetSiteTrackingAsync(XeroSiteTrackingRequest request, CancellationToken ct);

    /// <summary>
    /// Strips Sites and Cost Code tracking off EVERY line of a bill / credit note, status
    /// untouched — the undo of a Work Order bill approval (2026-09-08). Works on draft, submitted
    /// and approved bills with nothing paid or credited; a paid bill is locked and comes back as
    /// AlreadyApproved (a silent skip), like the site write. Addressed by bill, not by line id,
    /// because an approval that split a line replaced it with fresh Xero lines.
    /// </summary>
    Task<XeroApprovalResult> ClearTrackingAsync(string invoiceId, bool isCreditNote, CancellationToken ct);

    /// <summary>
    /// One bill as Xero holds it right now — status, what is paid or credited against it, VAT
    /// treatment, totals (2026-09-03). Null when Xero has no bill by that id (deleted). Throws
    /// <see cref="XeroCallFailedException"/> when Xero can't be asked, so "deleted" and "Xero is
    /// down" never read the same.
    /// </summary>
    Task<XeroBillSummary?> GetBillAsync(string invoiceId, CancellationToken ct);

    /// <summary>
    /// Every bill Xero holds under one invoice number, live or voided, newest first (2026-09-08)
    /// — how the coding run finds the re-issue of a bill that was voided and keyed again after
    /// the ledger last saw it. Throws <see cref="XeroCallFailedException"/> when Xero can't be
    /// asked.
    /// </summary>
    Task<IReadOnlyList<XeroBillSummary>> FindBillsByNumberAsync(string invoiceNumber, CancellationToken ct);

    /// <summary>
    /// Recodes a bill's whole line list to a settlement schedule
    /// (docs/Labour-Overview-Forecast-and-Xero-Mapping-Scope.md §6a) — DRAFT, SUBMITTED or
    /// AUTHORISED with nothing paid or credited (2026-09-03: the cover route authorises the
    /// bill BEFORE the run sees it, so authorised is the normal state). Status, LineAmountTypes,
    /// tax type and totals are preserved; the schedule supplies the split. The result carries
    /// the fresh line ids so the caller can re-point covers. Refused for paid / credited /
    /// voided / deleted bills.
    /// </summary>
    Task<XeroBillRecodeResult> RecodeBillAsync(XeroBillCodingRequest request, CancellationToken ct);

    /// <summary>
    /// Stages a brand-new DRAFT ACCPAY bill matching a settlement schedule. FreshStatus carries
    /// the new bill's InvoiceID on success so the run can record what it created. The tax type
    /// is NEVER assumed (2026-09-03): the contact's default purchases tax type, else the tax
    /// type on the contact's most recent bill, else omitted so Xero's account default applies —
    /// Note says which, so the run can relay it.
    /// </summary>
    Task<XeroApprovalResult> CreateDraftBillAsync(XeroDraftBillRequest request, CancellationToken ct);

    /// <summary>
    /// Lists the attachments Xero holds for one invoice or credit note — the supplier's
    /// document(s), typically published by Dext. Requires the custom connection's
    /// accounting.attachments scope; throws <see cref="XeroCallFailedException"/> (message
    /// safe to surface) when Xero refuses.
    /// </summary>
    Task<IReadOnlyList<XeroInvoiceAttachment>> ListAttachmentsAsync(
        string invoiceId, bool isCreditNote, CancellationToken ct);

    /// <summary>
    /// Streams one attachment's bytes by file name (Xero's attachment content endpoint is
    /// addressed by file name, with the Accept header naming the content type). Null when
    /// the invoice has no attachment by that name.
    /// </summary>
    Task<XeroAttachmentContent?> GetAttachmentAsync(
        string invoiceId, bool isCreditNote, string fileName, CancellationToken ct);

    /// <summary>
    /// Raises the AUTHORISED sales invoice a valuation invoice represents (2026-09-09). The tax
    /// type is NEVER assumed: the contact's default sales tax type, else the tax type on their
    /// most recent sales invoice, else omitted so Xero's account default applies — Note says
    /// which. Refused with the fix when the project's Sites option is not in Xero.
    /// </summary>
    Task<XeroSalesInvoiceResult> CreateSalesInvoiceAsync(XeroSalesInvoiceRequest request, CancellationToken ct);

    /// <summary>The client as Xero holds it (by ContactID, else exact name) and where its sales
    /// tax type would come from — the preview's reading, no write.</summary>
    Task<XeroSalesContactLookup> LookupSalesContactAsync(string? contactId, string contactName, CancellationToken ct);

    /// <summary>
    /// Attaches one file to an invoice (the certificate PDF on a raised sales invoice). Needs the
    /// custom connection's accounting.attachments scope; returns the refusal as an error rather
    /// than throwing — an invoice stands without its attachment.
    /// </summary>
    Task<XeroApprovalResult> AttachToInvoiceAsync(
        string invoiceId, string fileName, string contentType, byte[] content, CancellationToken ct);

    /// <summary>
    /// One site's monthly P&amp;L from Xero's profit &amp; loss report filtered by the named
    /// "Sites" tracking option: income, cost of sales and operating expenses per month, first
    /// of month, oldest first, months with no movement omitted. Needs the custom connection's
    /// accounting.reports.read scope. Reads in windows of up to twelve monthly columns per
    /// call (Xero's periods cap), so a multi-year history costs a handful of calls. Throws
    /// <c>XeroCallFailedException</c> (message safe to surface) when Xero refuses or the
    /// option doesn't exist — the site mapping is explicit, so a miss is a config error to
    /// report, never something to guess around.
    /// </summary>
    Task<IReadOnlyList<XeroSitePnlMonthFigures>> GetSiteMonthlyPnlAsync(
        string siteOption, DateTime fromMonth, DateTime toMonth, CancellationToken ct);

    /// <summary>
    /// One site's P&amp;L over a single plain date range — one Xero call, no comparison
    /// periods — for reconciling the stored months against Xero's own whole-range figure.
    /// Null when Xero isn't configured. Throws <c>XeroCallFailedException</c> like the
    /// monthly read.
    /// </summary>
    Task<XeroSitePnlRangeFigures?> GetSiteRangePnlAsync(
        string siteOption, DateTime fromDate, DateTime toDate, CancellationToken ct);
}

/// <summary>One attachment's bytes plus the content type Xero reported for it.</summary>
public sealed record XeroAttachmentContent(byte[] Content, string ContentType, string FileName);


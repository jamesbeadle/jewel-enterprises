using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.XeroRaise;

/// <summary>
/// One rule for what a valuation invoice becomes in Xero — the preview a person confirms and the
/// write that follows both read it, so what was shown is what is raised. Loads the invoice, its
/// project, the contract and the certificate the register holds for the claim, asks Xero for the
/// contact MAPPED ON THE PROJECT (by id only — never the client's name, never created;
/// 2026-09-10), takes the user's invoice and due dates when given (today and the certificate rule
/// otherwise), and names every blocker rather than the first.
/// </summary>
internal sealed class ValuationInvoiceXeroRaisePlanner
{
    private readonly JpmsContext context;
    private readonly IXeroClient xero;
    private readonly XeroOptions options;

    public ValuationInvoiceXeroRaisePlanner(JpmsContext context, IXeroClient xero, XeroOptions options)
    {
        this.context = context;
        this.xero = xero;
        this.options = options;
    }

    public async Task<ValuationInvoiceXeroRaisePlan> PlanAsync(
        string valuationInvoiceId, DateTime? invoiceDate, DateTime? dueDate, CancellationToken ct)
    {
        var invoice = await context.ValuationInvoices.AsNoTracking()
            .SingleOrDefaultAsync(row => row.ValuationInvoiceId == valuationInvoiceId, ct)
            ?? throw new InvalidOperationException($"Valuation invoice {valuationInvoiceId} not found.");
        var project = await context.Projects.AsNoTracking()
            .SingleOrDefaultAsync(row => row.ProjectId == invoice.ProjectId, ct)
            ?? throw new InvalidOperationException("The invoice's project no longer exists.");
        var certificate = await ValuationInvoiceXeroRaiseSources.CertificateForAsync(context, invoice, ct);
        var contract = await context.ProjectContracts.AsNoTracking().FirstOrDefaultAsync(row => row.ProjectId == project.ProjectId, ct);

        var (contactId, contactName) = ValuationInvoiceXeroRaiseSources.MappedContactOf(project);
        var lookup = contactId is null
            ? XeroSalesContactLookup.NotFound("No Xero contact is mapped on the project, so nothing was read from Xero.")
            : await xero.LookupSalesContactAsync(contactId, ct);

        var date = invoiceDate?.Date ?? DateTime.UtcNow.Date;
        var (due, dueNote) = DueDateFor(dueDate, certificate, contract);
        var request = new XeroSalesInvoiceRequest(
            contactId ?? "", contactName, date, due, ReferenceFor(invoice),
            DescriptionFor(invoice), invoice.Amount, options.SalesAccountCode, project.XeroSiteName ?? "");

        return new ValuationInvoiceXeroRaisePlan(invoice, project, certificate, request, lookup, dueNote,
            Blockers(invoice, project, contactId, lookup));
    }

    /// <summary>Xero's Reference: the INVOICE's number, two digits — "Valuation 05" (2026-09-10).</summary>
    internal static string ReferenceFor(ValuationInvoiceEntity invoice) => $"Valuation {invoice.Number:00}";

    /// <summary>The one line's text, numbered from the INVOICE (never the claim) and naming the
    /// period's valuation report — the certificate is attached, not described (2026-09-10).</summary>
    internal static string DescriptionFor(ValuationInvoiceEntity invoice) =>
        $"Valuation {invoice.Number:00} - Payment due as per {invoice.PeriodMonth:MMMM yyyy} valuation report (ex VAT)";

    /// <summary>The user's due date when given; else the final date for payment when the
    /// certificate and the contract give it (the certificate's issue date plus the contract's
    /// days); otherwise Xero's own sales default. The note says which applied.</summary>
    private static (DateTime? DueDate, string Note) DueDateFor(DateTime? requested, PaymentCertificateEntity? certificate, ProjectContractEntity? contract)
    {
        if (requested is { } chosen)
            return (chosen.Date, $"Due {chosen:dd MMM yyyy} — the date you gave.");
        if (certificate is not null && contract is { FinalDateForPaymentDays: > 0 })
            return (certificate.IssuedDate.UtcDateTime.Date.AddDays(contract.FinalDateForPaymentDays),
                $"Due {contract.FinalDateForPaymentDays} days after the certificate's {certificate.IssuedDate:dd MMM yyyy} — the contract's final date for payment. Give a due date to override it.");
        return (null, "No certificate date and contract terms to work from — Xero's default sales due date applies unless you give one.");
    }

    private static IReadOnlyList<string> Blockers(ValuationInvoiceEntity invoice, ProjectEntity project, string? contactId, XeroSalesContactLookup lookup)
    {
        var blockers = new List<string>();
        // A number recorded by hand counts as raised exactly as the portal's own raise does.
        if (!string.IsNullOrWhiteSpace(invoice.XeroInvoiceId) || !string.IsNullOrWhiteSpace(invoice.XeroInvoiceNumber))
            blockers.Add($"Already raised in Xero as {(string.IsNullOrWhiteSpace(invoice.XeroInvoiceNumber) ? invoice.XeroInvoiceId : invoice.XeroInvoiceNumber)}"
                + (invoice.XeroRaisedAt is { } raisedAt ? $" on {raisedAt:dd MMM yyyy}." : "."));
        switch ((ValuationInvoiceStatus)invoice.Status)
        {
            case ValuationInvoiceStatus.Raised or ValuationInvoiceStatus.Submitted or ValuationInvoiceStatus.Approved: break;
            case ValuationInvoiceStatus.Rejected: blockers.Add("The invoice was rejected — amend and resubmit it first."); break;
            case ValuationInvoiceStatus.Cancelled: blockers.Add("A cancelled invoice cannot be raised."); break;
            default: blockers.Add($"The invoice is already {(ValuationInvoiceStatus)invoice.Status} — raise in Xero happens at issue."); break;
        }
        if (invoice.Amount <= 0m) blockers.Add("The invoice amount must be greater than zero.");
        if (contactId is null)
            blockers.Add($"No Xero contact is mapped on {project.Name} — set it in Project settings (Xero contact).");
        else if (lookup.Status == XeroSalesContactStatus.NotFound)
            blockers.Add("The mapped Xero contact was not found in Xero — re-map it in Project settings.");
        if (string.IsNullOrWhiteSpace(project.XeroSiteName))
            blockers.Add($"{project.Name} has no Xero site mapping — set it on the project (Xero mappings) so the invoice carries its Sites tracking.");
        return blockers;
    }
}

/// <summary>Everything the preview shows and the write uses.</summary>
internal sealed record ValuationInvoiceXeroRaisePlan(
    ValuationInvoiceEntity Invoice,
    ProjectEntity Project,
    PaymentCertificateEntity? Certificate,
    XeroSalesInvoiceRequest Request,
    XeroSalesContactLookup Contact,
    string DueDateNote,
    IReadOnlyList<string> Blockers)
{
    public string ContactStatus => string.IsNullOrWhiteSpace(Request.ContactId)
        ? "No Xero contact mapped on the project — Raise in Xero is blocked until it is set in Project settings"
        : Contact.Status switch
        {
            XeroSalesContactStatus.Found => string.IsNullOrWhiteSpace(Contact.XeroName) || Contact.XeroName == Request.ContactName
                ? "Mapped on the project and found in Xero"
                : $"Mapped on the project and found in Xero as \"{Contact.XeroName}\"",
            XeroSalesContactStatus.NotFound => "Mapped on the project but NOT found in Xero — re-map it in Project settings",
            _ => "Mapped on the project; Xero could not be read to confirm it"
        };

    public string CertificateNote => Certificate is null
        ? "No payment certificate is filed against this claim — nothing to attach. File it from Document Triage first if you want it on the invoice."
        : $"Certificate {Certificate.CertificateNumber} ({Certificate.FileName}) will be attached.";

    public ValuationInvoiceXeroRaisePreview ToPreview() => new(
        Invoice.ValuationInvoiceId, Invoice.Reference, Request.Reference, Request.ContactName,
        string.IsNullOrWhiteSpace(Request.ContactId) ? null : Request.ContactId, ContactStatus,
        Request.Net, Request.Description, Request.AccountCode,
        string.IsNullOrWhiteSpace(Request.SiteOption) ? null : Request.SiteOption,
        Request.Date, Request.DueDate, DueDateNote, Contact.TaxNote,
        Certificate?.FileName, CertificateNote, Blockers);
}

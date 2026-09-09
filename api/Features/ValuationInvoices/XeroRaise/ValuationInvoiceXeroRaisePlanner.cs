using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.XeroRaise;

/// <summary>
/// One rule for what a valuation invoice becomes in Xero — the preview a person confirms and the
/// write that follows both read it, so what was shown is what is raised. Loads the invoice, its
/// project, claim, client and the certificate the register holds for the claim, asks Xero how it
/// knows the client, and names every blocker rather than the first.
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

    public async Task<ValuationInvoiceXeroRaisePlan> PlanAsync(string valuationInvoiceId, CancellationToken ct)
    {
        var invoice = await context.ValuationInvoices.AsNoTracking()
            .SingleOrDefaultAsync(row => row.ValuationInvoiceId == valuationInvoiceId, ct)
            ?? throw new InvalidOperationException($"Valuation invoice {valuationInvoiceId} not found.");
        var project = await context.Projects.AsNoTracking()
            .SingleOrDefaultAsync(row => row.ProjectId == invoice.ProjectId, ct)
            ?? throw new InvalidOperationException("The invoice's project no longer exists.");
        var claim = invoice.ValuationClaimId is null ? null
            : await context.ValuationClaims.AsNoTracking().SingleOrDefaultAsync(row => row.ValuationClaimId == invoice.ValuationClaimId, ct);
        var certificate = await ValuationInvoiceXeroRaiseSources.CertificateForAsync(context, invoice, ct);
        var contract = await context.ProjectContracts.AsNoTracking().FirstOrDefaultAsync(row => row.ProjectId == project.ProjectId, ct);

        var clientName = await ValuationInvoiceXeroRaiseSources.ClientNameAsync(context, project, ct);
        var linkedContactId = await ValuationInvoiceXeroRaiseSources.LinkedXeroContactIdAsync(context, clientName, ct);
        var lookup = await xero.LookupSalesContactAsync(linkedContactId, clientName, ct);

        var date = DateTime.UtcNow.Date;
        var (dueDate, dueNote) = DueDateFor(certificate, contract, date);
        var request = new XeroSalesInvoiceRequest(
            lookup.ContactId ?? linkedContactId, clientName, date, dueDate, invoice.Reference,
            DescriptionFor(project, claim, invoice, certificate), invoice.Amount, options.SalesAccountCode, project.XeroSiteName ?? "");

        return new ValuationInvoiceXeroRaisePlan(invoice, project, certificate, request, lookup, dueNote,
            Blockers(invoice, project));
    }

    private static string DescriptionFor(ProjectEntity project, ValuationClaimEntity? claim, ValuationInvoiceEntity invoice, PaymentCertificateEntity? certificate)
    {
        var valuation = claim is null ? invoice.Reference : $"Valuation {claim.ClaimNumber} ({claim.Name})";
        var certified = certificate is null ? "" : $" — payment certificate {certificate.CertificateNumber} of {certificate.IssuedDate:dd MMM yyyy}";
        return $"{project.Name} — {valuation}{certified}. Period {invoice.PeriodMonth:MMMM yyyy}.";
    }

    /// <summary>The final date for payment when the certificate and the contract give it (the
    /// certificate's issue date plus the contract's days); otherwise Xero's own sales default.</summary>
    private static (DateTime? DueDate, string Note) DueDateFor(PaymentCertificateEntity? certificate, ProjectContractEntity? contract, DateTime today)
    {
        if (certificate is not null && contract is { FinalDateForPaymentDays: > 0 })
            return (certificate.IssuedDate.UtcDateTime.Date.AddDays(contract.FinalDateForPaymentDays),
                $"Due {contract.FinalDateForPaymentDays} days after the certificate's {certificate.IssuedDate:dd MMM yyyy} — the contract's final date for payment.");
        return (null, "No certificate date and contract terms to work from — Xero's default sales due date applies.");
    }

    private static IReadOnlyList<string> Blockers(ValuationInvoiceEntity invoice, ProjectEntity project)
    {
        var blockers = new List<string>();
        if (!string.IsNullOrWhiteSpace(invoice.XeroInvoiceId))
            blockers.Add($"Already raised in Xero as {invoice.XeroInvoiceNumber ?? invoice.XeroInvoiceId} on {invoice.XeroRaisedAt:dd MMM yyyy}.");
        switch ((ValuationInvoiceStatus)invoice.Status)
        {
            case ValuationInvoiceStatus.Raised or ValuationInvoiceStatus.Submitted or ValuationInvoiceStatus.Approved: break;
            case ValuationInvoiceStatus.Rejected: blockers.Add("The invoice was rejected — amend and resubmit it first."); break;
            case ValuationInvoiceStatus.Cancelled: blockers.Add("A cancelled invoice cannot be raised."); break;
            default: blockers.Add($"The invoice is already {(ValuationInvoiceStatus)invoice.Status} — raise in Xero happens at issue."); break;
        }
        if (invoice.Amount <= 0m) blockers.Add("The invoice amount must be greater than zero.");
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
    public string ContactStatus => Contact.ContactId is null
        ? "Not in Xero — a contact is created with the invoice"
        : "Matched in Xero";

    public string CertificateNote => Certificate is null
        ? "No payment certificate is filed against this claim — nothing to attach. File it from Document Triage first if you want it on the invoice."
        : $"Certificate {Certificate.CertificateNumber} ({Certificate.FileName}) will be attached.";

    public ValuationInvoiceXeroRaisePreview ToPreview() => new(
        Invoice.ValuationInvoiceId, Invoice.Reference, Request.ContactName, Request.ContactId, ContactStatus,
        Request.Net, Request.Description, Request.AccountCode,
        string.IsNullOrWhiteSpace(Request.SiteOption) ? null : Request.SiteOption,
        Request.Date, Request.DueDate, DueDateNote, Contact.TaxNote,
        Certificate?.FileName, CertificateNote, Blockers);
}

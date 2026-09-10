using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.XeroRaise;

/// <summary>The portal facts a raise needs beyond the invoice: the Xero contact mapped on the
/// project (never the client's name — 2026-09-10) and which certificate the register holds.</summary>
internal static class ValuationInvoiceXeroRaiseSources
{
    /// <summary>
    /// The Xero contact the project's sales invoices are raised on, exactly as mapped in Project
    /// settings: (ContactID or null, the name to show). Null id means no mapping — a blocker, never
    /// a name match and never a contact created with the invoice. The name shown is the mapped
    /// contact's, else the project's client name so the preview still says who would be invoiced.
    /// </summary>
    public static (string? ContactId, string ContactName) MappedContactOf(ProjectEntity project)
    {
        var contactId = string.IsNullOrWhiteSpace(project.XeroContactId) ? null : project.XeroContactId.Trim();
        var name = contactId is not null && !string.IsNullOrWhiteSpace(project.XeroContactName)
            ? project.XeroContactName.Trim()
            : (project.ClientName ?? "").Trim();
        return (contactId, name);
    }

    /// <summary>The newest certificate filed against the invoice's claim — the register is the
    /// only source; nothing is guessed from the mailbox.</summary>
    public static async Task<PaymentCertificateEntity?> CertificateForAsync(JpmsContext context, ValuationInvoiceEntity invoice, CancellationToken ct)
    {
        if (invoice.ValuationClaimId is null) return null;
        return await context.PaymentCertificates.AsNoTracking()
            .Where(row => row.ProjectId == invoice.ProjectId && row.ValuationClaimId == invoice.ValuationClaimId)
            .OrderByDescending(row => row.IssuedDate).ThenByDescending(row => row.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }
}

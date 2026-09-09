using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.XeroRaise;

/// <summary>The three portal facts a raise needs beyond the invoice: who the client is, whether
/// the directory already links them to a Xero contact, and which certificate the register holds.</summary>
internal static class ValuationInvoiceXeroRaiseSources
{
    /// <summary>The client account's name when the project corresponds with a client directly;
    /// the project's free-text client name otherwise (an architect's project still invoices the client).</summary>
    public static async Task<string> ClientNameAsync(JpmsContext context, ProjectEntity project, CancellationToken ct)
    {
        if ((PartyKind)project.PartyKind == PartyKind.Client && project.PartyId is not null)
        {
            var client = await context.Clients.AsNoTracking().SingleOrDefaultAsync(row => row.ClientId == project.PartyId, ct);
            if (client is not null && !string.IsNullOrWhiteSpace(client.Name)) return client.Name.Trim();
        }
        if (string.IsNullOrWhiteSpace(project.ClientName))
            throw new InvalidOperationException($"{project.Name} has no client name — set the client on the project first.");
        return project.ClientName.Trim();
    }

    /// <summary>A directory record of category Client with this name and a Xero link gives the
    /// contact outright; otherwise Xero is asked by name.</summary>
    public static async Task<string?> LinkedXeroContactIdAsync(JpmsContext context, string clientName, CancellationToken ct)
    {
        var recordIds = await context.Subcontractors.AsNoTracking()
            .Where(row => row.Category == (int)DirectoryCategory.Client && row.CompanyName == clientName)
            .Select(row => row.SubcontractorId)
            .ToListAsync(ct);
        if (recordIds.Count == 0) return null;
        return await context.SubcontractorXeroLinks.AsNoTracking()
            .Where(link => recordIds.Contains(link.SubcontractorId))
            .OrderBy(link => link.ImportedAt)
            .Select(link => link.XeroContactId)
            .FirstOrDefaultAsync(ct);
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

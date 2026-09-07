namespace Jewel.JPMS.Api.Features.Commercial;

/// <summary>
/// The ONE rule for what a claim's "Certified to date" measures: the gross certification (cash
/// amount plus the deposit credit embedded in it) of every Issued/Paid valuation invoice that came
/// BEFORE the claim — invoices drawn against earlier claims, and historic entries linked to no
/// claim at all. The claim's own invoice never counts against itself, and nor does a later
/// claim's: a statement is what the client was asked to pay as at that claim, and it must still
/// say so after the invoice it raised has been issued and paid.
///
/// (Until 2026-09-07 a locked claim's totals were re-frozen from EVERY issued invoice, so the
/// moment its own invoice was issued its certified figure absorbed it and its payment due went
/// to nil — Abbot Road's Valuation 14 read £232,112.02 / £0.00 where the statement said
/// £207,235.16 / £24,876.86.)
///
/// Every writer of a claim's frozen totals (<see cref="ValuationClaimSummary"/>) and the snapshot
/// capture behind every PDF and spreadsheet read from here. A Draft claim is computed live in the
/// browser from every issued invoice; it can carry no invoice of its own (a raise needs a locked
/// claim), so the two readings coincide there.
/// </summary>
internal static class CertifiedBeforeClaim
{
    public sealed record Certification(decimal CertifiedToDate, decimal DepositCreditedToDate);

    /// <summary>
    /// Certification before the claim numbered <paramref name="claimNumber"/>. A null claim number
    /// (a project that has never been valued) counts every issued invoice.
    /// </summary>
    public static async Task<Certification> ForAsync(
        JpmsContext context, string projectId, int? claimNumber, CancellationToken cancellationToken)
    {
        var issued = await (
                from invoice in context.ValuationInvoices.AsNoTracking()
                join candidate in context.ValuationClaims.AsNoTracking()
                    on invoice.ValuationClaimId equals candidate.ValuationClaimId into linkedClaims
                from linked in linkedClaims.DefaultIfEmpty()
                where invoice.ProjectId == projectId
                      && (invoice.Status == (int)ValuationInvoiceStatus.Issued
                          || invoice.Status == (int)ValuationInvoiceStatus.Paid)
                select new
                {
                    invoice.Amount,
                    invoice.DepositCredited,
                    ClaimNumber = linked != null ? (int?)linked.ClaimNumber : null
                })
            .ToListAsync(cancellationToken);

        var counted = issued
            .Where(invoice => Counts(invoice.ClaimNumber, claimNumber))
            .ToList();

        return new Certification(
            counted.Sum(invoice => invoice.Amount + invoice.DepositCredited),
            counted.Sum(invoice => invoice.DepositCredited));
    }

    /// <summary>
    /// Whether an issued invoice counts toward a claim's certification: it does when it was drawn
    /// against an earlier claim, or against no claim (a historic entry); never when it belongs to
    /// the claim itself or a later one.
    /// </summary>
    public static bool Counts(int? invoiceClaimNumber, int? claimNumber)
    {
        if (claimNumber is null) return true;
        if (invoiceClaimNumber is null) return true;
        return invoiceClaimNumber < claimNumber;
    }
}

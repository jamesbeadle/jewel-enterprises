using Jewel.JPMS.Api.Features.Commercial;
using Xunit;

namespace Jewel.JPMS.Tests;

// A claim's "Certified to date" counts the invoices that came before it — never its own, never
// a later claim's — so a locked claim reads the same after its invoice is issued and paid as the
// statement said (Abbot Road Valuation 14, 2026-09-07: £207,235.16 / £24,876.86, not
// £232,112.02 / £0.00).
public sealed class CertifiedBeforeClaimTests
{
    [Theory]
    [InlineData(3, 4, true)]   // an earlier claim's invoice certifies toward claim 4
    [InlineData(4, 4, false)]  // the claim's own invoice never counts against itself
    [InlineData(5, 4, false)]  // nor does a later claim's
    [InlineData(1, 2, true)]
    public void Counts_earlierClaimsOnly(int invoiceClaimNumber, int claimNumber, bool expected) =>
        Assert.Equal(expected, CertifiedBeforeClaim.Counts(invoiceClaimNumber, claimNumber));

    [Fact]
    public void Counts_historicInvoiceLinkedToNoClaim_alwaysCertifies()
    {
        Assert.True(CertifiedBeforeClaim.Counts(null, 1));
        Assert.True(CertifiedBeforeClaim.Counts(null, 4));
    }

    [Fact]
    public void Counts_projectWithNoClaim_countsEveryIssuedInvoice()
    {
        Assert.True(CertifiedBeforeClaim.Counts(null, null));
        Assert.True(CertifiedBeforeClaim.Counts(7, null));
    }

    // Abbot Road as it stood on 3 Sep 2026: three earlier certificates plus claim 4's own.
    [Fact]
    public void AbbotRoadValuation14_readsCertificationBeforeItself()
    {
        var issued = new (int? ClaimNumber, decimal Gross)[]
        {
            (1, 100_000.00m),
            (2, 60_000.00m),
            (3, 47_235.16m),
            (4, 24_876.86m)    // VI-0005, drawn against claim 4 itself
        };

        var certifiedBeforeClaim4 = issued
            .Where(invoice => CertifiedBeforeClaim.Counts(invoice.ClaimNumber, 4))
            .Sum(invoice => invoice.Gross);
        var certifiedBeforeClaim5 = issued
            .Where(invoice => CertifiedBeforeClaim.Counts(invoice.ClaimNumber, 5))
            .Sum(invoice => invoice.Gross);

        Assert.Equal(207_235.16m, certifiedBeforeClaim4);
        Assert.Equal(232_112.02m, certifiedBeforeClaim5);
    }
}

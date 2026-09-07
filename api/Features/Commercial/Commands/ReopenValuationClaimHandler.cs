using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

// The undo for an unintended "We're claiming this": Preapproved -> Draft. Clears the
// preapproval stamp and zeroes the frozen totals — a Draft's figures compute live from
// its entries, exactly as before preapproval, so nothing else needs recomputing.
// Confirmed claims are final (their amounts advanced CertifiedToDate) and cannot reopen.
public sealed class ReopenValuationClaimHandler : ICommandHandler<ReopenValuationClaim, ValuationClaim>
{
    private readonly JpmsContext context;
    public ReopenValuationClaimHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationClaim> HandleAsync(ReopenValuationClaim command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationClaims.FindAsync(new object?[] { command.ValuationClaimId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Valuation claim {command.ValuationClaimId} was not found.");

        if (entity.Status == (int)ValuationClaimStatus.Confirmed)
            throw new InvalidOperationException("A confirmed claim is final — its amounts have advanced certified to date and it cannot be reopened.");
        if (entity.Status != (int)ValuationClaimStatus.Preapproved)
            throw new InvalidOperationException("Only a Preapproved claim can be reopened to Draft.");

        // A claim with a live invoice against it is what that invoice (and its frozen statement)
        // describes — reopening it would leave the invoice pointing at figures being re-edited,
        // and a Draft's live summary counts every issued invoice, its own included, so the payment
        // due would jump on re-lock. Cancel the invoice first (2026-09-07).
        var liveInvoiceReference = await context.ValuationInvoices.AsNoTracking()
            .Where(invoice => invoice.ValuationClaimId == entity.ValuationClaimId
                              && invoice.Status != (int)ValuationInvoiceStatus.Cancelled)
            .OrderByDescending(invoice => invoice.Number)
            .Select(invoice => invoice.Reference)
            .FirstOrDefaultAsync(cancellationToken);
        if (liveInvoiceReference is not null)
            throw new InvalidOperationException($"This claim has invoice {liveInvoiceReference} against it — cancel that first, then reopen the claim.");

        entity.Status = (int)ValuationClaimStatus.Draft;
        entity.PreapprovedAt = null;
        // Frozen totals go back to zero, matching a freshly started claim: Draft views
        // compute the summary live from the line entries.
        entity.ContractSum = 0m;
        entity.NetVariations = 0m;
        entity.RevisedContractSum = 0m;
        entity.TotalWorksComplete = 0m;
        entity.RetentionHeld = 0m;
        entity.RetentionReleased = 0m;
        entity.DepositReleased = 0m;
        entity.CertifiedToDate = 0m;
        entity.PaymentDueExVat = 0m;

        // Back on Draft, the deposit AND retention terms track the project's current
        // record again (the same rule SetProjectRetention applies to open drafts), so a
        // claim locked before terms were recorded picks them up when it is reopened and
        // re-locked. The completion release % follows StartValuationClaimHandler's rule:
        // it only bites once the claim date has reached practical completion.
        var terms = await context.ProjectRetentions
            .FirstOrDefaultAsync(retention => retention.ProjectId == entity.ProjectId, cancellationToken);
        if (terms is not null)
        {
            entity.RetentionPercent = terms.RetentionPercent;
            entity.RetentionReleasePercent =
                terms.PracticalCompletionAt is { } practicalCompletion && entity.ClaimDate >= practicalCompletion
                    ? terms.CompletionReleasePercent
                    : 0m;
            entity.DepositPercent = terms.DepositPercent;
            entity.DepositReleasedOpening = terms.DepositReleasedOpening;
        }

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

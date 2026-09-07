using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

// Client has paid. Re-freezes the totals and marks the claim Confirmed; from here its
// per-row claimed amounts are final and advance CertifiedToDate for the next claim.
//
// Confirming changes nothing about any later claim's period figures: a claim's "previous"
// is the claim immediately before it whatever its status (ClaimPeriodBaseline), so the
// baseline was already this claim while it was merely preapproved. Payment timing shows in
// certified-to-date, never in the movement column.
public sealed class ConfirmValuationClaimHandler : ICommandHandler<ConfirmValuationClaim, ValuationClaim>
{
    private readonly JpmsContext context;
    public ConfirmValuationClaimHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationClaim> HandleAsync(ConfirmValuationClaim command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationClaims.FindAsync(new object?[] { command.ValuationClaimId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Valuation claim {command.ValuationClaimId} was not found.");

        await ValuationClaimSummary.ApplyTotalsAsync(context, entity, cancellationToken);
        entity.Status = (int)ValuationClaimStatus.Confirmed;
        entity.ConfirmedAt = DateTimeOffset.UtcNow;
        if (entity.PreapprovedAt is null) entity.PreapprovedAt = entity.ConfirmedAt;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

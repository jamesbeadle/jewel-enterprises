using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class StartValuationClaimHandler : ICommandHandler<StartValuationClaim, ValuationClaim>
{
    private readonly JpmsContext context;
    public StartValuationClaimHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationClaim> HandleAsync(StartValuationClaim command, CancellationToken cancellationToken)
    {
        // Retention comes from the project's terms (Setup), stamped here as the claim's
        // frozen copy so later term changes never rewrite a locked claim. The completion
        // release % only applies once the claim date has reached practical completion —
        // the release money itself stays a separately confirmed event on the schedule.
        var terms = await context.ProjectRetentions
            .FirstOrDefaultAsync(retention => retention.ProjectId == command.ProjectId, cancellationToken);
        var retentionPercent = command.RetentionPercent ?? terms?.RetentionPercent ?? 0m;
        var retentionReleasePercent = command.RetentionReleasePercent
            ?? (terms?.PracticalCompletionAt is { } practicalCompletion && command.ClaimDate >= practicalCompletion
                ? terms.CompletionReleasePercent
                : 0m);

        var entity = new ValuationClaimEntity
        {
            ValuationClaimId = CommercialIdentifierFactory.NextValuationClaimId(),
            ProjectId = command.ProjectId,
            ClaimNumber = command.ClaimNumber,
            Name = command.Name?.Trim() ?? "",
            ClaimDate = command.ClaimDate,
            Status = (int)ValuationClaimStatus.Draft,
            RetentionPercent = retentionPercent,
            RetentionReleasePercent = retentionReleasePercent,
            // Cash-up-front deposit terms from the same record; SetProjectRetention keeps
            // them current on Drafts, and locking freezes them with the other totals.
            DepositPercent = terms?.DepositPercent ?? 0m,
            DepositReleasedOpening = terms?.DepositReleasedOpening ?? 0m,
            PreapprovedAt = null,
            ConfirmedAt = null
            // Summary totals stay zero until the claim is preapproved / confirmed.
        };
        context.ValuationClaims.Add(entity);

        // Cumulative rollover: open with the seed claim's per-line percentages so the new
        // period starts where the last one left off — only movement needs entering, and
        // the report never collapses to 0% between periods.
        if (!string.IsNullOrWhiteSpace(command.SeedFromClaimId))
        {
            var seedClaim = await context.ValuationClaims.FindAsync(new object?[] { command.SeedFromClaimId }, cancellationToken)
                ?? throw new KeyNotFoundException($"Seed claim {command.SeedFromClaimId} was not found.");
            if (seedClaim.ProjectId != command.ProjectId)
                throw new InvalidOperationException("A claim can only be seeded from a claim on the same project.");

            var seedLines = await context.ClaimLines
                .Where(line => line.ValuationClaimId == command.SeedFromClaimId)
                .ToListAsync(cancellationToken);

            // Copy only entries whose bill line still exists — a removed line must not resurrect.
            var liveLineItemIds = (await context.ValuationLineItems
                    .Where(line => line.ProjectId == command.ProjectId)
                    .Select(line => line.ValuationLineItemId)
                    .ToListAsync(cancellationToken))
                .ToHashSet();

            // Baseline per line = cumulative on the claim immediately before this one (the one
            // rule, ClaimPeriodBaseline), fetched once for all lines. Seeded from the latest
            // claim, that IS the seed claim, so every line opens with a nil period increment —
            // only movement entered on the new claim counts as this period's.
            var previousByLine = await ClaimPeriodBaseline.PreviousCumulativeByLineAsync(
                context, command.ProjectId, command.ClaimNumber, cancellationToken);

            foreach (var seedLine in seedLines.Where(line => liveLineItemIds.Contains(line.ValuationLineItemId)))
            {
                context.ClaimLines.Add(new ClaimLineEntity
                {
                    ClaimLineId = CommercialIdentifierFactory.NextClaimLineId(),
                    ValuationClaimId = entity.ValuationClaimId,
                    ValuationLineItemId = seedLine.ValuationLineItemId,
                    PercentComplete = seedLine.PercentComplete,
                    CumulativeClaimed = seedLine.CumulativeClaimed,
                    PeriodIncrement = ClaimPeriodBaseline.PeriodIncrement(
                        seedLine.CumulativeClaimed, previousByLine, seedLine.ValuationLineItemId)
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

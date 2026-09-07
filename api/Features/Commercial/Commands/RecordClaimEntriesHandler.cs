using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

// Batched RecordClaimEntry: upserts many lines' % complete on one claim in a single save.
// Same maths per line as the single-entry handler — cumulative = % x line amount, period
// increment = cumulative minus the line's cumulative on the claim immediately before
// (ClaimPeriodBaseline, the one rule) — with the baselines fetched once for the batch.
public sealed class RecordClaimEntriesHandler : ICommandHandler<RecordClaimEntries, IReadOnlyList<ClaimLine>>
{
    private readonly JpmsContext context;
    public RecordClaimEntriesHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ClaimLine>> HandleAsync(RecordClaimEntries command, CancellationToken cancellationToken)
    {
        var claim = await context.ValuationClaims.FindAsync(new object?[] { command.ValuationClaimId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Valuation claim {command.ValuationClaimId} was not found.");

        var linesById = await context.ValuationLineItems
            .Where(line => line.ProjectId == claim.ProjectId)
            .ToDictionaryAsync(line => line.ValuationLineItemId,
                line => (line.LineAmount, line.ElementType), cancellationToken);

        // Cumulative claimed per line on the claim immediately before this one — the same
        // rule as RecordClaimEntryHandler, fetched once for the whole batch.
        var previousByLine = await ClaimPeriodBaseline.PreviousCumulativeByLineAsync(
            context, claim.ProjectId, claim.ClaimNumber, cancellationToken);

        var existingByLine = await context.ClaimLines
            .Where(line => line.ValuationClaimId == command.ValuationClaimId)
            .ToDictionaryAsync(line => line.ValuationLineItemId, cancellationToken);

        var results = new List<ClaimLineEntity>(command.Entries.Count);
        foreach (var input in command.Entries)
        {
            if (!linesById.TryGetValue(input.ValuationLineItemId, out var lineInfo))
                throw new KeyNotFoundException($"Valuation line item {input.ValuationLineItemId} was not found on this project.");

            // Same stance as the single-entry handler: no per-line range rule beyond the
            // validation's +/-100000 rail (see RecordClaimEntryHandler).

            var cumulativeClaimed = ValuationCalculations.CumulativeClaimed(input.PercentComplete, lineInfo.LineAmount);

            if (!existingByLine.TryGetValue(input.ValuationLineItemId, out var entity))
            {
                entity = new ClaimLineEntity
                {
                    ClaimLineId = CommercialIdentifierFactory.NextClaimLineId(),
                    ValuationClaimId = command.ValuationClaimId,
                    ValuationLineItemId = input.ValuationLineItemId
                };
                context.ClaimLines.Add(entity);
                existingByLine[input.ValuationLineItemId] = entity;
            }

            entity.PercentComplete = input.PercentComplete;
            entity.CumulativeClaimed = cumulativeClaimed;
            entity.PeriodIncrement = ClaimPeriodBaseline.PeriodIncrement(cumulativeClaimed, previousByLine, input.ValuationLineItemId);
            results.Add(entity);
        }

        await context.SaveChangesAsync(cancellationToken);
        return results.Select(entity => entity.ToModel()).ToList();
    }
}

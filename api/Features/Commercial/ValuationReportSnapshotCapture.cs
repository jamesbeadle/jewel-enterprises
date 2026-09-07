using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial;

/// <summary>
/// Freezes an immutable, line-level copy of a project's valuation report as it stands right now:
/// every priced line with the % complete / cumulative claimed from the project's latest claim
/// (missing entries count as 0%), plus the summary footer with "Certified to date" stamped from
/// the Issued+Paid valuation invoices that came before that claim (never its own — the one rule,
/// CertifiedBeforeClaim). Values are copied, never referenced — later edits or deletions of live
/// lines must not disturb what was submitted to the client.
///
/// <see cref="CaptureAsync"/> adds the snapshot and its lines to the change tracker but does NOT
/// save; callers (invoice raise, submission/issue re-freezes after an amendment, on-demand
/// capture) save in their own transaction. When the snapshot backs an invoice, any earlier
/// snapshots for the same invoice are flagged superseded in the same save.
/// <see cref="ComputeAsync"/> is the read-only half: the same maths producing the same entities
/// without touching the change tracker — the draft (working-copy) PDF renders from it so the
/// preview and the eventual snapshot can never disagree.
/// </summary>
internal static class ValuationReportSnapshotCapture
{
    public static async Task<ValuationReportSnapshotEntity> CaptureAsync(
        JpmsContext context,
        string projectId,
        string label,
        string? valuationInvoiceId,
        CancellationToken cancellationToken)
    {
        var (snapshot, lines) = await ComputeAsync(context, projectId, label, valuationInvoiceId, cancellationToken);

        foreach (var line in lines)
            context.ValuationReportSnapshotLines.Add(line);
        context.ValuationReportSnapshots.Add(snapshot);

        if (valuationInvoiceId is not null)
        {
            var earlier = await context.ValuationReportSnapshots
                .Where(s => s.ValuationInvoiceId == valuationInvoiceId && !s.IsSuperseded)
                .ToListAsync(cancellationToken);
            foreach (var previous in earlier.Where(s => s.ValuationReportSnapshotId != snapshot.ValuationReportSnapshotId))
                previous.IsSuperseded = true;
        }

        return snapshot;
    }

    /// <summary>
    /// Computes the snapshot a capture WOULD freeze right now — same figures, same line order —
    /// without adding anything to the change tracker. Nothing here persists: the entities exist
    /// only to be mapped/rendered (the working-copy PDF) or handed to <see cref="CaptureAsync"/>.
    /// </summary>
    public static async Task<(ValuationReportSnapshotEntity Snapshot, List<ValuationReportSnapshotLineEntity> Lines)> ComputeAsync(
        JpmsContext context,
        string projectId,
        string label,
        string? valuationInvoiceId,
        CancellationToken cancellationToken)
    {
        var lines = await context.ValuationLineItems
            .Where(line => line.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        // The client's schedule-of-works references, frozen onto each line now: the line's own
        // reference when it carries one ("1.03"), else the per-cost-centre map's — the PDF prints
        // what was known at capture, and a later remap never rewrites an issued statement.
        var clientReferencesByCostCode = await ClientReferencesByCostCodeAsync(context, projectId, cancellationToken);

        // The figures come from the latest claim (highest number), whatever its status —
        // that is what the report tab shows and what a submission is asking to be paid for.
        var claim = await context.ValuationClaims
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.ClaimNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var entriesByLineItem = claim is null
            ? new Dictionary<string, ClaimLineEntity>()
            : await context.ClaimLines
                .Where(entry => entry.ValuationClaimId == claim.ValuationClaimId)
                .ToDictionaryAsync(entry => entry.ValuationLineItemId, cancellationToken);

        // "Previous" and "this period" are derived here from the claim immediately before, by
        // the one rule (ClaimPeriodBaseline) — never copied from the entry's stored increment,
        // which is a convenience written when the % was entered and can lag what the claim
        // before it finally said. The statement must show the movement since the last
        // statement, whatever that statement's payment status.
        var previousByLine = claim is null
            ? new Dictionary<string, decimal>()
            : await ClaimPeriodBaseline.PreviousCumulativeByLineAsync(
                context, projectId, claim.ClaimNumber, cancellationToken);

        // Gross certification (issued/paid cash amounts plus their embedded deposit credits) of
        // the invoices that came BEFORE this claim — never the claim's own, so a working copy or
        // a re-freeze taken after its invoice has been issued still reads as the statement.
        var certification = await CertifiedBeforeClaim.ForAsync(
            context, projectId, claim?.ClaimNumber, cancellationToken);
        var certifiedToDate = certification.CertifiedToDate;
        var depositCreditedToDate = certification.DepositCreditedToDate;

        var lineModels = lines.Select(line => line.ToModel()).ToList();
        var contractSum = ValuationCalculations.ContractSum(lineModels);
        var netVariations = ValuationCalculations.NetVariations(lineModels);

        // The next per-project number — the stem of the snapshot's mailbox tag
        // ("JPMS/VRS-{projectRef}-{Number}"), so triage can associate emails with THIS
        // snapshot. Max + 1 over the saved register: captures are one per command, so a
        // same-save collision cannot arise in practice. (If the latest snapshot is deleted
        // its number CAN be re-minted — deletes are for snapshots taken in error, before
        // anything is linked to them.)
        var number = await context.ValuationReportSnapshots
            .Where(s => s.ProjectId == projectId)
            .Select(s => (int?)s.Number)
            .MaxAsync(cancellationToken) ?? 0;

        var snapshot = new ValuationReportSnapshotEntity
        {
            ValuationReportSnapshotId = CommercialIdentifierFactory.NextValuationReportSnapshotId(),
            ProjectId = projectId,
            Number = number + 1,
            ValuationInvoiceId = valuationInvoiceId,
            ValuationClaimId = claim?.ValuationClaimId,
            Label = label,
            TakenAt = DateTimeOffset.UtcNow,
            IsSuperseded = false,
            ContractSum = contractSum,
            NetVariations = netVariations,
            RevisedContractSum = ValuationCalculations.RevisedContractSum(contractSum, netVariations),
            RetentionPercent = claim?.RetentionPercent ?? 0m,
            RetentionReleasePercent = claim?.RetentionReleasePercent ?? 0m,
            // RetentionReleased is computed below once works complete is known — the claim's
            // release % (non-zero only after practical completion) of works complete,
            // mirroring ValuationClaimSummary.
            DepositPercent = claim?.DepositPercent ?? 0m,
            CertifiedToDate = certifiedToDate
        };

        var snapshotLines = new List<ValuationReportSnapshotLineEntity>(lines.Count);
        var totalWorksComplete = 0m;
        // Contract-side works only (variations excluded) — the base the deposit releases against.
        var nonVariationWorksComplete = 0m;
        var displayOrder = 0;
        // Variation lines group by their V-ref (natural numeric order) before display order,
        // matching the live report table — a line added to an earlier variation later on
        // must not drop to the bottom of the client-facing statement.
        static int VariationRefOrder(string variationRef)
        {
            var digits = new string(variationRef.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var number) ? number : int.MaxValue;
        }
        foreach (var line in lines
            .OrderBy(line => line.ElementType)
            .ThenBy(line => line.ElementType == (int)ValuationElementType.Variation
                ? VariationRefOrder(line.VariationRef) : 0)
            .ThenBy(line => line.DisplayOrder))
        {
            entriesByLineItem.TryGetValue(line.ValuationLineItemId, out var entry);
            var snapshotLine = new ValuationReportSnapshotLineEntity
            {
                ValuationReportSnapshotLineId = CommercialIdentifierFactory.NextValuationReportSnapshotLineId(),
                ValuationReportSnapshotId = snapshot.ValuationReportSnapshotId,
                SourceValuationLineItemId = line.ValuationLineItemId,
                ElementType = line.ElementType,
                SectionCode = line.SectionCode,
                SectionName = line.SectionName,
                VariationRef = line.VariationRef,
                VariationTitle = line.VariationTitle,
                LineType = line.LineType,
                CostCode = line.CostCode,
                Description = line.Description,
                Unit = line.Unit,
                Quantity = line.Quantity,
                Rate = line.Rate,
                LineAmount = line.LineAmount,
                PercentComplete = entry?.PercentComplete ?? 0m,
                CumulativeClaimed = entry?.CumulativeClaimed ?? 0m,
                PeriodIncrement = ClaimPeriodBaseline.PeriodIncrement(
                    entry?.CumulativeClaimed ?? 0m, previousByLine, line.ValuationLineItemId),
                Comments = line.Comments,
                DisplayOrder = displayOrder++,
                ClientReference = !string.IsNullOrWhiteSpace(line.ClientReference)
                    ? line.ClientReference
                    : clientReferencesByCostCode.GetValueOrDefault(line.CostCode, "")
            };
            // Declined/TBC lines are recorded but never priced into totals — keep the
            // footer reconciling with the viewer's per-section sums.
            var countsTowardTotals = snapshotLine.LineType is not ((int)ValuationLineType.Declined or (int)ValuationLineType.Tbc);
            if (countsTowardTotals)
            {
                totalWorksComplete += snapshotLine.CumulativeClaimed;
                if (snapshotLine.ElementType != (int)ValuationElementType.Variation)
                    nonVariationWorksComplete += snapshotLine.CumulativeClaimed;
            }
            snapshotLines.Add(snapshotLine);
        }

        snapshot.TotalWorksComplete = totalWorksComplete;
        snapshot.RetentionHeld = ValuationCalculations.RetentionHeld(totalWorksComplete, snapshot.RetentionPercent);
        snapshot.RetentionReleased = ValuationCalculations.RetentionReleased(totalWorksComplete, snapshot.RetentionReleasePercent);
        // Cash-up-front deposit credit still to be taken: release earned against
        // contract-side works (capped at the deposit received), less the opening balance
        // settled before tracking, less credits already embedded in issued/paid invoices —
        // mirrors ValuationClaimSummary.
        snapshot.DepositReleased = ValuationCalculations.DepositDeduction(
            ValuationCalculations.DepositReleased(
                nonVariationWorksComplete, snapshot.DepositPercent,
                ValuationCalculations.DepositReceived(contractSum, snapshot.DepositPercent)),
            claim?.DepositReleasedOpening ?? 0m, depositCreditedToDate);
        snapshot.PaymentDueExVat = ValuationCalculations.PaymentDueExVat(
            totalWorksComplete, snapshot.RetentionHeld, snapshot.RetentionReleased,
            snapshot.DepositReleased, certifiedToDate);

        return (snapshot, snapshotLines);
    }

    // Grouped rather than ToDictionary so a code duplicated by case alone can never turn a
    // capture (or a working-copy download) into a 500.
    private static async Task<Dictionary<string, string>> ClientReferencesByCostCodeAsync(
        JpmsContext context, string projectId, CancellationToken cancellationToken)
    {
        var references = await context.ClientCostReferences.AsNoTracking()
            .Where(reference => reference.ProjectId == projectId)
            .Select(reference => new { reference.CostCode, reference.ClientReference })
            .ToListAsync(cancellationToken);
        return references
            .GroupBy(reference => reference.CostCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().ClientReference, StringComparer.OrdinalIgnoreCase);
    }
}

using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

public sealed partial class SetXeroAllocationHandler
{
    private Task ValidateAllocateAsync(Batch batch, CancellationToken cancellationToken) =>
        batch.Splits is null
            ? ValidateWholeLineAllocationAsync(batch, cancellationToken)
            : ValidateSplitAllocationAsync(batch, batch.Splits, cancellationToken);

    private async Task ValidateWholeLineAllocationAsync(Batch batch, CancellationToken cancellationToken)
    {
        var projectExists = await context.Projects
            .AnyAsync(project => project.ProjectId == batch.SingleProject, cancellationToken);
        if (!projectExists)
            throw new InvalidOperationException("Choose a project before allocating.");

        var costCenterActive = await context.CostCenters
            .AnyAsync(centre => centre.Code == batch.SingleCode && centre.IsActive, cancellationToken);
        if (!costCenterActive)
            throw new InvalidOperationException("Choose an active cost centre before allocating.");

        // A one-entry Splits list collapsed to this path — its amount must still
        // describe the whole of exactly one line, or the caller's intent is off.
        if (batch.Command.Splits is not { Count: 1 } single) return;
        if (batch.Lines.Count != 1)
            throw new InvalidOperationException("A split applies to one line at a time.");
        if (single[0].Net != batch.Lines[0].Net)
            throw new InvalidOperationException(
                $"The split must add up to the line's net of {batch.Lines[0].Net:0.00} — it currently adds up to {single[0].Net:0.00}.");
    }

    /// <summary>Split allocations are line-specific (the amounts belong to one line's net).</summary>
    private async Task ValidateSplitAllocationAsync(Batch batch, IReadOnlyList<XeroCostSplit> splits, CancellationToken cancellationToken)
    {
        if (batch.Lines.Count != 1)
            throw new InvalidOperationException("A split applies to one line at a time.");
        if (splits.Any(split => split.Net <= 0m))
            throw new InvalidOperationException("Every split amount must be greater than zero.");
        if (splits.Any(split => string.IsNullOrWhiteSpace(split.ProjectId)))
            throw new InvalidOperationException("Every split row needs a project.");

        var pairs = splits.Select(split => $"{split.ProjectId}:{split.CostCenterCode}").ToList();
        if (pairs.Distinct(StringComparer.OrdinalIgnoreCase).Count() != pairs.Count)
            throw new InvalidOperationException("Each project + cost centre combination can appear only once in a split.");

        await RequireKnownProjectsAsync(splits, cancellationToken);
        await RequireActiveCentresAsync(splits, cancellationToken);

        var line = batch.Lines[0];
        var total = splits.Sum(split => split.Net);
        if (total != line.Net)
            throw new InvalidOperationException(
                $"The split must add up to the line's net of {line.Net:0.00} — it currently adds up to {total:0.00}.");
    }

    private async Task RequireKnownProjectsAsync(IReadOnlyList<XeroCostSplit> splits, CancellationToken cancellationToken)
    {
        var projectIds = splits.Select(split => split.ProjectId!).Distinct().ToList();
        var knownProjects = await context.Projects
            .Where(project => projectIds.Contains(project.ProjectId))
            .Select(project => project.ProjectId)
            .ToListAsync(cancellationToken);
        var unknownProjects = projectIds.Except(knownProjects).ToList();
        if (unknownProjects.Count > 0)
            throw new InvalidOperationException($"Not a known project: {string.Join(", ", unknownProjects)}.");
    }

    private async Task RequireActiveCentresAsync(IReadOnlyList<XeroCostSplit> splits, CancellationToken cancellationToken)
    {
        var codes = splits.Select(split => split.CostCenterCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var activeCodes = await context.CostCenters
            .Where(centre => centre.IsActive && codes.Contains(centre.Code))
            .Select(centre => centre.Code)
            .ToListAsync(cancellationToken);
        var inactive = codes.Except(activeCodes, StringComparer.OrdinalIgnoreCase).ToList();
        if (inactive.Count > 0)
            throw new InvalidOperationException($"Not an active cost centre: {string.Join(", ", inactive)}.");
    }
}

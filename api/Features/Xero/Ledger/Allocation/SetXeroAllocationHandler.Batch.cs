using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

public sealed partial class SetXeroAllocationHandler
{
    /// <summary>The command as the run reads it: the lines, the split resolved (a one-entry
    /// "split" collapsed to a whole-line allocation), one stamp for the batch, and what the
    /// per-line pass notes for the steps after it.</summary>
    private sealed record Batch(
        SetXeroAllocation Command,
        List<XeroLedgerLineEntity> Lines,
        IReadOnlyList<XeroCostSplit>? Splits,
        string? SingleProject,
        string? SingleCode,
        DateTimeOffset Now)
    {
        public XeroAllocationAction Action => Command.Action;
        public List<string> LinesKeepingLinks { get; } = new();
        public List<string> ApprovedSiteRewrites { get; } = new();

        /// <summary>A split spanning projects has no single line-level project; the common one
        /// is kept when there is one so lists and summaries can still show it directly.</summary>
        public string? CommonSplitProject => Splits?.Select(split => split.ProjectId!).Distinct().Count() == 1 ? Splits[0].ProjectId : null;

        public static Batch For(SetXeroAllocation command, List<XeroLedgerLineEntity> lines)
        {
            // Resolve each share's project up front (a share without one falls back to the
            // command's project) and collapse a one-entry "split" to a whole-line allocation.
            var resolved = ResolveSplits(command);
            return new Batch(command, lines,
                resolved is { Count: > 1 } ? resolved : null,
                resolved is { Count: 1 } ? resolved[0].ProjectId : command.ProjectId,
                resolved is { Count: 1 } ? resolved[0].CostCenterCode : command.CostCenterCode,
                DateTimeOffset.UtcNow);
        }
    }

    /// <summary>
    /// Fills each share's project from the command-level fallback. Returns null for
    /// "no splits supplied"; a share left without any project surfaces in validation.
    /// </summary>
    private static IReadOnlyList<XeroCostSplit>? ResolveSplits(SetXeroAllocation command)
    {
        if (command.Splits is null || command.Splits.Count == 0) return null;
        return command.Splits
            .Select(split => split with { ProjectId = split.ProjectId ?? command.ProjectId })
            .ToList();
    }
}

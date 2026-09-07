using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

public sealed partial class SetXeroAllocationHandler
{
    /// <summary>
    /// Whatever the action, the previous split rows no longer describe these lines —
    /// EXCEPT a discussion message, which changes nothing about the allocation and
    /// must not quietly delete anything (the caller skips this for it). Reconciled in
    /// place rather than delete-and-re-add: EF's identity map refuses a new instance
    /// whose key matches a deleted-but-tracked row, and a re-cut split commonly keeps
    /// some of the same project + centre combinations (same keys).
    /// </summary>
    private async Task ReconcileSplitsAsync(Batch batch, List<string> ids, CancellationToken cancellationToken)
    {
        var oldSplits = await context.XeroCostSplits
            .Where(split => ids.Contains(split.XeroLedgerLineId))
            .ToListAsync(cancellationToken);
        var lineId = batch.Lines[0].XeroLedgerLineId;

        var desiredSplits = new Dictionary<string, XeroCostSplit>(StringComparer.OrdinalIgnoreCase);
        if (batch.Action == XeroAllocationAction.Allocate && batch.Splits is not null)
            foreach (var split in batch.Splits)
                desiredSplits[$"{lineId}:{split.ProjectId}:{split.CostCenterCode}"] = split; // splits ⇒ exactly one line (validated above)

        foreach (var oldSplit in oldSplits)
        {
            if (desiredSplits.TryGetValue(oldSplit.XeroCostSplitId, out var kept))
            {
                oldSplit.Net = kept.Net;
                desiredSplits.Remove(oldSplit.XeroCostSplitId);
            }
            else
            {
                context.XeroCostSplits.Remove(oldSplit);
            }
        }
        foreach (var (key, split) in desiredSplits)
            context.XeroCostSplits.Add(new XeroCostSplitEntity
            {
                XeroCostSplitId = key,
                XeroLedgerLineId = lineId,
                ProjectId = split.ProjectId!,
                CostCenterCode = split.CostCenterCode,
                Net = split.Net
            });
    }
}

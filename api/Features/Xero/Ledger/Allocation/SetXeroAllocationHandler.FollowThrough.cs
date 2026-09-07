using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

public sealed partial class SetXeroAllocationHandler
{
    /// <summary>The thread: Dispute's opening message (when one was written) and every
    /// AddDisputeMessage become rows, stamped with the signed-in user.</summary>
    private void AddThreadMessages(Batch batch)
    {
        var command = batch.Command;
        var hasNote = !string.IsNullOrWhiteSpace(command.Note);
        var opensThread = command.Action == XeroAllocationAction.Dispute && hasNote;
        if (!opensThread && command.Action != XeroAllocationAction.AddDisputeMessage) return;
        var body = command.Note!.Trim();
        foreach (var line in batch.Lines)
            context.XeroDisputeMessages.Add(new XeroDisputeMessageEntity
            {
                XeroDisputeMessageId = $"XDM-{Guid.NewGuid():N}",
                XeroLedgerLineId = line.XeroLedgerLineId,
                Author = command.AllocatedBy ?? "",
                Body = body,
                SentAtUtc = batch.Now
            });
    }

    /// <summary>
    /// After the save on purpose: the allocation is the record; Xero's answer is stamped
    /// onto it. Every write here is best-effort — the saved coding stands whatever Xero says.
    /// </summary>
    private async Task FollowThroughToXeroAsync(Batch batch, CancellationToken cancellationToken)
    {
        var lines = batch.Lines;
        if (lines.Count == 0) return;
        switch (batch.Action)
        {
            case XeroAllocationAction.Allocate:
                // Confirm-and-approve any draft invoice these allocations completed.
                await writeBack.TryWriteBackAsync(lines.Select(line => line.XeroInvoiceId).Distinct().ToList(), cancellationToken);
                // Approved bills skip the write-back (nothing to approve) — but a line that
                // just moved project still gets its Sites tracking rewritten in Xero.
                if (batch.ApprovedSiteRewrites.Count > 0)
                    await writeBack.TrySetSiteAsync(batch.ApprovedSiteRewrites, cancellationToken);
                break;
            case XeroAllocationAction.SetProject:
                // A set project writes its Site tracking without approving. Disputed lines
                // are the exception: nothing is agreed until resolution, so Xero waits.
                await writeBack.TrySetSiteAsync(
                    lines.Where(line => line.AllocationStatus == (int)XeroAllocationStatus.Unallocated)
                         .Select(line => line.XeroLedgerLineId).ToList(), cancellationToken);
                break;
            case XeroAllocationAction.ResolveDispute:
                // Resolution is when the agreed project follows through to Xero's Sites
                // tracking; paid bills stay untouched.
                await writeBack.TrySetSiteAsync(
                    lines.Where(line => line.ProjectId is not null)
                         .Select(line => line.XeroLedgerLineId).ToList(), cancellationToken);
                break;
        }
    }
}

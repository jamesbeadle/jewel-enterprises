using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

/// <summary>
/// Applies one allocation action to a batch of ledger lines. Returns how many
/// lines were updated. AllocatedBy arrives stamped by the endpoint from the
/// signed-in user.
///
/// Allocate carries either one project + cost centre for the whole batch or —
/// for a single line — a split across several shares, each with its own
/// project and cost centre, whose nets must sum exactly to the line's net.
/// Any change of allocation replaces the line's previous split rows.
///
/// After the allocation saves, any draft/submitted invoice whose stored lines
/// are now all allocated is confirmed back to Xero (tracking + approval) —
/// best-effort: the allocation stands even when Xero says no, and the outcome
/// is stamped on the lines.
///
/// A line that keeps its work-order links through a re-allocation (whole-line,
/// same project, new centre) drags the linked orders with it: they are recoded
/// wholesale to the new centre, because the invoice drives the work order's
/// coding (see WorkOrderInvoiceRecoding).
///
/// The dispute trio (2026-08-14): Dispute parks queued/allocated lines in the
/// Disputed bucket with an optional opening message; AddDisputeMessage appends
/// to the thread and touches nothing else; ResolveDispute returns lines to the
/// queue keeping the agreed coding and writes the agreed Site to Xero. Set (and
/// its saved cost centre) works on disputed lines too, Xero deferred to
/// resolution.
///
/// One partial per concern: Guards (the refusals), AllocateValidation, Splits
/// (reconciling the split rows), Apply (what each action leaves on a line),
/// Links (work-order links and package slices), FollowThrough (the dispute
/// thread and the Xero writes after the save).
/// </summary>
public sealed partial class SetXeroAllocationHandler : ICommandHandler<SetXeroAllocation, int>
{
    private readonly JpmsContext context;
    private readonly IXeroWriteBackService writeBack;

    public SetXeroAllocationHandler(JpmsContext context, IXeroWriteBackService writeBack)
    {
        this.context = context;
        this.writeBack = writeBack;
    }

    public async Task<int> HandleAsync(SetXeroAllocation command, CancellationToken cancellationToken)
    {
        var ids = command.XeroLedgerLineIds.Distinct().ToList();
        var lines = await context.XeroLedgerLines
            .Where(line => ids.Contains(line.XeroLedgerLineId))
            .ToListAsync(cancellationToken);
        var batch = Batch.For(command, lines);

        await GuardAsync(batch, cancellationToken);
        if (command.Action != XeroAllocationAction.AddDisputeMessage)
            await ReconcileSplitsAsync(batch, ids, cancellationToken);

        foreach (var line in lines)
        {
            var previousProjectId = line.ProjectId;
            ApplyToLine(batch, line);
            NoteApprovedSiteMove(batch, line, previousProjectId);
            await KeepOrClearLinksAsync(batch, line, previousProjectId, cancellationToken);
        }
        await RecodeLinkedOrdersAsync(batch, cancellationToken);
        AddThreadMessages(batch);

        await context.SaveChangesAsync(cancellationToken);
        await FollowThroughToXeroAsync(batch, cancellationToken);
        return lines.Count;
    }
}

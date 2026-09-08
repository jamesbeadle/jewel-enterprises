using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Commercial;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

public sealed partial class SetXeroAllocationHandler
{
    /// <summary>
    /// A re-allocation that moves an approved bill's line to another project must carry the
    /// move to Xero too — the Sites tracking is what the accountant reads, and a change of
    /// mind after approval is legitimate (decision 2026-08-14). Draft/submitted bills take
    /// the full write-back instead; paid bills are locked in Xero and move portal-side only
    /// (the write-back service skips them). Multi-project splits have no line-level project
    /// to site (ProjectId null), and an unchanged project needs no rewrite.
    /// </summary>
    private static void NoteApprovedSiteMove(Batch batch, XeroLedgerLineEntity line, string? previousProjectId)
    {
        if (batch.Action == XeroAllocationAction.Allocate
            && line.InvoiceStatus.Equals("AUTHORISED", StringComparison.OrdinalIgnoreCase)
            && line.ProjectId is not null
            && !string.Equals(line.ProjectId, previousProjectId, StringComparison.OrdinalIgnoreCase))
            batch.ApprovedSiteRewrites.Add(line.XeroLedgerLineId);
    }

    /// <summary>
    /// Work-order link slices describe a line allocated to the order's project — whole, or
    /// (since 2026-09-08, the Work Order bill against a multi-code order) split across centres
    /// on that ONE project. Moving the line to another project, splitting it across projects,
    /// bucketing, ignoring, disputing or resetting it orphans the slices — clear them so the
    /// orders' invoiced balances never count a line that left them. Re-cutting the centres
    /// within the same project keeps the links; only a whole-line move drags the orders'
    /// coding with it (a split has no single centre to recode to). A discussion message moves
    /// nothing and touches nothing.
    /// </summary>
    private async Task KeepOrClearLinksAsync(Batch batch, XeroLedgerLineEntity line, string? previousProjectId, CancellationToken cancellationToken)
    {
        if (batch.Action == XeroAllocationAction.AddDisputeMessage) return;
        var staysOnProject = batch.Action == XeroAllocationAction.Allocate
            && line.ProjectId is not null
            && string.Equals(line.ProjectId, previousProjectId, StringComparison.OrdinalIgnoreCase);
        if (staysOnProject)
        {
            if (line.CostCenterCode is not null) batch.LinesKeepingLinks.Add(line.XeroLedgerLineId);
            return;
        }
        var orphanedLinks = await context.XeroLineWorkOrderLinks
            .Where(link => link.XeroLedgerLineId == line.XeroLedgerLineId)
            .ToListAsync(cancellationToken);
        context.XeroLineWorkOrderLinks.RemoveRange(orphanedLinks);

        // Package cost slices describe the same whole-line allocation, so they orphan under
        // exactly the same moves — clear them with the links.
        var orphanedPackageCosts = await context.ReconciliationPackageCostLines
            .Where(slice => slice.XeroLedgerLineId == line.XeroLedgerLineId)
            .ToListAsync(cancellationToken);
        context.ReconciliationPackageCostLines.RemoveRange(orphanedPackageCosts);
    }

    /// <summary>
    /// The invoice drives the work order's coding: a linked line moving between cost centres
    /// within its project keeps its links, so the orders it pays are recoded wholesale to the
    /// new centre — allocation and committed value never drift apart. Only the whole-batch
    /// path reaches here (splits null the centre, which clears the links instead), so
    /// SingleCode is the centre every kept line now carries.
    /// </summary>
    private async Task RecodeLinkedOrdersAsync(Batch batch, CancellationToken cancellationToken)
    {
        if (batch.LinesKeepingLinks.Count == 0) return;
        var kept = batch.LinesKeepingLinks;
        var linkedOrderIds = await context.XeroLineWorkOrderLinks
            .Where(link => kept.Contains(link.XeroLedgerLineId))
            .Select(link => link.WorkOrderId)
            .Distinct()
            .ToListAsync(cancellationToken);
        await WorkOrderInvoiceRecoding.RecodeOrdersToCentreAsync(context, linkedOrderIds, batch.SingleCode!, cancellationToken);
    }
}

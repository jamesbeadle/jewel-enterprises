using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger.WorkOrderBills;

/// <summary>
/// Reverses a Work Order bill approval (2026-09-08) in one save: every line of the bill back to
/// Unallocated, its split rows, work-order links and package cost slices removed, every order's
/// approval row marked undone — then the Sites and Cost Code tracking cleared off the bill in Xero and
/// the audit row written. Xero never un-approves, so a bill approved there stays approved; the
/// outcome carries the bill's Xero status so the page can say exactly that.
///
/// Refused when no approval stands, when any line has since been re-allocated by hand (the
/// undo would silently throw that decision away), and when the bill is paid (Xero locks it,
/// and a paid cost is not one to put back in the queue).
/// </summary>
public sealed class UndoWorkOrderBillApprovalHandler : ICommandHandler<UndoWorkOrderBillApproval, WorkOrderBillUndoOutcome>
{
    private readonly JpmsContext context;
    private readonly IXeroWriteBackService writeBack;
    private readonly AuditTrail audit;

    public UndoWorkOrderBillApprovalHandler(JpmsContext context, IXeroWriteBackService writeBack, AuditTrail audit)
    {
        this.context = context;
        this.writeBack = writeBack;
        this.audit = audit;
    }

    public async Task<WorkOrderBillUndoOutcome> HandleAsync(UndoWorkOrderBillApproval command, CancellationToken cancellationToken)
    {
        var approvals = await context.WorkOrderBillApprovals
            .Where(row => row.XeroInvoiceId == command.XeroInvoiceId && row.UndoneAtUtc == null)
            .OrderByDescending(row => row.ApprovedAtUtc)
            .ToListAsync(cancellationToken);
        if (approvals.Count == 0) throw new InvalidOperationException("No Work Order bill approval stands on this bill.");
        var approval = approvals[0];
        var lines = await context.XeroLedgerLines
            .Where(line => line.XeroInvoiceId == command.XeroInvoiceId)
            .ToListAsync(cancellationToken);
        Guard(lines, approval);

        var ids = lines.Select(line => line.XeroLedgerLineId).ToList();
        context.XeroCostSplits.RemoveRange(await context.XeroCostSplits.Where(split => ids.Contains(split.XeroLedgerLineId)).ToListAsync(cancellationToken));
        context.XeroLineWorkOrderLinks.RemoveRange(await context.XeroLineWorkOrderLinks.Where(link => ids.Contains(link.XeroLedgerLineId)).ToListAsync(cancellationToken));
        context.ReconciliationPackageCostLines.RemoveRange(await context.ReconciliationPackageCostLines.Where(slice => ids.Contains(slice.XeroLedgerLineId)).ToListAsync(cancellationToken));
        foreach (var line in lines) ReturnToQueue(line);
        foreach (var row in approvals)
        {
            row.UndoneByEmail = command.UndoneBy ?? "";
            row.UndoneAtUtc = DateTimeOffset.UtcNow;
        }
        await context.SaveChangesAsync(cancellationToken);

        var xero = await writeBack.TryClearTrackingAsync(command.XeroInvoiceId, cancellationToken);
        var orderIds = approvals.Select(row => row.WorkOrderId).Distinct().ToList();
        var references = await context.WorkOrders.AsNoTracking()
            .Where(candidate => orderIds.Contains(candidate.WorkOrderId))
            .Select(candidate => candidate.Reference)
            .ToListAsync(cancellationToken);
        await audit.WriteAsync(
            AuditEventType.WorkOrderBillApprovalUndone,
            $"{lines[0].ContactName} {lines[0].InvoiceNumber} Work Order bill approval undone — {lines.Count} line(s) back to Unallocated, "
            + $"links to {string.Join(" + ", references.DefaultIfEmpty(approval.WorkOrderId))} removed. "
            + (xero.Succeeded ? $"Xero tracking cleared; the bill is {xero.XeroStatus} in Xero." : $"Xero: {xero.Error}"),
            projectId: approval.ProjectId,
            recordType: RecordType.WorkOrder,
            recordId: approval.WorkOrderId,
            recordReference: string.Join(" + ", references),
            cancellationToken: cancellationToken);
        return new WorkOrderBillUndoOutcome(lines.Count, xero.Succeeded, xero.Error, xero.XeroStatus);
    }

    private static void Guard(List<XeroLedgerLineEntity> lines, WorkOrderBillApprovalEntity approval)
    {
        if (lines.Count == 0)
            throw new InvalidOperationException("No stored ledger lines for this bill.");
        if (lines[0].InvoiceStatus.Equals("PAID", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The bill is paid in Xero — a paid cost stays allocated.");
        var reallocatedByHand = lines.Any(line =>
            line.AllocationStatus != (int)XeroAllocationStatus.Allocated
            || line.AllocatedAtUtc is null
            || line.AllocatedAtUtc > approval.ApprovedAtUtc.AddSeconds(1));
        if (reallocatedByHand)
            throw new InvalidOperationException(
                "A line of this bill has been re-allocated by hand since the approval — undo would throw that decision away. Reset the lines on the Allocated tab instead.");
    }

    /// <summary>Reset semantics, the same as the Allocated tab's Undo on one line.</summary>
    private static void ReturnToQueue(XeroLedgerLineEntity line)
    {
        line.AllocationStatus = (int)XeroAllocationStatus.Unallocated;
        line.ProjectId = null;
        line.CostCenterCode = null;
        line.Bucket = null;
        line.AllocatedBy = null;
        line.AllocatedAtUtc = null;
        line.Note = null;
        line.WriteBackStatus = (int)XeroWriteBackStatus.None;
        line.WriteBackError = null;
        line.WriteBackAtUtc = null;
    }
}

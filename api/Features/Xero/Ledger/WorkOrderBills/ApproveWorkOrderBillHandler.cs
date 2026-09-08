using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger.WorkOrderBills;

/// <summary>
/// The one action on a Work Order bill (2026-09-08): every line of the bill allocated to the
/// order's project and cost code(s) as the card showed them, every line linked to the order for
/// its full net, the approval recorded — one save — then the tracking confirmed to Xero and the
/// bill approved there through the existing write-back, and the audit row written.
///
/// The order drives the invoice here, the reverse of the WO Allocation tab's rule: the order's
/// lines are never recoded to the bill's centre (see WorkOrderInvoiceRecoding), because the
/// bill's coding CAME from the order. The match is re-run server-side before anything is
/// touched, so a client cannot approve a bill against an order the rule would not give it.
/// One partial per concern: Guards (the refusals), Apply (what each line is left with).
/// </summary>
public sealed partial class ApproveWorkOrderBillHandler : ICommandHandler<ApproveWorkOrderBill, WorkOrderBillApprovalOutcome>
{
    private readonly JpmsContext context;
    private readonly IXeroWriteBackService writeBack;
    private readonly AuditTrail audit;

    public ApproveWorkOrderBillHandler(JpmsContext context, IXeroWriteBackService writeBack, AuditTrail audit)
    {
        this.context = context;
        this.writeBack = writeBack;
        this.audit = audit;
    }

    public async Task<WorkOrderBillApprovalOutcome> HandleAsync(ApproveWorkOrderBill command, CancellationToken cancellationToken)
    {
        var lines = await context.XeroLedgerLines
            .Where(line => line.XeroInvoiceId == command.XeroInvoiceId)
            .ToListAsync(cancellationToken);
        GuardBill(command, lines);

        var match = await RequireMatchAsync(command, lines, cancellationToken);
        var order = await context.WorkOrders.FirstAsync(candidate => candidate.WorkOrderId == match.WorkOrderId, cancellationToken);
        var codings = await RequireCodingsAsync(command, lines, order, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        await ClearStaleSplitsAsync(lines, cancellationToken);
        foreach (var line in lines)
            Allocate(line, order, codings[line.XeroLedgerLineId], command.ApprovedBy ?? "", now);
        RecordApproval(command, order, match, lines, now);
        await context.SaveChangesAsync(cancellationToken);

        var xero = await writeBack.WriteBackWorkOrderBillAsync(command.XeroInvoiceId, cancellationToken);
        await audit.WriteAsync(
            AuditEventType.WorkOrderBillApproved,
            $"{BillLabel(lines[0])} approved as a Work Order bill against {order.Reference} — "
            + $"{lines.Count} line(s), £{BillNet(lines):N2}; {match.Detail} "
            + (xero.Succeeded ? "Approved in Xero." : $"Xero: {xero.Error}"),
            projectId: order.ProjectId,
            recordType: RecordType.WorkOrder,
            recordId: order.WorkOrderId,
            recordReference: order.Reference,
            cancellationToken: cancellationToken);
        return new WorkOrderBillApprovalOutcome(lines.Count, xero.Succeeded, xero.Error);
    }

    private static string BillLabel(XeroLedgerLineEntity line) =>
        $"{line.ContactName} {line.InvoiceNumber}".Trim();

    private static decimal BillNet(IEnumerable<XeroLedgerLineEntity> lines) =>
        lines.Sum(line => line.Type == "ACCPAYCREDIT" ? -line.Net : line.Net);
}

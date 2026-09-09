using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger.WorkOrderBills;

/// <summary>
/// The one action on a Work Order bill (2026-09-08): every line of the bill allocated to its
/// shares' projects and cost code(s) as the card showed them, every share linked to its order,
/// the approval recorded per order — one save — then the tracking confirmed to Xero and the
/// bill approved there through the existing write-back, and the audit row written. Since
/// 2026-09-09 a line's shares may sit on several of the supplier's open orders.
///
/// The order drives the invoice here, the reverse of the WO Allocation tab's rule: the order's
/// lines are never recoded to the bill's centre (see WorkOrderInvoiceRecoding), because the
/// bill's coding CAME from the order. The match is re-run server-side before anything is
/// touched, so a client cannot approve a bill against an order that is not the supplier's, and
/// each order's slice is held to what is left to invoice on it. One partial per concern:
/// Guards (the refusals), Apply (what each line is left with).
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

        var match = await RequireMatchAsync(lines, cancellationToken);
        var orders = await RequireOrdersAsync(command, match, cancellationToken);
        var codings = await RequireCodingsAsync(command, lines, orders, cancellationToken);
        RequireEachOrderWithinValue(lines, codings, orders);

        var now = DateTimeOffset.UtcNow;
        await ClearStaleSplitsAsync(lines, cancellationToken);
        foreach (var line in lines)
            Allocate(line, codings[line.XeroLedgerLineId], orders, command.ApprovedBy ?? "", now);
        foreach (var order in orders.Values)
            RecordApproval(command, order, match, SliceNet(lines, codings, order.WorkOrderId), now);
        await context.SaveChangesAsync(cancellationToken);

        var references = orders.Values.Select(order => order.Reference).OrderBy(reference => reference).ToList();
        var xero = await writeBack.WriteBackWorkOrderBillAsync(command.XeroInvoiceId, cancellationToken);
        await audit.WriteAsync(
            AuditEventType.WorkOrderBillApproved,
            $"{BillLabel(lines[0])} approved as a Work Order bill against {string.Join(" + ", references)} — "
            + $"{lines.Count} line(s), £{BillNet(lines):N2}; {match.Detail} "
            + (xero.Succeeded ? "Approved in Xero." : $"Xero: {xero.Error}"),
            projectId: orders.Values.First().ProjectId,
            recordType: RecordType.WorkOrder,
            recordId: orders.Values.First().WorkOrderId,
            recordReference: string.Join(" + ", references),
            cancellationToken: cancellationToken);
        return new WorkOrderBillApprovalOutcome(lines.Count, references, xero.Succeeded, xero.Error);
    }

    private static string BillLabel(XeroLedgerLineEntity line) =>
        $"{line.ContactName} {line.InvoiceNumber}".Trim();

    private static decimal BillNet(IEnumerable<XeroLedgerLineEntity> lines) =>
        lines.Sum(SignedNet);

    private static decimal SignedNet(XeroLedgerLineEntity line) =>
        line.Type == "ACCPAYCREDIT" ? -line.Net : line.Net;

    /// <summary>How much of the bill one order takes: its shares, signed like the lines they come from.</summary>
    private static decimal SliceNet(
        List<XeroLedgerLineEntity> lines, Dictionary<string, IReadOnlyList<WorkOrderBillShare>> codings, string workOrderId) =>
        lines.Sum(line => codings[line.XeroLedgerLineId]
            .Where(share => share.WorkOrderId.Equals(workOrderId, StringComparison.OrdinalIgnoreCase))
            .Sum(share => line.Type == "ACCPAYCREDIT" ? -share.Net : share.Net));
}

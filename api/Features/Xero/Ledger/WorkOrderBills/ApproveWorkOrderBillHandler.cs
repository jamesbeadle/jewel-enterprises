using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger.WorkOrderBills;

/// <summary>
/// The one action on a Work Order bill (2026-09-08): the bill's net taken as a slice per order,
/// spread over the bill's lines and each order's cost codes (WorkOrderBillSliceSpread), every
/// line allocated accordingly, every order linked for its slice, the approval recorded per order
/// — one save — then each Xero line's tracking stamped whole and the bill approved there through
/// the existing write-back, and the audit row written. The Xero lines are never split
/// (2026-09-09, the accountant's ask: they are the supplier's CIS split, left as raised).
///
/// The order drives the invoice here, the reverse of the WO Allocation tab's rule: the order's
/// lines are never recoded to the bill's centre (see WorkOrderInvoiceRecoding), because the
/// bill's coding CAME from the order. The match is re-run server-side before anything is
/// touched, so a client cannot approve a bill against an order that is not the supplier's, the
/// slices must tie to the bill, and each order's slice is held to what is left to invoice on
/// it. One partial per concern: Guards (the refusals), Apply (what each line is left with).
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
        GuardBill(lines);

        var (match, recognition) = await RequireMatchAsync(lines, cancellationToken);
        var orders = await RequireOrdersAsync(command, match, cancellationToken);
        RequireSlicesTieToTheBill(command, lines);
        RequireEachOrderWithinValue(command, orders);
        var codings = WorkOrderBillSliceSpread.Spread(lines, command.Slices,
            orderId => recognition.CodeWeightsOf(orderId) ?? Array.Empty<KeyValuePair<string, decimal>>(),
            orderId => orders[orderId].ProjectId);

        var now = DateTimeOffset.UtcNow;
        await ClearStaleSplitsAsync(lines, cancellationToken);
        foreach (var line in lines)
            Allocate(line, codings[line.XeroLedgerLineId], orders, command.ApprovedBy ?? "", now);
        foreach (var slice in command.Slices)
            RecordApproval(command, orders[slice.WorkOrderId], match, slice.Net, now);
        await context.SaveChangesAsync(cancellationToken);

        var references = command.Slices.Select(slice => orders[slice.WorkOrderId].Reference).OrderBy(reference => reference).ToList();
        var xero = await writeBack.WriteBackWorkOrderBillAsync(command.XeroInvoiceId, cancellationToken);
        if (xero.Note is not null) await NoteTrackingNotWrittenAsync(lines, cancellationToken);
        await audit.WriteAsync(
            AuditEventType.WorkOrderBillApproved,
            $"{BillLabel(lines[0])} approved as a Work Order bill against {string.Join(" + ", references)} — "
            + $"{lines.Count} line(s), £{BillNet(lines):N2}; {match.Detail} "
            + (xero.Succeeded ? (xero.Note is null ? "Approved in Xero." : "Approved in Xero, no tracking written (the order split crosses the supplier's lines).") : $"Xero: {xero.Error}"),
            projectId: orders[command.Slices[0].WorkOrderId].ProjectId,
            recordType: RecordType.WorkOrder,
            recordId: command.Slices[0].WorkOrderId,
            recordReference: string.Join(" + ", references),
            cancellationToken: cancellationToken);
        return new WorkOrderBillApprovalOutcome(lines.Count, references, xero.Succeeded, xero.Error, xero.Note);
    }

    /// <summary>The Allocated row reads why the bill carries no tracking in Xero — on the line, where the note lives.</summary>
    private async Task NoteTrackingNotWrittenAsync(List<XeroLedgerLineEntity> lines, CancellationToken cancellationToken)
    {
        foreach (var line in lines) line.Note = $"{line.Note} · no Xero tracking (order split crosses the line)";
        await context.SaveChangesAsync(cancellationToken);
    }

    private static string BillLabel(XeroLedgerLineEntity line) =>
        $"{line.ContactName} {line.InvoiceNumber}".Trim();

    private static decimal BillNet(IEnumerable<XeroLedgerLineEntity> lines) =>
        lines.Sum(SignedNet);

    private static decimal SignedNet(XeroLedgerLineEntity line) =>
        line.Type == "ACCPAYCREDIT" ? -line.Net : line.Net;
}

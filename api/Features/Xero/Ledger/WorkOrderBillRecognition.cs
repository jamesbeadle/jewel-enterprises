using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Subcontractors;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

/// <summary>
/// Work Order bill recognition (2026-09-08, the accountant's ask), computed next to labour
/// recognition on every unallocated read: a bill whose supplier has an open work order was
/// decided when the order was approved — project, cost code(s) — so it belongs on the allocation
/// page's Work Order bills tab, pre-filled from the order, not in the queue for a hand decision.
///
/// The rules, per BILL (never per line): the labour registry wins (a worker's bill is settlement,
/// not a cost — the cover route); then the supplier is resolved to its directory company; then a
/// work-order number written on the bill picks the order, else a supplier with exactly one open
/// order matches on that alone; finally the bill must fit inside the order's remaining value. An
/// "open" order is Released with value still left to invoice (decision 2026-09-08). Anything that
/// does not match cleanly stays in the queue — with the reason on the row when an order was in
/// play, and silently when none was. One partial per concern: Rules (the decision), Splits (the
/// proposed coding and the wording).
/// </summary>
public sealed partial class WorkOrderBillRecognition
{
    private readonly Dictionary<string, List<OpenOrder>> ordersBySubcontractor;
    private readonly List<SupplierName> supplierNames;
    private readonly Dictionary<string, BillVerdict?> verdictByInvoice = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<OpenOrder>?> ordersByContactName = new(StringComparer.OrdinalIgnoreCase);

    private WorkOrderBillRecognition(
        Dictionary<string, List<OpenOrder>> ordersBySubcontractor, List<SupplierName> supplierNames)
    {
        this.ordersBySubcontractor = ordersBySubcontractor;
        this.supplierNames = supplierNames;
    }

    /// <summary>
    /// Builds the recogniser for a read, or null when no line in it is unallocated — like the
    /// suggester and labour recognition, the order and directory reads are skipped on every
    /// other tab.
    /// </summary>
    public static async Task<WorkOrderBillRecognition?> ForAsync(
        JpmsContext context, IReadOnlyList<XeroLedgerLineEntity> entities, CancellationToken cancellationToken)
    {
        if (!entities.Any(entity => entity.AllocationStatus == (int)XeroAllocationStatus.Unallocated))
            return null;

        var orders = await LoadOpenOrdersAsync(context, cancellationToken);
        var subcontractorIds = orders.Select(order => order.SubcontractorId).Distinct().ToList();
        var names = await LoadSupplierNamesAsync(context, subcontractorIds, cancellationToken);
        return new WorkOrderBillRecognition(
            orders.GroupBy(order => order.SubcontractorId, StringComparer.OrdinalIgnoreCase)
                  .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase),
            names);
    }

    /// <summary>
    /// Recognition for one unallocated line, decided once per bill and memoised.
    /// <paramref name="billLines"/> are every unallocated line of the same bill;
    /// <paramref name="isLabour"/> is labour recognition's answer for the bill;
    /// <paramref name="hintedProjectId"/> is the bill's site — the project set on it in the portal,
    /// else the one its Xero Sites tracking points at — the tie-break between orders that share a
    /// number on different projects.
    /// </summary>
    public LineVerdict? ForLine(
        XeroLedgerLineEntity line, IReadOnlyList<XeroLedgerLineEntity> billLines, bool isLabour, string? hintedProjectId)
    {
        if (line.AllocationStatus != (int)XeroAllocationStatus.Unallocated || billLines.Count == 0) return null;
        if (!verdictByInvoice.TryGetValue(line.XeroInvoiceId, out var verdict))
            verdictByInvoice[line.XeroInvoiceId] = verdict = Evaluate(billLines, isLabour, hintedProjectId);
        if (verdict is null) return null;
        return verdict.Order is null
            ? new LineVerdict(null, verdict.ExceptionReason)
            : new LineVerdict(MatchFor(verdict.Order, verdict.Rule, verdict.Detail!, line), null);
    }

    /// <summary>
    /// The supplier's open orders, or null when the bill's contact is nobody with one. The name
    /// rule is the directory's own (<see cref="DirectoryXeroMatcher"/> — the one "does this name
    /// mean that company" rule), so a bill matches exactly the record the import would suggest.
    /// </summary>
    private List<OpenOrder>? OrdersFor(string? contactName)
    {
        if (string.IsNullOrWhiteSpace(contactName)) return null;
        if (ordersByContactName.TryGetValue(contactName, out var cached)) return cached;
        var subcontractorIds = supplierNames
            .Where(supplier => DirectoryXeroMatcher.Matches(supplier.Name, contactName))
            .Select(supplier => supplier.SubcontractorId)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var orders = subcontractorIds
            .SelectMany(id => ordersBySubcontractor.TryGetValue(id, out var found) ? found : new List<OpenOrder>())
            .ToList();
        var result = orders.Count == 0 ? null : orders;
        ordersByContactName[contactName] = result;
        return result;
    }
}

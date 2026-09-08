using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Commercial;

/// <summary>One work order's share of one allocated purchase line, on the centre the invoice carries it.</summary>
public sealed record WorkOrderLinkSlice(string WorkOrderId, decimal Amount, string InvoiceCode);

/// <summary>
/// The work-order-linked spend of a project, slice by slice, each on the invoice's own centre —
/// what the financial summary subtracts from non-WO cost of sales and re-attributes to the
/// order's code mix. A whole-line link is one slice on the line's centre. Since 2026-09-08 a
/// link may also sit on a line split across the order's cost codes (a Work Order bill against a
/// multi-code order): that link becomes one slice per split share, the link's amount shared in
/// the split's own proportions, so every penny still lands on a centre.
/// </summary>
public static class WorkOrderLinkSlices
{
    public static async Task<List<WorkOrderLinkSlice>> ForProjectAsync(
        JpmsContext context, string projectId, CancellationToken cancellationToken)
    {
        var links = await context.XeroLineWorkOrderLinks.AsNoTracking()
            .Where(link => link.ProjectId == projectId)
            .Join(context.XeroLedgerLines,
                link => link.XeroLedgerLineId,
                line => line.XeroLedgerLineId,
                (link, line) => new { link.WorkOrderId, link.Amount, line.XeroLedgerLineId, line.CostCenterCode, line.Net })
            .ToListAsync(cancellationToken);

        var slices = links
            .Where(link => link.CostCenterCode != null)
            .Select(link => new WorkOrderLinkSlice(link.WorkOrderId, link.Amount, link.CostCenterCode!))
            .ToList();

        var splitLineIds = links.Where(link => link.CostCenterCode == null).Select(link => link.XeroLedgerLineId).Distinct().ToList();
        if (splitLineIds.Count == 0) return slices;

        var splitsByLine = (await context.XeroCostSplits.AsNoTracking()
                .Where(split => splitLineIds.Contains(split.XeroLedgerLineId))
                .ToListAsync(cancellationToken))
            .GroupBy(split => split.XeroLedgerLineId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var link in links.Where(link => link.CostCenterCode == null))
        {
            if (!splitsByLine.TryGetValue(link.XeroLedgerLineId, out var splits) || link.Net == 0m) continue;
            var shares = XeroSplitMaths.ProportionalShares(link.Amount, splits.Select(split => split.Net).ToList());
            for (var index = 0; index < splits.Count; index++)
                slices.Add(new WorkOrderLinkSlice(link.WorkOrderId, shares[index], splits[index].CostCenterCode));
        }
        return slices;
    }
}

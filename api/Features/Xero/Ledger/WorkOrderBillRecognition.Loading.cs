using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

public sealed partial class WorkOrderBillRecognition
{
    /// <summary>
    /// Every Released order with value still to invoice, with its lines folded into one weight
    /// per cost code (the pro rata split) and its invoiced-to-date summed off the link slices —
    /// the same figure the WO Allocation tab shows. Orders are few; read whole.
    /// </summary>
    private static async Task<List<OpenOrder>> LoadOpenOrdersAsync(JpmsContext context, CancellationToken cancellationToken)
    {
        var released = await context.WorkOrders.AsNoTracking()
            .Where(order => order.Status == (int)WorkOrderStatus.Released)
            .ToListAsync(cancellationToken);
        if (released.Count == 0) return new List<OpenOrder>();

        var orderIds = released.Select(order => order.WorkOrderId).ToList();
        var linesByOrder = (await context.WorkOrderLines.AsNoTracking()
                .Where(line => orderIds.Contains(line.WorkOrderId))
                .OrderBy(line => line.SortOrder)
                .ToListAsync(cancellationToken))
            .GroupBy(line => line.WorkOrderId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        var invoicedByOrder = (await context.XeroLineWorkOrderLinks.AsNoTracking()
                .Where(link => orderIds.Contains(link.WorkOrderId))
                .GroupBy(link => link.WorkOrderId)
                .Select(group => new { WorkOrderId = group.Key, Invoiced = group.Sum(link => link.Amount) })
                .ToListAsync(cancellationToken))
            .ToDictionary(row => row.WorkOrderId, row => row.Invoiced, StringComparer.OrdinalIgnoreCase);
        var projectIds = released.Select(order => order.ProjectId).Distinct().ToList();
        var projectNames = await context.Projects.AsNoTracking()
            .Where(project => projectIds.Contains(project.ProjectId))
            .ToDictionaryAsync(project => project.ProjectId, project => project.Name, cancellationToken);

        return released
            .Select(order => new OpenOrder(
                order.WorkOrderId,
                order.Reference,
                order.Number,
                order.Title,
                order.ProjectId,
                projectNames.TryGetValue(order.ProjectId, out var name) ? name : order.ProjectId,
                order.SubcontractorId,
                order.Value,
                invoicedByOrder.TryGetValue(order.WorkOrderId, out var invoiced) ? invoiced : 0m,
                CodeWeightsOf(linesByOrder.TryGetValue(order.WorkOrderId, out var lines) ? lines : new())))
            .Where(order => order.Remaining > 0m)
            .ToList();
    }

    private static IReadOnlyList<KeyValuePair<string, decimal>> CodeWeightsOf(List<Data.Entities.WorkOrderLineEntity> lines) =>
        lines.Where(line => !string.IsNullOrWhiteSpace(line.CostCode))
            .GroupBy(line => line.CostCode, StringComparer.OrdinalIgnoreCase)
            .Select(group => new KeyValuePair<string, decimal>(group.Key, group.Sum(line => line.LineTotal)))
            .ToList();

    /// <summary>
    /// The names a bill's Xero contact can be matched under, each with the directory record it
    /// means: the record's own company name, and the Xero contact name it was linked to (which
    /// is exact by construction). Only suppliers holding an open order are worth knowing.
    /// </summary>
    private static async Task<List<SupplierName>> LoadSupplierNamesAsync(
        JpmsContext context, List<string> subcontractorIds, CancellationToken cancellationToken)
    {
        var names = new List<SupplierName>();
        if (subcontractorIds.Count == 0) return names;

        var companies = await context.Subcontractors.AsNoTracking()
            .Where(company => subcontractorIds.Contains(company.SubcontractorId))
            .Select(company => new { company.SubcontractorId, company.CompanyName })
            .ToListAsync(cancellationToken);
        names.AddRange(companies
            .Where(company => !string.IsNullOrWhiteSpace(company.CompanyName))
            .Select(company => new SupplierName(company.CompanyName, company.SubcontractorId)));

        var links = await context.SubcontractorXeroLinks.AsNoTracking()
            .Where(link => subcontractorIds.Contains(link.SubcontractorId))
            .Select(link => new { link.SubcontractorId, link.XeroContactName })
            .ToListAsync(cancellationToken);
        names.AddRange(links
            .Where(link => !string.IsNullOrWhiteSpace(link.XeroContactName))
            .Select(link => new SupplierName(link.XeroContactName, link.SubcontractorId)));
        return names;
    }
}

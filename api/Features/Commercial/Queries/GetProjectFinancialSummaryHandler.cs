using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class GetProjectFinancialSummaryHandler : IQueryHandler<GetProjectFinancialSummary, IReadOnlyList<ProjectFinancialSummaryRow>>
{
    private readonly JpmsContext context;

    public GetProjectFinancialSummaryHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ProjectFinancialSummaryRow>> HandleAsync(GetProjectFinancialSummary query, CancellationToken cancellationToken)
    {
        // Budgeted sales: every counting valuation line (declined / TBC excluded —
        // mirrors ValuationLineItem.CountsTowardTotals). Omit lines carry negative
        // amounts and net off naturally; variation lines carry cost codes and count too.
        var sales = await context.ValuationLineItems.AsNoTracking()
            .Where(line => line.ProjectId == query.ProjectId
                           && line.LineType != (int)ValuationLineType.Declined
                           && line.LineType != (int)ValuationLineType.Tbc
                           && line.CostCode != "")
            .GroupBy(line => line.CostCode)
            .Select(group => new { CostCode = group.Key, Amount = group.Sum(line => line.LineAmount) })
            .ToListAsync(cancellationToken);

        // Completion: the latest claim's cumulative claimed value per cost centre
        // (any status — reflects current site progress even while a claim is in
        // draft). Completion % = claimed / budgeted sales, i.e. amount-weighted
        // across the centre's lines; lines not yet claimed against count as 0%.
        var latestClaimId = await context.ValuationClaims.AsNoTracking()
            .Where(claim => claim.ProjectId == query.ProjectId)
            .OrderByDescending(claim => claim.ClaimNumber)
            .Select(claim => (string?)claim.ValuationClaimId)
            .FirstOrDefaultAsync(cancellationToken);

        var claimedByCode = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        if (latestClaimId is not null)
        {
            var claimed = await context.ClaimLines.AsNoTracking()
                .Where(claimLine => claimLine.ValuationClaimId == latestClaimId)
                .Join(context.ValuationLineItems,
                    claimLine => claimLine.ValuationLineItemId,
                    line => line.ValuationLineItemId,
                    (claimLine, line) => new { line.CostCode, line.LineType, claimLine.CumulativeClaimed })
                .Where(joined => joined.LineType != (int)ValuationLineType.Declined
                                 && joined.LineType != (int)ValuationLineType.Tbc
                                 && joined.CostCode != "")
                .GroupBy(joined => joined.CostCode)
                .Select(group => new { CostCode = group.Key, Amount = group.Sum(joined => joined.CumulativeClaimed) })
                .ToListAsync(cancellationToken);
            foreach (var entry in claimed) claimedByCode[entry.CostCode] = entry.Amount;
        }

        // Actuals: Xero purchase lines allocated to this project. Net is stored
        // positive; supplier credit notes (ACCPAYCREDIT) subtract. Whole-line
        // allocations carry their project + centre on the line; split lines carry
        // theirs in XeroCostSplits (each row a share of the line's net, with its
        // own project), so both populations sum into the same per-centre totals.
        // Lines marked covered-by-timesheets are settlement of approved labour, not fresh
        // cost — excluded here so labour never double-counts (Labour-Time-Tracking-Scope §6).
        // Excluded below with a correlated NOT EXISTS so the filter stays in SQL and scales
        // with row count — rather than pulling every cover id (across all projects) into an
        // ever-growing NOT IN (...) parameter list that eventually hits SQL's ~2100-param cap.

        var actuals = await context.XeroLedgerLines.AsNoTracking()
            .Where(line => line.ProjectId == query.ProjectId
                           && line.AllocationStatus == (int)XeroAllocationStatus.Allocated
                           && line.CostCenterCode != null
                           && !context.XeroLineTimesheetCovers.AsNoTracking()
                               .Any(cover => cover.XeroLedgerLineId == line.XeroLedgerLineId))
            .GroupBy(line => line.CostCenterCode!)
            .Select(group => new
            {
                CostCode = group.Key,
                Amount = group.Sum(line => line.Type == "ACCPAYCREDIT" ? -line.Net : line.Net)
            })
            .ToListAsync(cancellationToken);

        var splitActuals = await context.XeroCostSplits.AsNoTracking()
            .Join(context.XeroLedgerLines,
                split => split.XeroLedgerLineId,
                line => line.XeroLedgerLineId,
                (split, line) => new { split.CostCenterCode, split.Net, split.ProjectId, line.AllocationStatus, line.Type, line.XeroLedgerLineId })
            .Where(joined => joined.ProjectId == query.ProjectId
                             && joined.AllocationStatus == (int)XeroAllocationStatus.Allocated
                             && !context.XeroLineTimesheetCovers.AsNoTracking()
                                 .Any(cover => cover.XeroLedgerLineId == joined.XeroLedgerLineId))
            .GroupBy(joined => joined.CostCenterCode)
            .Select(group => new
            {
                CostCode = group.Key,
                Amount = group.Sum(joined => joined.Type == "ACCPAYCREDIT" ? -joined.Net : joined.Net)
            })
            .ToListAsync(cancellationToken);

        // Work-order-linked spend, slice by slice, each on the invoice's own centre (a link on a
        // line split across the order's codes arrives as one slice per share — see
        // WorkOrderLinkSlices). Non-WO cost of sales per centre = total actual spend less these
        // linked slices — a partially split bill only counts its unallocated remainder. The
        // linked slices themselves are then re-attributed to the order's cost centres pro-rata,
        // so the Actual Cost of Sales column lines up with the Work Orders column.
        var linkSlices = await WorkOrderLinkSlices.ForProjectAsync(context, query.ProjectId, cancellationToken);

        var codeTotalsByOrder = await WorkOrderCostApportionment.CodeTotalsByOrderAsync(context, query.ProjectId, cancellationToken);

        // Packaged scope per centre — what reconciliation packages already account for,
        // so the table can net it out and show only unreconciled scope. Sales slices land
        // on their line's centre (with a pro-rata share of that line's claimed value);
        // packaged orders' committed value lands on the order lines' centres; and their
        // invoiced spend follows the same re-attribution as ActualCost below.
        static void Accumulate(Dictionary<string, decimal> map, string code, decimal amount) =>
            map[code] = map.TryGetValue(code, out var current) ? current + amount : amount;

        var packagedOrderIds = await context.ReconciliationPackageOrders.AsNoTracking()
            .Where(member => member.ProjectId == query.ProjectId)
            .Select(member => member.WorkOrderId)
            .ToListAsync(cancellationToken);
        var packagedOrderIdSet = packagedOrderIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var packagedSlices = await context.ReconciliationPackageSalesLines.AsNoTracking()
            .Where(slice => slice.ProjectId == query.ProjectId)
            .Join(context.ValuationLineItems,
                slice => slice.ValuationLineItemId,
                line => line.ValuationLineItemId,
                (slice, line) => new { line.ValuationLineItemId, line.CostCode, line.LineAmount, slice.Amount })
            .Where(joined => joined.CostCode != "")
            .ToListAsync(cancellationToken);

        var packagedLineIds = packagedSlices.Select(slice => slice.ValuationLineItemId).Distinct().ToList();
        var claimedByPackagedLine = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        if (latestClaimId is not null && packagedLineIds.Count > 0)
        {
            var claimedLines = await context.ClaimLines.AsNoTracking()
                .Where(claimLine => claimLine.ValuationClaimId == latestClaimId
                                    && packagedLineIds.Contains(claimLine.ValuationLineItemId))
                .Select(claimLine => new { claimLine.ValuationLineItemId, claimLine.CumulativeClaimed })
                .ToListAsync(cancellationToken);
            foreach (var entry in claimedLines) claimedByPackagedLine[entry.ValuationLineItemId] = entry.CumulativeClaimed;
        }

        var packagedSalesByCode = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var packagedClaimedByCode = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var slice in packagedSlices)
        {
            Accumulate(packagedSalesByCode, slice.CostCode, slice.Amount);
            if (slice.LineAmount != 0m && claimedByPackagedLine.TryGetValue(slice.ValuationLineItemId, out var lineClaimed))
                Accumulate(packagedClaimedByCode, slice.CostCode, Math.Round(lineClaimed * slice.Amount / slice.LineAmount, 2));
        }

        var packagedWoByCode = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        if (packagedOrderIds.Count > 0)
        {
            var packagedWoRows = await context.WorkOrderLines.AsNoTracking()
                .Where(line => packagedOrderIds.Contains(line.WorkOrderId) && line.CostCode != "")
                .GroupBy(line => line.CostCode)
                .Select(group => new { CostCode = group.Key, Total = group.Sum(line => line.LineTotal) })
                .ToListAsync(cancellationToken);
            foreach (var entry in packagedWoRows) packagedWoByCode[entry.CostCode] = entry.Total;
        }

        // Direct purchase costs inside packages — slices of allocated lines not paying
        // any work order (materials bought straight for the packaged scope). They sit
        // on the invoice's own centre, inside both ActualCost and Non-WO cost of sales,
        // so they net out of both when the table hides packaged scope.
        var packagedNonWoByCode = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var packagedDirectCosts = await context.ReconciliationPackageCostLines.AsNoTracking()
            .Where(slice => slice.ProjectId == query.ProjectId)
            .Join(context.XeroLedgerLines,
                slice => slice.XeroLedgerLineId,
                line => line.XeroLedgerLineId,
                (slice, line) => new
                {
                    InvoiceCode = line.CostCenterCode,
                    slice.Amount,
                    line.AllocationStatus,
                    line.XeroLedgerLineId
                })
            // Mirror the ActualCost filters above: only allocated, not-timesheet-covered
            // lines feed the columns, so only those may net out of them — anything else
            // would push a centre's netted figures below zero.
            .Where(joined => joined.InvoiceCode != null
                             && joined.AllocationStatus == (int)XeroAllocationStatus.Allocated
                             && !context.XeroLineTimesheetCovers.AsNoTracking()
                                 .Any(cover => cover.XeroLedgerLineId == joined.XeroLedgerLineId))
            .ToListAsync(cancellationToken);
        foreach (var direct in packagedDirectCosts)
            Accumulate(packagedNonWoByCode, direct.InvoiceCode!, direct.Amount);

        // Cost-side state: completion % and the finalisation lock, set inline on the
        // Financials tab, one row per cost centre. Centres never edited default to 0% / unlocked.
        var costProgress = await context.CostCentreCostProgress.AsNoTracking()
            .Where(progress => progress.ProjectId == query.ProjectId)
            .Select(progress => new { progress.CostCode, progress.CostCompletionPercent, progress.IsFinalised })
            .ToListAsync(cancellationToken);
        var costProgressByCode = new Dictionary<string, (decimal Percent, bool IsFinalised)>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in costProgress) costProgressByCode[entry.CostCode] = (entry.CostCompletionPercent, entry.IsFinalised);

        // Labour (Labour-Time-Tracking-Scope §6): approved timesheet cost posts as direct
        // (non-WO) actual cost of sales, settlement variances alongside it; submitted hours
        // surface as pending labour only — visible, never posted.
        var approvedLabour = await context.Timesheets.AsNoTracking()
            .Where(timesheet => timesheet.ProjectId == query.ProjectId
                                && timesheet.Status == (int)TimesheetStatus.Approved)
            .GroupBy(timesheet => timesheet.CostCode)
            .Select(group => new { CostCode = group.Key, Amount = group.Sum(timesheet => timesheet.CostAmount) })
            .ToListAsync(cancellationToken);

        var labourVariances = await context.LabourSettlementVariances.AsNoTracking()
            .Where(variance => variance.ProjectId == query.ProjectId)
            .GroupBy(variance => variance.CostCode)
            .Select(group => new { CostCode = group.Key, Amount = group.Sum(variance => variance.Amount) })
            .ToListAsync(cancellationToken);

        var pendingLabour = await context.Timesheets.AsNoTracking()
            .Where(timesheet => timesheet.ProjectId == query.ProjectId
                                && timesheet.Status == (int)TimesheetStatus.Submitted
                                && timesheet.WorkerId != "")
            .Join(context.Workers, timesheet => timesheet.WorkerId, worker => worker.WorkerId,
                (timesheet, worker) => new { timesheet.CostCode, timesheet.Hours, worker.HourlyRate })
            .GroupBy(row => row.CostCode)
            .Select(group => new { CostCode = group.Key, Amount = group.Sum(row => row.Hours * row.HourlyRate) })
            .ToListAsync(cancellationToken);

        var labourByCode = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in approvedLabour) labourByCode[entry.CostCode] = entry.Amount;
        foreach (var entry in labourVariances)
            labourByCode[entry.CostCode] = labourByCode.TryGetValue(entry.CostCode, out var labourSum)
                ? labourSum + entry.Amount
                : entry.Amount;
        var pendingLabourByCode = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in pendingLabour) pendingLabourByCode[entry.CostCode] = Math.Round(entry.Amount, 2);

        var salesByCode = sales.ToDictionary(s => s.CostCode, s => s.Amount, StringComparer.OrdinalIgnoreCase);
        var actualByCode = actuals.ToDictionary(a => a.CostCode, a => a.Amount, StringComparer.OrdinalIgnoreCase);
        foreach (var splitActual in splitActuals)
            actualByCode[splitActual.CostCode] = actualByCode.TryGetValue(splitActual.CostCode, out var existing)
                ? existing + splitActual.Amount
                : splitActual.Amount;

        // Non-WO = actual spend less the work-order-linked slices, per the invoice's own
        // centre — computed BEFORE the re-attribution below moves the linked spend around.
        var nonWoActualByCode = new Dictionary<string, decimal>(actualByCode, StringComparer.OrdinalIgnoreCase);
        foreach (var slice in linkSlices)
            nonWoActualByCode[slice.InvoiceCode] = nonWoActualByCode.TryGetValue(slice.InvoiceCode, out var beforeLink)
                ? beforeLink - slice.Amount
                : -slice.Amount;

        // Re-attribute each linked slice from the invoice's Xero centre to the order's
        // cost-code mix (pro-rata by the order's line values). Orders with no coded lines
        // can't be apportioned — those slices stay on the invoice's centre. Slices paying
        // a PACKAGED order also accumulate into the packaged-actuals map, on the same
        // centres, so netting them out mirrors this attribution exactly.
        var packagedActualByCode = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var slice in linkSlices)
        {
            var isPackaged = packagedOrderIdSet.Contains(slice.WorkOrderId);

            if (!codeTotalsByOrder.TryGetValue(slice.WorkOrderId, out var codeTotals))
            {
                if (isPackaged) Accumulate(packagedActualByCode, slice.InvoiceCode, slice.Amount);
                continue;
            }

            actualByCode[slice.InvoiceCode] = actualByCode.TryGetValue(slice.InvoiceCode, out var sourceTotal)
                ? sourceTotal - slice.Amount
                : -slice.Amount;

            foreach (var (costCode, share) in WorkOrderCostApportionment.Apportion(slice.Amount, codeTotals))
            {
                actualByCode[costCode] = actualByCode.TryGetValue(costCode, out var destinationTotal)
                    ? destinationTotal + share
                    : share;
                if (isPackaged) Accumulate(packagedActualByCode, costCode, share);
            }
        }

        // Packaged direct spend is packaged actual cost too — it never left the
        // invoice's own centre (no order to re-attribute it to).
        foreach (var entry in packagedNonWoByCode)
            Accumulate(packagedActualByCode, entry.Key, entry.Value);

        return salesByCode.Keys.Union(actualByCode.Keys, StringComparer.OrdinalIgnoreCase)
            .Union(costProgressByCode.Keys, StringComparer.OrdinalIgnoreCase)
            .Union(labourByCode.Keys, StringComparer.OrdinalIgnoreCase)
            .Union(pendingLabourByCode.Keys, StringComparer.OrdinalIgnoreCase)
            .Union(packagedSalesByCode.Keys, StringComparer.OrdinalIgnoreCase)
            .Union(packagedWoByCode.Keys, StringComparer.OrdinalIgnoreCase)
            // Always a subset of actualByCode's keys today (both branches above write the
            // same key there), but unioned defensively so a refactor can't drop a row.
            .Union(packagedActualByCode.Keys, StringComparer.OrdinalIgnoreCase)
            .Select(code =>
            {
                var budgetedSales = salesByCode.TryGetValue(code, out var salesAmount) ? salesAmount : 0m;
                var claimedAmount = claimedByCode.TryGetValue(code, out var claimedValue) ? claimedValue : 0m;

                // Approved labour (and its settlement variances) is direct non-WO actual cost
                // of sales, on top of the Xero-side spend computed above.
                var labourActualCost = labourByCode.TryGetValue(code, out var labour) ? labour : 0m;
                var actualCost = (actualByCode.TryGetValue(code, out var actual) ? actual : 0m) + labourActualCost;

                var budgetedCost = Math.Round(budgetedSales * FinancialSummaryAssumptions.CostFactor, 2);
                var completionPercent = budgetedSales == 0m ? 0m : Math.Round(claimedAmount / budgetedSales * 100m, 1);
                var expectedActualCost = Math.Round(budgetedCost * completionPercent / 100m, 2);

                var costState = costProgressByCode.TryGetValue(code, out var state) ? state : (Percent: 0m, IsFinalised: false);
                var nonWoActualCost = (nonWoActualByCode.TryGetValue(code, out var nonWo) ? nonWo : 0m) + labourActualCost;
                var pendingLabourCost = pendingLabourByCode.TryGetValue(code, out var pending) ? pending : 0m;

                return new ProjectFinancialSummaryRow(
                    code,
                    budgetedSales,
                    budgetedCost,
                    completionPercent,
                    expectedActualCost,
                    actualCost,
                    expectedActualCost - actualCost,
                    claimedAmount,
                    costState.Percent,
                    nonWoActualCost,
                    costState.IsFinalised,
                    labourActualCost,
                    pendingLabourCost,
                    packagedSalesByCode.TryGetValue(code, out var packagedSales) ? packagedSales : 0m,
                    packagedClaimedByCode.TryGetValue(code, out var packagedClaimed) ? packagedClaimed : 0m,
                    packagedWoByCode.TryGetValue(code, out var packagedWo) ? packagedWo : 0m,
                    packagedActualByCode.TryGetValue(code, out var packagedActual) ? packagedActual : 0m,
                    packagedNonWoByCode.TryGetValue(code, out var packagedNonWo) ? packagedNonWo : 0m);
            })
            .ToList();
    }
}

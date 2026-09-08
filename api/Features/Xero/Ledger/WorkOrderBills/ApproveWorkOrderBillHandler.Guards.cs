using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger.WorkOrderBills;

public sealed partial class ApproveWorkOrderBillHandler
{
    private static readonly HashSet<string> LockedStatuses = new(StringComparer.OrdinalIgnoreCase) { "PAID", "VOIDED", "DELETED" };

    /// <summary>The bill as stored must be whole, still queued, and coded line for line.</summary>
    private static void GuardBill(ApproveWorkOrderBill command, List<XeroLedgerLineEntity> lines)
    {
        if (lines.Count == 0)
            throw new InvalidOperationException("No stored ledger lines for this bill — sync from Xero and try again.");
        if (lines.Any(line => line.AllocationStatus != (int)XeroAllocationStatus.Unallocated))
            throw new InvalidOperationException("Every line of the bill must still be unallocated — it has moved since the card was drawn.");
        if (LockedStatuses.Contains(lines[0].InvoiceStatus))
            throw new InvalidOperationException($"The bill is {lines[0].InvoiceStatus} in Xero and can't be approved from here.");

        var stored = lines.Select(line => line.XeroLedgerLineId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var coded = command.Lines.Select(line => line.XeroLedgerLineId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!stored.SetEquals(coded))
            throw new InvalidOperationException("Approve codes every line of the bill at once — the lines sent don't match the bill's lines.");
    }

    /// <summary>
    /// The rule re-run at the moment of approval — the same recogniser the read used, over the
    /// bill's lines alone — must give the bill the order the user is approving it against.
    /// </summary>
    private async Task<WorkOrderBillMatch> RequireMatchAsync(
        ApproveWorkOrderBill command, List<XeroLedgerLineEntity> lines, CancellationToken cancellationToken)
    {
        var recognition = await WorkOrderBillRecognition.ForAsync(context, lines, cancellationToken);
        var labour = await LabourSupplierRecognition.ForAsync(context, lines, cancellationToken);
        var suggester = await XeroLedgerReads.SuggesterForAsync(context, lines, cancellationToken);
        var verdicts = XeroLedgerReads.WorkOrderBillsFor(recognition, lines, labour, suggester);
        if (!verdicts.TryGetValue(lines[0].XeroLedgerLineId, out var verdict) || verdict.Match is null)
            throw new InvalidOperationException(verdict.ExceptionReason
                ?? "The bill no longer matches a work order — re-check matches and try again.");
        if (!verdict.Match.WorkOrderId.Equals(command.WorkOrderId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"The bill now matches {verdict.Match.WorkOrderReference}, not the order on the card — re-check matches and try again.");
        return verdict.Match;
    }

    /// <summary>
    /// Each line's shares must add up to its net, sit on the order's own cost codes, and name
    /// active centres. Returns the codings keyed by line, projects filled from the order.
    /// </summary>
    private async Task<Dictionary<string, IReadOnlyList<XeroCostSplit>>> RequireCodingsAsync(
        ApproveWorkOrderBill command, List<XeroLedgerLineEntity> lines, WorkOrderEntity order, CancellationToken cancellationToken)
    {
        var orderCodes = await context.WorkOrderLines.AsNoTracking()
            .Where(line => line.WorkOrderId == order.WorkOrderId)
            .Select(line => line.CostCode)
            .Distinct()
            .ToListAsync(cancellationToken);
        var activeCodes = await context.CostCenters.AsNoTracking()
            .Where(centre => centre.IsActive && orderCodes.Contains(centre.Code))
            .Select(centre => centre.Code)
            .ToListAsync(cancellationToken);

        var codings = new Dictionary<string, IReadOnlyList<XeroCostSplit>>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            var coding = command.Lines.First(candidate => candidate.XeroLedgerLineId.Equals(line.XeroLedgerLineId, StringComparison.OrdinalIgnoreCase));
            var codes = coding.Splits.Select(split => split.CostCenterCode).ToList();
            if (codes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != codes.Count)
                throw new InvalidOperationException("Each cost code can appear only once on a line.");
            if (coding.Splits.Any(split => split.Net <= 0m))
                throw new InvalidOperationException("Every share must be greater than zero.");
            var strangers = codes.Where(code => !orderCodes.Contains(code, StringComparer.OrdinalIgnoreCase)).ToList();
            if (strangers.Count > 0)
                throw new InvalidOperationException($"{order.Reference} carries no {string.Join(", ", strangers)} line — a Work Order bill is coded to the order's own cost codes.");
            var inactive = codes.Where(code => !activeCodes.Contains(code, StringComparer.OrdinalIgnoreCase)).ToList();
            if (inactive.Count > 0)
                throw new InvalidOperationException($"Not an active cost centre: {string.Join(", ", inactive)}.");
            var total = coding.Splits.Sum(split => split.Net);
            if (total != line.Net)
                throw new InvalidOperationException(
                    $"The shares of \"{line.Description}\" must add up to its net of {line.Net:0.00} — they add up to {total:0.00}.");
            codings[line.XeroLedgerLineId] = coding.Splits.Select(split => split with { ProjectId = order.ProjectId }).ToList();
        }
        return codings;
    }
}

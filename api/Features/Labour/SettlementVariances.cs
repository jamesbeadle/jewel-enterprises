using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>
/// The variances posted against a month's covered lines, netted per worker (2026-09-08, the
/// accountant's "the settlement view does not net a posted variance"): a variance follows the
/// cover of the line it names — that line's worker when the run marked the cover per worker,
/// the counterparty as a whole when a person marked it — so a worker's difference reads what
/// is still unexplained after the accepted difference. A variance that names no line cannot be
/// placed in a month and is not netted here; the per-project settlement view still counts it.
/// </summary>
public static class SettlementVariances
{
    public static async Task<IReadOnlyDictionary<string, decimal>> ByCoveredLineAsync(
        JpmsContext context, IReadOnlyList<string> coveredLineIds, CancellationToken cancellationToken)
    {
        if (coveredLineIds.Count == 0) return new Dictionary<string, decimal>();
        var variances = await context.LabourSettlementVariances.AsNoTracking()
            .Where(variance => variance.XeroLedgerLineId != null && coveredLineIds.Contains(variance.XeroLedgerLineId!))
            .ToListAsync(cancellationToken);
        return variances
            .GroupBy(variance => variance.XeroLedgerLineId!)
            .ToDictionary(group => group.Key, group => group.Sum(variance => variance.Amount));
    }

    /// <summary>What has been accepted for this worker's month: the variances on the lines whose
    /// cover is theirs (or the counterparty's as a whole).</summary>
    public static decimal PostedFor(
        string workerId, IEnumerable<XeroLineTimesheetCoverEntity> counterpartyCovers, IReadOnlyDictionary<string, decimal> byCoveredLine) =>
        counterpartyCovers
            .Where(cover => cover.WorkerId is null || cover.WorkerId == workerId)
            .Sum(cover => byCoveredLine.TryGetValue(cover.XeroLedgerLineId, out var amount) ? amount : 0m);
}

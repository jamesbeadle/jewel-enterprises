using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero;
using static Jewel.JPMS.Api.Features.Labour.Commands.XeroCodingWording;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed partial class RunXeroCodingHandler
{
    /// <summary>A recognised bill the ledger holds, with the bill as Xero holds it now — null
    /// when Xero could not be asked, in which case the ledger's word stands.</summary>
    private sealed record StandingBill(IGrouping<string, XeroLedgerLineEntity> Lines, XeroBillSummary? Fresh);

    /// <summary>The candidates Xero still holds live — a bill voided since the ledger last saw
    /// it drops out here. A bill Xero cannot be asked about stays a candidate; the fresh read
    /// before any write reports that properly.</summary>
    private async Task<List<StandingBill>> StillStandingAsync(
        List<IGrouping<string, XeroLedgerLineEntity>> candidates, CancellationToken cancellationToken)
    {
        var standing = new List<StandingBill>();
        foreach (var candidate in candidates)
        {
            var (isLive, fresh) = await ReadStandingAsync(candidate.Key, cancellationToken);
            if (isLive) standing.Add(new StandingBill(candidate, fresh));
        }
        return standing;
    }

    private async Task<(bool IsLive, XeroBillSummary? Fresh)> ReadStandingAsync(string billId, CancellationToken cancellationToken)
    {
        try
        {
            var bill = await xero.GetBillAsync(billId, cancellationToken);
            return (bill is not null && !IsGone(bill.Status), bill);
        }
        catch (XeroCallFailedException)
        {
            return (true, null);
        }
    }

    /// <summary>Several live bills that all carry ONE number are the same invoice keyed more than
    /// once (2026-09-08): <see cref="ReissueChoice"/> takes the one whose net is the schedule's,
    /// else the newest, and every worker's outcome says so. Different numbers, or a bill Xero
    /// could not describe, leave the choice to a person.</summary>
    private static string? ChooseAmongSameNumber(CodingParty party, List<StandingBill> standing)
    {
        var numbers = standing.Select(bill => bill.Lines.First().InvoiceNumber?.Trim() ?? "").Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (numbers.Count != 1 || numbers[0] == "" || standing.Any(bill => bill.Fresh is null)) return null;
        var (chosen, why) = ReissueChoice.Choose(standing.Select(bill => bill.Fresh!).ToList(), party.ScheduleTotal);
        if (chosen is null) return null;
        foreach (var worker in party.Workers) worker.Preface += why;
        return chosen.InvoiceId;
    }
}

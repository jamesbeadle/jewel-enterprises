using Jewel.JPMS.Api.Features.Xero;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

/// <summary>
/// Which of several LIVE bills sharing a number the run takes (2026-09-08, the accountant's
/// "the run halts when two live bills share a number"): the one whose net is the schedule's;
/// among several of those, or none, the newest — by when Xero last changed it, then by its
/// date. Two that cannot be told apart are a question for a person, never a guess.
/// </summary>
internal static class ReissueChoice
{
    public static (XeroBillSummary? Bill, string? Why) Choose(IReadOnlyList<XeroBillSummary> live, decimal scheduleTotal)
    {
        if (live.Count == 1) return (live[0], null);
        var matching = live.Where(bill => bill.SubTotal == scheduleTotal).ToList();
        var candidates = matching.Count > 0 ? matching : live;
        var newestFirst = candidates
            .OrderByDescending(bill => bill.UpdatedUtc ?? DateTime.MinValue)
            .ThenByDescending(bill => bill.Date ?? DateTime.MinValue)
            .ToList();
        if (newestFirst.Count > 1 && IsSameMoment(newestFirst[0], newestFirst[1])) return (null, null);
        var chosen = newestFirst[0];
        var because = matching.Count == 1 ? "its net is the schedule's"
            : matching.Count > 1 ? "of those whose net is the schedule's it is the newest"
            : "none has the schedule's net, so it is the newest";
        var others = live.Where(bill => bill.InvoiceId != chosen.InvoiceId).Select(Describe);
        return (chosen, $"{live.Count} live bills carry the number \"{chosen.InvoiceNumber}\": took {Describe(chosen)} because {because}, "
            + $"over {string.Join(", ", others)}. ");
    }

    private static bool IsSameMoment(XeroBillSummary first, XeroBillSummary second) =>
        first.UpdatedUtc == second.UpdatedUtc && first.Date == second.Date;

    private static string Describe(XeroBillSummary bill) => $"{bill.InvoiceId} ({bill.Status}, net £{bill.SubTotal:N2})";
}

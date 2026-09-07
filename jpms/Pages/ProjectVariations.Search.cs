using Jewel.JPMS.Components;
using Jewel.JPMS.Features.RecordLinks;

namespace Jewel.JPMS.Pages;

public partial class ProjectVariations
{
    // ---- Free-text search -----------------------------------------------------------------------
    // Keywords are ANDed on whitespace, same as every register search. The originating request's
    // own text is included — someone searching "boiler" should find the variation that priced the
    // boiler RFI even when only the RFI says the word.

    private string search = "";

    private bool Searching => !string.IsNullOrWhiteSpace(search);

    private void OnSearchInput(string value) => search = value;

    private void ClearSearch() => search = "";

    private bool MatchesSearch(VariationOrder order)
    {
        if (!Searching) return true;

        var tokens = search.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return tokens.All(token =>
            SearchableText(order).Any(field => field is not null && field.Contains(token, StringComparison.OrdinalIgnoreCase)));
    }

    private IEnumerable<string?> SearchableText(VariationOrder order)
    {
        yield return order.Reference;
        yield return order.DisplayNumber;
        yield return order.VariationRef;
        yield return order.Title;
        yield return order.Description;
        yield return order.CostCode;

        // The Request column's own text, so the search finds a variation by its RFI.
        var source = RequestFor(order);
        if (source is null) yield break;
        yield return source.Reference;
        yield return source.DisplayNumber;
        yield return source.Title;
    }

    // ---- Status filter --------------------------------------------------------------------------
    // One chip per stage of the variation ladder plus "All" (accountant's ask, 2026-09-07). The
    // filter narrows the TABLE only: the headline counts and the Approved-value tile above it stay
    // whole-book figures, so the reader never mistakes a filtered subtotal for the project's position.
    // Default is All — nothing is hidden until the reader chooses to hide it; the chip counts say
    // where the rows are (and follow the search, so they count what the search left).

    private const string AllStatusesKey = "all";

    private static readonly (VariationOrderStatus Status, string Label)[] StatusChoicesInOrder =
    {
        (VariationOrderStatus.Quoting, "Quoting"),
        (VariationOrderStatus.Issued, "Issued"),
        (VariationOrderStatus.AwaitingArchitectInstruction, "Awaiting AI"),
        (VariationOrderStatus.Approved, "Approved"),
        (VariationOrderStatus.Rejected, "Rejected")
    };

    private VariationOrderStatus? statusFilter;

    private bool FilteringByStatus => statusFilter is not null;

    private string StatusFilterKey => statusFilter?.ToString() ?? AllStatusesKey;

    private string StatusFilterLabel =>
        statusFilter is { } picked
            ? StatusChoicesInOrder.First(c => c.Status == picked).Label
            : "All";

    private void PickStatus(string key) =>
        statusFilter = Enum.TryParse<VariationOrderStatus>(key, out var status) ? status : null;

    private void ClearStatusFilter() => statusFilter = null;

    private bool MatchesStatus(VariationOrder order) => statusFilter is null || order.Status == statusFilter;

    private IReadOnlyList<TabItem> StatusChips
    {
        get
        {
            var searched = Rows.Where(MatchesSearch).ToList();
            var chips = new List<TabItem>(StatusChoicesInOrder.Length + 1)
            {
                new(AllStatusesKey, "All", Count: searched.Count, Title: "Every variation in the book")
            };
            foreach (var (status, label) in StatusChoicesInOrder)
            {
                chips.Add(new TabItem(status.ToString(), label,
                    Count: searched.Count(o => o.Status == status),
                    Title: $"Only variations at {label}"));
            }
            return chips;
        }
    }

    // ---- The rows on screen ---------------------------------------------------------------------

    // Search OR status chip is narrowing the table — drives the export's "include entire register" offer.
    private bool Narrowed => Searching || FilteringByStatus;

    // Search matches parked under a status other than the selected chip.
    private int HiddenByStatusCount => FilteringByStatus ? Rows.Count(o => MatchesSearch(o) && !MatchesStatus(o)) : 0;

    private IReadOnlyList<VariationOrder> FilteredRows =>
        Rows.Where(o => MatchesSearch(o) && MatchesStatus(o)).ToList();

}

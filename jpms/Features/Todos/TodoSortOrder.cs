namespace Jewel.JPMS.Features.Todos;

// The reader's sort direction for the OPEN items on every to-do surface — the dashboard panel,
// the project To-do tab and the To-dos browser, list and board alike (the board's Open column
// keeps its host's order). One rule here so the three can never sort differently.
//
// The direction is by the date the item was RAISED (CreatedAt, with the TODO-#### reference as
// the tie-break — the two agree, the number is issued at creation). Oldest first is the server's
// canonical order (api TodosOrdering: number order, "the reference is what people quote and scan
// for"), so the default reads exactly as it always has; Newest first turns the open pile over so
// what was just raised sits at the top. Due dates stay a badge, not the sort.
//
// The DONE pile is deliberately left alone: it always reads most recently completed first
// (the server's order, and what the board's Done cap keeps in view) — a history that reads
// backwards, whichever way the open list is turned. Done items keep their given order and
// follow the open ones, so a mixed (All) list still groups open before done.
public static class TodoSortOrder
{
    public static IReadOnlyList<TodoItem> Apply(IEnumerable<TodoItem> items, bool newestFirst)
    {
        var all = items as IReadOnlyList<TodoItem> ?? items.ToList();
        var open = all.Where(item => !item.IsComplete);
        var ordered = newestFirst
            ? open.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Reference, StringComparer.Ordinal)
            : open.OrderBy(item => item.CreatedAt).ThenBy(item => item.Reference, StringComparer.Ordinal);
        return ordered.Concat(all.Where(item => item.IsComplete)).ToList();
    }

    /// <summary>The toggle's own label — it names the CURRENT order, not the one a click gives.</summary>
    public static string Label(bool newestFirst) => newestFirst ? "Newest first" : "Oldest first";

    /// <summary>The toggle's tooltip: what the order is now and what a click does.</summary>
    public static string Title(bool newestFirst) =>
        newestFirst
            ? "Open items read newest raised first — click for oldest first (number order). Done items always read most recently completed first."
            : "Open items read oldest raised first (number order) — click for newest first. Done items always read most recently completed first.";
}

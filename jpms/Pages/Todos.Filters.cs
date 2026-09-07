using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Features.Todos;

namespace Jewel.JPMS.Pages;

public partial class Todos
{
    // Everything the scope and assignee filters — and the search — leave standing: the BOARD's
    // item set (it shows both statuses as columns, so the status filter is not applied here).
    // Sorted through the one rule (TodoSortOrder) before it leaves: the board's Open column
    // keeps the host's order, and the list below narrows this set without reordering it.
    private IReadOnlyList<TodoItem> FilteredItems =>
        TodoSortOrder.Apply(
            items
                .Where(item => scopeFilter switch
                {
                    ScopeAll => true,
                    ScopeGeneral => IsGeneral(item),
                    _ => item.ProjectId == scopeFilter
                })
                .Where(item => MatchesAssigneeFilter(item))
                .Where(item => !HasQuery || MatchesTodo(item)),
            newestFirst);

    // The LIST's item set: the filtered items narrowed further by the Open/Done/All tabs — except
    // while a query is live, when the search looks across every status (the tabs render disabled).
    private IReadOnlyList<TodoItem> VisibleItems =>
        FilteredItems
            .Where(item => HasQuery || statusFilter switch
            {
                StatusFilter.Open => !item.IsComplete,
                StatusFilter.Done => item.IsComplete,
                _ => true
            })
            .ToList();

    private bool HasQuery => !string.IsNullOrWhiteSpace(search);

    private void OnSearchInput(string value) => search = value;

    private void ClearSearch() => search = "";

    // Every whitespace-separated keyword must appear in at least one of the item's fields
    // (case-insensitive) — AND across keywords so extra words narrow the results. "todo-0011"
    // matches the Reference; a pinned assignee's name matches too.
    private bool MatchesKeywords(params string?[] fields)
    {
        var tokens = search.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return tokens.All(token =>
            fields.Any(field => field is not null && field.Contains(token, StringComparison.OrdinalIgnoreCase)));
    }

    private bool MatchesTodo(TodoItem item) =>
        MatchesKeywords(item.Reference, item.Title, item.Notes, item.AssigneePersonName);

    // Matches the search but not the scope/assignee filters — announced under the list so a
    // filtered-out hit never reads as "not found".
    private int HiddenMatchCount =>
        !HasQuery ? 0 : items.Count(MatchesTodo) - (boardView ? FilteredItems : VisibleItems).Count;

    // Role and person are independent narrowings: a role alone matches every item assigned to it
    // — pinned or not — a person alone matches everything pinned to them under ANY role they
    // hold, and both together intersect. Unassigned means the role slot is empty, so no person
    // filter can apply with it (the select is disabled and cleared while it's picked).
    private bool MatchesAssigneeFilter(TodoItem item)
    {
        if (roleFilter == Unassigned) return item.AssigneeRole is null;
        if (SelectedRoleFilter is Role role && item.AssigneeRole != role) return false;
        return personFilter == ""
            || string.Equals(item.AssigneePersonEmail, personFilter, StringComparison.OrdinalIgnoreCase);
    }

    private Role? SelectedRoleFilter =>
        roleFilter != "" && roleFilter != Unassigned && int.TryParse(roleFilter, out var value)
            ? (Role)value
            : null;

    // The person filter's pool: holders of the selected role, or — with no role picked — every
    // assignable user once (a person holding two roles is still one person), A–Z by name.
    private IReadOnlyList<TodoAssignablePerson> FilterablePeople =>
        SelectedRoleFilter is Role role
            ? assignablePeople.Where(person => person.Role == role).ToList()
            : assignablePeople
                .GroupBy(person => person.Email, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(person => person.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

    private void OnRoleFilterChange(ChangeEventArgs e)
    {
        roleFilter = e.Value?.ToString() ?? "";
        // An unassigned item has no person, and a person picked under the old role may not hold
        // the new one — either way the person filter resets rather than silently matching nothing.
        if (personFilter != ""
            && (roleFilter == Unassigned
                || !FilterablePeople.Any(person => string.Equals(person.Email, personFilter, StringComparison.OrdinalIgnoreCase))))
            personFilter = "";
    }

    // The role filter's option pool: the roles that actually HAVE items on this reader's current
    // list, A–Z by display name. Unassigned is offered when it would match something (or to the
    // MD/administrators, whose list is the master one).
    private IReadOnlyList<Role> RolesOnList =>
        items.Where(item => item.AssigneeRole is not null)
            .Select(item => item.AssigneeRole!.Value)
            .Distinct()
            .OrderBy(role => role.DisplayName(), StringComparer.OrdinalIgnoreCase)
            .ToList();

    private bool HasUnassigned => items.Any(item => item.AssigneeRole is null);

    private string StatusTabClass(StatusFilter tab) =>
        statusFilter == tab
            ? "chip chip-active"
            : "chip";

    private string ViewTabClass(bool board) =>
        boardView == board
            ? "chip chip-active"
            : "chip";

    private async Task SetView(bool board)
    {
        if (boardView == board) return;
        boardView = board;
        await ViewStorage.WriteAsync(Auth.CurrentUser!.Email, board);
    }

    // The sort direction applies to OPEN items only (TodoSortOrder), so the list's Done tab —
    // with no query, which would bring the open items back in — has nothing for it to turn.
    private bool SortToggleIdle => !boardView && !HasQuery && statusFilter == StatusFilter.Done;

    private async Task ToggleSort()
    {
        newestFirst = !newestFirst;
        await ViewStorage.WriteNewestFirstAsync(Auth.CurrentUser!.Email, newestFirst);
    }
}

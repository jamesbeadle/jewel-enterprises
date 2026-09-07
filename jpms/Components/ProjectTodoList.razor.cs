using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Features.Todos;

namespace Jewel.JPMS.Components;

public partial class ProjectTodoList
{
    [Parameter, EditorRequired] public string ProjectId { get; set; } = "";

    private IReadOnlyList<TodoItem> items = Array.Empty<TodoItem>();

    // The assignee picker's option pool: the ROLES a to-do can be assigned to (internal
    // office/management roles — TodoRoles.AssignableAsTodoAssignee, served by
    // ListTodoAssignableRoles) and, under each role, the directory holders it can be pinned to
    // (ListTodoAssignablePeople), fetched once when the tab loads and shaped by
    // TodoAssigneePicker.BuildOptions. Assignment is picker-only.
    private IReadOnlyList<SearchSelect.Option> AssigneeOptions = Array.Empty<SearchSelect.Option>();
    private bool loading = true;
    private bool busy;
    private string? error;
    // Board (the default) or flat list — the shared per-user preference (TodoViewStorage).
    private bool boardView = true;
    // Open items newest raised first (true) or oldest first (false, the default — the server's
    // number order) — the other shared per-user preference. Applied through TodoSortOrder.
    private bool newestFirst;
    // The assigned-role filter: "" = any, UnassignedFilter, or a Role's int as a string.
    private const string UnassignedFilter = "__unassigned__";
    private string roleFilter = "";

    // Keyword search state. The query filters the to-do list and drives the "Related records"
    // sections. Requests and drawings read synchronously from their cached stores; variation orders
    // have no cached register, so they are fetched once in the background when the tab loads and held
    // here for the session's stay on the tab.
    private const int MaxRelated = 8;
    private string search = "";
    private IReadOnlyList<VariationOrder> variations = Array.Empty<VariationOrder>();
    private bool relatedLoading = true;

    // "Add a to-do item" modal state. The form lives in the modal; opening it starts from a clean
    // slate, and a successful add closes it.
    private bool addOpen;
    private string? addError;
    private string newTitle = "";
    private string newNotes = "";
    private string newAssignee = "";
    private string newDue = "";

    // Managing the list mirrors the server-side TodoRoles.AllowedToManageTodos gate: directors
    // (managing and finance), project managers, site managers and accounts (administrators carry
    // every role). Everyone else sees the list read-only.
    private bool CanManage =>
        Session.AvailableRoles.Any(role => role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector
            or Role.ProjectManager or Role.SiteManager or Role.Accounts);

    protected override async Task OnInitializedAsync()
    {
        var email = Auth.CurrentUser?.Email ?? "";
        boardView = await ViewStorage.ReadBoardAsync(email);
        newestFirst = await ViewStorage.ReadNewestFirstAsync(email);

        // Related-record sources for the keyword search. Refresh on entry per the store
        // convention: cached requests/drawings serve matches immediately, then update when the
        // background reload lands. Variation orders load once alongside (no cached register to lean on).
        Requests.OnChange += HandleRegisterChange;
        Drawings.OnChange += HandleRegisterChange;
        Requests.Refresh(ProjectId);
        Drawings.Refresh(ProjectId);
        _ = LoadVariationsAsync();

        // The list and the assignable-role pool (the add modal's picker) are independent reads —
        // run them together.
        if (CanManage) await Task.WhenAll(LoadAsync(), LoadAssignableRolesAsync());
        else await LoadAsync();
    }

    public void Dispose()
    {
        Requests.OnChange -= HandleRegisterChange;
        Drawings.OnChange -= HandleRegisterChange;
    }

    private void HandleRegisterChange() => _ = InvokeAsync(StateHasChanged);

    private async Task LoadVariationsAsync()
    {
        // A failed load simply leaves variations out of the search results rather than blocking
        // the tab — requests and drawings still match.
        try
        {
            variations = await Variations.ListForProjectAsync(ProjectId);
        }
        catch { }
        finally
        {
            relatedLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadAssignableRolesAsync()
    {
        // A failed load leaves the picker with no options rather than blocking the tab — the add
        // modal still works, the item just goes in unassigned.
        try
        {
            var rolesTask = Todos.ListAssignableRolesAsync();
            var peopleTask = Todos.ListAssignablePeopleAsync();
            AssigneeOptions = TodoAssigneePicker.BuildOptions(await rolesTask, await peopleTask);
        }
        catch { }
    }

    private async Task LoadAsync()
    {
        loading = true;
        try { items = await Todos.ListForProjectAsync(ProjectId); }
        catch { error = "Couldn't load the to-do list. Please try again."; }
        finally { loading = false; }
    }

    private void OnNewNotesInput(ChangeEventArgs e) => newNotes = e.Value?.ToString() ?? "";

    private void OpenAdd()
    {
        newTitle = newNotes = newAssignee = "";
        // Due defaults to one week out; clear the field to raise an item with no due date.
        newDue = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd");
        addError = null;
        addOpen = true;
    }

    private void CloseAdd()
    {
        addOpen = false;
        addError = null;
    }

    private async Task Add()
    {
        if (busy) return;
        if (string.IsNullOrWhiteSpace(newTitle)) { addError = "A title is required."; return; }
        addError = null;
        var assignee = TodoAssigneePicker.Parse(newAssignee);
        await Run(async () =>
        {
            await Todos.AddAsync(new AddTodoItem(
                ProjectId,
                newTitle.Trim(),
                NullIfBlank(newNotes),
                assignee?.Role,
                assignee?.PersonEmail,
                ParseDate(newDue)));
            CloseAdd();
        });
        // A failed add leaves the modal open with the panel-level error mirrored inside it.
        if (error is not null && addOpen) addError = error;
    }

    // Opening an item is a navigation: everything beyond the row — full detail, linked to-dos,
    // communications, the manage actions — lives on the item's own page.
    private void OpenItem(TodoItem item) => Nav.NavigateTo($"/todos/{item.TodoItemId}");

    // The board's drag onto the other column — the tab's one in-place status change. The reloaded
    // list re-groups the item into Open/Done with the server's ordering.
    private async Task SetComplete(TodoItem item, bool complete)
    {
        if (busy || item.IsComplete == complete) return;
        await Run(() => Todos.UpdateAsync(new UpdateTodoItem(
            item.TodoItemId,
            item.Title,
            NullIfBlank(item.Notes),
            item.AssigneeRole,
            item.AssigneePersonEmail,
            item.DueAt,
            complete)));
    }

    private async Task Run(Func<Task> action)
    {
        error = null;
        try
        {
            busy = true;
            await action();
            // Take the server's fresh open-first/done-pile order: a dragged card re-grouping on
            // reload is the expected "moves to the done column" behaviour.
            items = await Todos.ListForProjectAsync(ProjectId);
        }
        catch (CommandFailedException ex) { error = ex.Message; }
        catch { error = "That action didn't complete. Please try again."; }
        finally { busy = false; }
    }

    // ---- Keyword search ------------------------------------------------------------------------

    private bool HasQuery => !string.IsNullOrWhiteSpace(search);

    private void OnSearchInput(string value) => search = value;

    private void ClearSearch() => search = "";

    // Every whitespace-separated keyword must appear in at least one of the record's fields
    // (case-insensitive) — AND across keywords so extra words narrow the results.
    private bool MatchesKeywords(params string?[] fields)
    {
        var tokens = search.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return tokens.All(token =>
            fields.Any(field => field is not null && field.Contains(token, StringComparison.OrdinalIgnoreCase)));
    }

    private bool MatchesTodo(TodoItem item) =>
        MatchesKeywords(item.Reference, item.Title, item.Notes, item.AssigneePersonName);

    private IReadOnlyList<Request> MatchedRequests =>
        Requests.ForProject(ProjectId)
            .Where(r => MatchesKeywords(r.Reference, r.DisplayNumber, r.Kind.DisplayName(), r.Kind.LongName(), r.Title, r.Description))
            .ToList();

    private IReadOnlyList<VariationOrder> MatchedVariations =>
        variations.Where(v => MatchesKeywords(v.Reference, v.DisplayNumber, v.VariationRef ?? "", v.Title, v.Description)).ToList();

    private IReadOnlyList<Drawing> MatchedDrawings =>
        Drawings.DrawingsFor(ProjectId).Where(d => MatchesKeywords(d.DrawingCode, d.Title)).ToList();

    private void OpenRequest(Request record) =>
        Nav.NavigateTo($"/projects/{ProjectId}/requests/view/{record.RequestId}");

    private void OpenVariation(string variationId) =>
        Nav.NavigateTo($"/projects/{ProjectId}/variations/{variationId}");

    private void OpenDrawing(Drawing drawing) =>
        Nav.NavigateTo($"/projects/{ProjectId}/documents/{drawing.DrawingId}");

    private static string RequestStatusLabel(RequestStatus status) => status.DisplayName();

    // The role filter's option pool: the roles that actually have items on this project.
    private IReadOnlyList<Role> RolesOnList =>
        items.Where(item => item.AssigneeRole is not null)
            .Select(item => item.AssigneeRole!.Value)
            .Distinct()
            .OrderBy(role => role.DisplayName(), StringComparer.OrdinalIgnoreCase)
            .ToList();

    private bool HasUnassigned => items.Any(item => item.AssigneeRole is null);

    private bool MatchesRoleFilter(TodoItem item) =>
        roleFilter == "" ? true
        : roleFilter == UnassignedFilter ? item.AssigneeRole is null
        : int.TryParse(roleFilter, out var value) && item.AssigneeRole == (Role)value;

    private string ViewTabClass(bool board) =>
        boardView == board
            ? "chip chip-active"
            : "chip";

    private async Task SetView(bool board)
    {
        if (boardView == board) return;
        boardView = board;
        await ViewStorage.WriteAsync(Auth.CurrentUser?.Email ?? "", board);
    }

    private async Task ToggleSort()
    {
        newestFirst = !newestFirst;
        await ViewStorage.WriteNewestFirstAsync(Auth.CurrentUser?.Email ?? "", newestFirst);
    }

    private static bool IsOverdue(TodoItem item) =>
        !item.IsComplete && item.DueAt is not null && item.DueAt.Value < DateTimeOffset.Now.Date;

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTimeOffset? ParseDate(string value) =>
        DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
}

using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Features.Todos;

namespace Jewel.JPMS.Pages;

public partial class Todos
{
    private const string ScopeAll = "__all__";
    private const string ScopeGeneral = "__general__";
    private const string Unassigned = "__unassigned__";

    private enum StatusFilter { Open, Done, All }

    // Session checked and the user signed in. This is NOT "the data is here" — keeping the two
    // apart is what lets the page show its chrome at once and hold the list until it lands.
    private bool loading = true;
    // A failed fetch has to open the gate, or the jewel pulses forever; the panel then says so
    // rather than reporting an empty list nobody actually asked for.
    private bool listFailed;
    // The filter row is built from the same round of fetches as the list, so it waits with it.
    private bool filtersReady;
    // The project list failed to load — the scope filter loses its labels and the add modal's
    // project picker has nothing to offer, so it says that instead of pretending to be empty.
    private bool projectsFailed;
    private bool busy;
    private string? error;

    private IReadOnlyList<TodoItem> items = Array.Empty<TodoItem>();
    // The flattened role/"Role — Person" pool feeds the add modal's assignee picker; the filter
    // row reads the raw lists below instead.
    private IReadOnlyList<SearchSelect.Option> assigneeOptions = Array.Empty<SearchSelect.Option>();
    private IReadOnlyList<Role> assignableRoles = Array.Empty<Role>();
    private IReadOnlyList<TodoAssignablePerson> assignablePeople = Array.Empty<TodoAssignablePerson>();

    private StatusFilter statusFilter = StatusFilter.Open;
    // Keyword search (the project tab's idiom): filters the to-dos client-side and drives the
    // tagged-email lookup. While a query is live the status tabs are bypassed — search looks
    // across Open and Done alike, so a completed item is always findable.
    private string search = "";
    // Board (the default) or flat list — read from TodoViewStorage per user, written back on
    // toggle, and shared with the project tab and the dashboard panel.
    private bool boardView = true;
    // Open items newest raised first (true) or oldest first (false, the default — the server's
    // number order) — the other shared per-user preference. Applied through TodoSortOrder.
    private bool newestFirst;
    private string scopeFilter = ScopeAll;
    // The two assignee filters: a role (its int as a string, or Unassigned, "" = any) and a
    // person's email ("" = anyone). Independent — see MatchesAssigneeFilter.
    private string roleFilter = "";
    private string personFilter = "";

    // "Add a to-do" modal state (the manage gate — see CanManage). newProject is the picked
    // project's id, "" = company-wide, which only CanSeeAll may post.
    private bool addOpen;
    private string? addError;
    private string newProject = "";
    private string newTitle = "";
    private string newNotes = "";
    private string newAssignee = "";
    private string newDue = "";

    // Said after an add whose item did NOT come back on the reader's own list — it went to another
    // role, so without this it would look like nothing happened. Cleared on the next add.
    private string? addedNote;
    private string? addedNoteHref;

    // Mirrors the API's TodoRoles.AllowedToSeeAllTodos gate: the MD and administrators browse
    // everything; everyone else reads their own list (ListMyTodoItems).
    private bool CanSeeAll =>
        Session.AvailableRoles.Any(role => role is Role.Admin or Role.ManagingDirector);

    // Mirrors the API's TodoRoles.AllowedToManageTodos gate — deliberately WIDER than CanSeeAll:
    // an FD, PM, site manager or accounts user reads only their own items here but may still hand
    // one on to another role, exactly as they can on a project's To-do tab.
    private bool CanManage =>
        Session.AvailableRoles.Any(role => role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector
            or Role.ProjectManager or Role.SiteManager or Role.Accounts);

    // Mirrors the API's JpmsRoleSets.AllInternal — who may read to-dos at all.
    private bool HasInternalRole =>
        Session.AvailableRoles.Any(role => role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector
            or Role.ProjectManager or Role.QuantitySurveyor or Role.SiteManager or Role.HealthSafetyOfficer
            or Role.OfficeComplianceCoordinator or Role.OfficeAdmin or Role.SalesMarketing or Role.Foreman or Role.Accounts);

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        boardView = await ViewStorage.ReadBoardAsync(Auth.CurrentUser!.Email);
        newestFirst = await ViewStorage.ReadNewestFirstAsync(Auth.CurrentUser!.Email);
        // Paint the chrome before the fetches: Blazor re-renders OnInitializedAsync only at its
        // FIRST await, which has already passed, so without this the page waits on the list.
        StateHasChanged();
        if (!Session.IsApproved || !HasInternalRole) { loading = false; return; }

        // The list, the project labels and the assignable-role pool are independent, so they go
        // out together rather than one after another.
        var loads = new List<Task> { LoadAsync(), LoadProjectLabelsAsync() };
        // The role pool feeds the assignee FILTER (MD/admin only) and the add modal's assignee
        // picker (anyone in the manage gate), so either reason is enough to fetch it.
        if (CanSeeAll || CanManage) loads.Add(LoadAssigneeOptionsAsync());
        await Task.WhenAll(loads);
        filtersReady = true;
    }

    // Project labels for the scope chips/filter, and the assignable-role pool for the
    // picker/filter. Either failing degrades the labels, never the list itself.
    private async Task LoadProjectLabelsAsync()
    {
        try { if (Projects.Current is null) await Projects.RefreshAsync(CancellationToken.None); }
        // Degrades the scope filter's labels — and, since the add modal picks a project from this
        // same list, leaves nothing to pick. The picker says so rather than sitting empty.
        catch { projectsFailed = true; }
    }

    private async Task LoadAssigneeOptionsAsync()
    {
        try
        {
            var rolesTask = TodoStore.ListAssignableRolesAsync();
            var peopleTask = TodoStore.ListAssignablePeopleAsync();
            assignableRoles = await rolesTask;
            assignablePeople = await peopleTask;
            assigneeOptions = TodoAssigneePicker.BuildOptions(assignableRoles, assignablePeople);
        }
        catch { }
    }

    private async Task LoadAsync()
    {
        loading = true;
        try { items = CanSeeAll ? await TodoStore.ListAllAsync() : await TodoStore.ListMineAsync(); listFailed = false; }
        catch { error = "Couldn't load the to-do list. Please try again."; listFailed = true; }
        finally { loading = false; }
    }

    // Opening an item is a navigation: everything beyond the row — full detail, linked to-dos,
    // communications, the manage actions — lives on the item's own page.
    private void OpenItem(TodoItem item) => Nav.NavigateTo($"/todos/{item.TodoItemId}");

    // The board's drag onto the other column — this page's one in-place status change. The
    // reloaded list re-files the item under its new status with the server's ordering.
    private async Task SetComplete(TodoItem item, bool complete)
    {
        if (busy || item.IsComplete == complete) return;
        await Run(() => TodoStore.UpdateAsync(new UpdateTodoItem(
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
            // Take the server's fresh open-first/done-pile order: a dragged card re-filing on
            // reload is the expected "moves to the done column" behaviour.
            items = CanSeeAll ? await TodoStore.ListAllAsync() : await TodoStore.ListMineAsync();
        }
        catch (CommandFailedException ex) { error = ex.Message; }
        catch { error = "That action didn't complete. Please try again."; }
        finally { busy = false; }
    }

    private static bool IsOverdue(TodoItem item) =>
        !item.IsComplete && item.DueAt is not null && item.DueAt.Value < DateTimeOffset.Now.Date;

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTimeOffset? ParseDate(string value) =>
        DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
}

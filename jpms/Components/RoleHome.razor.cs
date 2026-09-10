using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Features.Projects;
using Jewel.JPMS.Features.Requests;
using Jewel.JPMS.Features.Subcontractors;
using Jewel.JPMS.Features.Xero;

namespace Jewel.JPMS.Components;

public partial class RoleHome
{
    [Parameter, EditorRequired] public Role Role { get; set; }
    [Parameter, EditorRequired] public string DisplayName { get; set; } = "";
    [Parameter, EditorRequired] public string Email { get; set; } = "";

    /// <summary>How old a library rate may be before the QS is asked to look at it. Matches the
    /// threshold the /rate-library/stale page uses.</summary>
    private const int StaleRateThresholdDays = 60;

    private sealed record Tile(string Label, string Value, string Href, string? Note = null, bool IsBad = false);

    // ---- Which panels this role gets -------------------------------------------------------------
    // Deliberately explicit rather than clever: a landing page that guesses wrong wastes the one
    // screen someone looks at every morning.

    // Site-floor roles do their day (sign in, hours, rejected timesheets) straight from the
    // dashboard — MyDayWorkspace renders nothing for accounts without a worker record.
    private bool ShowMyDay => Role is Role.SiteOperative or Role.Foreman or Role.SiteManager;

    // Mirrors the API's TodoRoles.AssignableAsTodoAssignee: the internal office/management roles a
    // to-do can be assigned to. Everyone else has no list to show.
    private bool ShowMyTodos =>
        Role is Role.ManagingDirector or Role.FinanceDirector or Role.ProjectManager
            or Role.QuantitySurveyor or Role.SiteManager or Role.HealthSafetyOfficer
            or Role.OfficeComplianceCoordinator or Role.OfficeAdmin or Role.SalesMarketing or Role.Accounts;

    // Whoever chases or answers RFIs: the PM owns the change layer, the director watches it, the
    // QS prices what comes out of it, and the architect is usually the one being waited on.
    private bool ShowRequests =>
        Role is Role.ProjectManager or Role.ManagingDirector or Role.QuantitySurveyor
            or Role.Architect or Role.SiteManager;

    private bool ShowValuations =>
        Role is Role.FinanceDirector or Role.ManagingDirector or Role.QuantitySurveyor;

    private bool ShowStaleRates => Role is Role.QuantitySurveyor;

    // The FD's exposure (paying an uninsured subcontractor) and the MD's watch. Deliberately no
    // wider: every row links into /directory, which only Admin, MD, FD and PM may browse — a
    // panel whose links all land on "Not available" is worse than no panel. Widening the
    // audience means widening the directory pages' CanAccess first.
    private bool ShowExpiringDocuments =>
        Role is Role.ManagingDirector or Role.FinanceDirector;

    // Mirrors the API's XeroLedgerRoles.AllowedToAllocate (and the sidebar's FinanceRoles): the
    // finance-facing audience that works — or, for the MD, watches — the allocation queue.
    private bool ShowAllocation =>
        Role is Role.ManagingDirector or Role.FinanceDirector or Role.ProjectManager
            or Role.QuantitySurveyor;

    // Mirrors the API's TriageRoles.AllowedToTriage (and DesktopNavigation.TriageRoles): the
    // people who route inbound mail. The MD is in that gate precisely so this tile can link
    // straight into the queue it counts.
    private bool ShowTriage =>
        Role is Role.ManagingDirector or Role.FinanceDirector or Role.ProjectManager;

    // ---- The count strip --------------------------------------------------------------------------

    private List<Tile> Tiles
    {
        get
        {
            var tiles = new List<Tile>();

            if (ShowRequests && Rfis.Current is not null)
            {
                var open = Rfis.Current.Where(r => r.Status is not RequestStatus.Closed).ToList();
                var overdue = open.Count(r => r.IsOverdue);
                tiles.Add(new("Open RFIs", open.Count.ToString(), "/rfis"));
                tiles.Add(new("Overdue RFIs", overdue.ToString(), "/rfis",
                    Note: overdue > 0 ? "past 7 days" : "all on time", IsBad: overdue > 0));
            }

            if (ShowValuations && Projects.Current is not null)
            {
                var live = Projects.Current.Where(p => p.Stage != ProjectStage.Completed).ToList();
                var overdue = live.Count(ValuationDue.IsOverdue);
                tiles.Add(new("Live projects", live.Count.ToString(), "/projects"));
                tiles.Add(new("Valuations overdue", overdue.ToString(), ValuationDue.OverdueFilterRoute,
                    Note: overdue > 0 ? "needs chasing" : "up to date", IsBad: overdue > 0));
            }

            // The two back-office queues: a non-zero count is work someone is not doing. Each
            // tile appears only once its count has actually landed (null = no fetch yet) — the
            // loading-states convention: never a zero that silently becomes 47.
            if (ShowTriage && inboxTotal is { } toTriage)
                tiles.Add(new("Control Centre inbox", toTriage.ToString(), "/control-centre",
                    Note: toTriage > 0 ? "awaiting routing" : "queue clear", IsBad: toTriage > 0));

            // The Xero figure is what the allocation page will actually ask for — lines to code,
            // Work Order bills to approve, labour lines to mark — NOT the raw Unallocated status
            // count (2026-09-10: that read 44 against a tab bar adding up to 15, the difference
            // being worker bills already covered by timesheets, which the page hides). The
            // server partitions with the page's own recognition, so the two can't disagree.
            if (ShowAllocation && XeroLedger.Counts is { } ledger)
                tiles.Add(new("Xero costs to allocate", ledger.AwaitingAction.ToString(), "/finance/allocation",
                    Note: AllocationNote(ledger), IsBad: ledger.AwaitingAction > 0));

            // Fed by MyTodosPanel's one fetch (OnOpenItemsChanged), so the tiles and the panel
            // below can never disagree about what is open.
            if (ShowMyTodos && openTodos is { } todos)
            {
                var overdueTodos = todos.Count(IsOverdueTodo);
                tiles.Add(new("Open to-dos", todos.Count.ToString(), "/todos"));
                tiles.Add(new("Overdue to-dos", overdueTodos.ToString(), "/todos",
                    Note: overdueTodos > 0 ? "past their due date" : "none late", IsBad: overdueTodos > 0));
            }

            // Fed by ExpiringDocumentsPanel's one fetch — the tile appears only once that read
            // has landed (null = no fetch yet), so it never shows a zero that becomes real later.
            if (ShowExpiringDocuments && Compliance.Current is not null)
            {
                var lapsing = Compliance.Current.Count(document =>
                    document.Status() is ComplianceStatus.ExpiringSoon or ComplianceStatus.Expired);
                tiles.Add(new("Documents expiring", lapsing.ToString(), "/directory/compliance",
                    Note: lapsing > 0 ? "expired or due in 30 days" : "all current", IsBad: lapsing > 0));
            }

            if (ShowStaleRates)
                tiles.Add(new("Stale rates", StaleRateCount.ToString(), "/rate-library/stale",
                    Note: StaleRateCount > 0 ? $"over {StaleRateThresholdDays} days" : "all reviewed",
                    IsBad: StaleRateCount > 0));

            return tiles;
        }
    }

    private int StaleRateCount
    {
        get { try { return Rates.Stale(StaleRateThresholdDays).Count; } catch { return 0; } }
    }

    // Null until the live inbox read lands — the honest "not fetched yet" the tile guards on
    // (a failed read leaves it null and the tile simply never appears; conditional tiles are
    // never gated, per the loading-states convention).
    private int? inboxTotal;

    // Null until MyTodosPanel's load lands. The panel owns the fetch (it needs the rows; we only
    // need the counts) and pushes each change up, including completes and reassigns.
    private IReadOnlyList<TodoItem>? openTodos;

    // Same definition as MyTodosPanel.IsOverdue — the tile and the row highlight must agree.
    private static bool IsOverdueTodo(TodoItem item) =>
        !item.IsComplete && item.DueAt is not null && item.DueAt.Value < DateTimeOffset.Now.Date;

    // The Xero tile's second line: what the figure is made of, in the page's own words — the
    // tabs a reader will meet when they click through ("13 lines · 2 WO bills"). Only the parts
    // that are non-zero; "all coded" when nothing is waiting.
    private static string AllocationNote(XeroLedgerCounts ledger)
    {
        if (ledger.AwaitingAction == 0) return "all coded";
        var parts = new List<string>(3);
        if (ledger.ToCode > 0) parts.Add(ledger.ToCode == 1 ? "1 line" : $"{ledger.ToCode} lines");
        if (ledger.WorkOrderBills > 0) parts.Add(ledger.WorkOrderBills == 1 ? "1 WO bill" : $"{ledger.WorkOrderBills} WO bills");
        if (ledger.LabourOutstanding > 0) parts.Add(ledger.LabourOutstanding == 1 ? "1 labour line" : $"{ledger.LabourOutstanding} labour lines");
        return string.Join(" · ", parts);
    }

    // ---- Navigation cards -------------------------------------------------------------------------

    // One card per visible folder, landing on the folder's first visible row (project-scoped rows
    // resolve against the last-viewed project), then one card per folderless row. Reads the same
    // catalog as the sidebar (DesktopNavigation.FoldersFor / StandaloneItemsFor) so the two can
    // never drift apart — and so a row that leaves the folders keeps its place here.
    private IEnumerable<(string Label, string IconKey, string Href)> Destinations
    {
        get
        {
            var projectId = CurrentProject.ResolveFor(Projects.Current);
            var folders = DesktopNavigation.FoldersFor(Role);
            if (folders.Count == 1)
            {
                // The flatten rule (decision 2026-07-22), mirrored from the sidebar: a role whose
                // whole world is one folder gets a card per row — never a card labelled after a
                // folder it doesn't know it lives in.
                foreach (var item in folders[0].Items)
                {
                    yield return (item.Label, item.Href, item.ResolveHref(projectId));
                }
            }
            else
            {
                foreach (var folder in folders)
                {
                    yield return (folder.Label, folder.IconKey, folder.Items[0].ResolveHref(projectId));
                }
            }
            // Folderless rows come last, as themselves — whole-company destinations, so there is
            // no folder card they could sit behind (SidebarFolders.Standalone).
            foreach (var item in DesktopNavigation.StandaloneItemsFor(Role))
            {
                yield return (item.Label, item.Href, item.ResolveHref(projectId));
            }
        }
    }

    // ---- Chrome -----------------------------------------------------------------------------------

    private string FirstName => FirstWordOf(DisplayName) ?? Role.DisplayName();

    private static string Greeting => DateTimeOffset.Now.Hour switch
    {
        >= 18 => "Evening",
        >= 12 => "Afternoon",
        _     => "Morning"
    };

    // One line saying what this page is for, in the terms of the job — not a description of the
    // software.
    private string RoleBrief => Role switch
    {
        Role.ManagingDirector => "Where the portfolio stands, and what is slipping.",
        Role.FinanceDirector => "What is due to be claimed, and what is late.",
        Role.ProjectManager => "What is waiting on you, and who you are waiting on.",
        Role.QuantitySurveyor => "What needs pricing, and what needs re-checking before it is.",
        Role.SiteManager => "Today on site, and anything holding the work up.",
        Role.HealthSafetyOfficer => "Open actions and anything due for inspection.",
        Role.OfficeComplianceCoordinator => "Paperwork that is about to lapse.",
        Role.OfficeAdmin => "The office work waiting on you.",
        Role.SalesMarketing => "The enquiries and bids waiting on you.",
        Role.Accounts => "The accounts work waiting on you.",
        Role.Architect => "The requests waiting on your response.",
        Role.Client => "Where your project has got to.",
        Role.Foreman or Role.SiteOperative => "Sign in, log your hours, flag anything wrong.",
        _ => "Everything assigned to you, in one place."
    };

    protected override async Task OnInitializedAsync()
    {
        // Group cards resolve against the last-viewed project — re-render when either the
        // remembered project or the project list arrives after first paint.
        Projects.OnChanged += StateHasChanged;
        CurrentProject.OnChange += StateHasChanged;
        Rfis.OnChanged += StateHasChanged;
        XeroLedger.OnChanged += StateHasChanged;
        Compliance.OnChanged += StateHasChanged;
        _ = CurrentProject.EnsureLoadedAsync();

        // Refresh on entry, never from render (the front-end data-loading convention). The RFI
        // register is deliberately NOT refreshed here: OpenRequestsPanel already does it, and both
        // read the same scoped read model — refreshing in both places would fetch it twice. The
        // count tiles guard on Current being present and re-render off OnChanged when it lands.
        // The three reads are independent, so they run concurrently — the slowest of them (the
        // live mailbox read) never holds the others' tiles back. Each helper swallows its own
        // failure: a tile that can't be counted simply doesn't appear, per the tiles' guards.
        await Task.WhenAll(
            RefreshProjectsAsync(),
            RefreshXeroCountsAsync(),
            RefreshInboxTotalAsync());
    }

    private async Task RefreshProjectsAsync()
    {
        try { await Projects.RefreshAsync(CancellationToken.None); }
        catch { /* each panel renders its own empty state */ }
    }

    // The allocation page's own tab-bar counts query (a GROUP BY on the server) — the cheapest
    // honest answer to "is there coding to do", and it can never disagree with the queue itself.
    private async Task RefreshXeroCountsAsync()
    {
        if (!ShowAllocation) return;
        try { await XeroLedger.RefreshCountsAsync(CancellationToken.None); }
        catch { /* the tile guards on Counts being present */ }
    }

    // One message is enough: the queue is read live from the mailbox and the page's Total is the
    // whole pile, so take: 1 keeps the dashboard from shipping a page of emails it never shows.
    private async Task RefreshInboxTotalAsync()
    {
        if (!ShowTriage) return;
        try { inboxTotal = (await Intake.ListInboxLiveAsync(take: 1)).Total; }
        catch { /* the tile guards on inboxTotal being present */ }
    }

    public void Dispose()
    {
        Projects.OnChanged -= StateHasChanged;
        CurrentProject.OnChange -= StateHasChanged;
        Rfis.OnChanged -= StateHasChanged;
        XeroLedger.OnChanged -= StateHasChanged;
        Compliance.OnChanged -= StateHasChanged;
    }

    private static string? FirstWordOf(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return null;
        return fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
    }
}

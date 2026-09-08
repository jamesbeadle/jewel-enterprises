using Jewel.JPMS.Features.Projects;
using Jewel.JPMS.Features.Xero;


namespace Jewel.JPMS.Pages;

public partial class XeroAllocation
{
    // Session checked and the user is signed in. This is NOT "the ledger is here" — the heading,
    // the Sync and Re-check buttons show at once; the tab bar and the rows wait behind their own
    // gates.

    // Which tab is open decides which status is fetched and which counts the bar shows, so the
    // ledger section waits for the remembered choice rather than opening on the wrong tab.
    private bool tabRestored;

    private bool isSyncing;
    private bool isRechecking;
    private bool isApplying;
    private string search = string.Empty;
    private string? syncMessage;
    private string? errorMessage;
    private XeroAllocationStatus activeTab = XeroAllocationStatus.Unallocated;

    // Which project tab of the incoming queue is open. Only meaningful while
    // activeTab is Unallocated: null is the plain "Unallocated" tab (no project
    // set yet); otherwise the tab for that project. Grouping follows only what's
    // real — the persisted project (Set) or the Xero suggestion — never the
    // dropdown pick, so choosing a project doesn't move the row anywhere until
    // Set or Allocate makes it official.
    private string? activeProjectId;

    private readonly HashSet<string> selectedIds = new();
    private readonly Dictionary<string, string?> chosenProject = new();
    private readonly Dictionary<string, string?> chosenCostCenter = new();
    private readonly Dictionary<string, string?> chosenBucket = new();
    private string bulkProjectId = "";
    private string bulkCostCenterCode = "";
    private string bulkBucket = "";
    private string? bucketFilter;

    // -- Labour section state (scope §6 recognition) ---------------------------
    // labourTab is a sub-view of the Unallocated status, like activeProjectId: the tab the
    // recognised settlement bills fall in instead of the queue. Covered lines fold away once
    // marked (showCoveredLabour reveals them); notLabourIds is this visit's escape hatch for a
    // false match — the line rejoins the plain queue for a normal allocation. Recognition is
    // recomputed server-side on every ledger read, so the set is not persisted: a mis-match is
    // fixed properly on the Workers page (rename/deactivate), not remembered here.
    private bool labourTab;
    private bool showCoveredLabour;
    private readonly HashSet<string> notLabourIds = new();

    // -- Allocated-tab recode state --------------------------------------------
    // "" = all projects. The filter narrows the Allocated tab to one project so
    // its coding can be reviewed, selected and re-sent in bulk.
    private string allocatedProjectFilter = "";
    private bool sendToCostCentreOpen;
    private string sendCostCenterCode = "";
    private string? sendError;
    private bool sendToProjectOpen;
    private string sendProjectId = "";
    private string? sendProjectError;

    // -- Dispute state ---------------------------------------------------------
    // disputeLine: the line the "Dispute…" modal is composing an opening message
    // for (a snapshot is fine — nothing about it changes while composing).
    // discussLineId: the DISPUTED line whose thread modal is open — held as an id
    // and re-resolved from the store each render, so a sent reply (which reloads
    // the tab) shows up in the open modal instead of a stale snapshot.
    private XeroLedgerLine? disputeLine;
    private string disputeMessage = "";
    private string? disputeError;
    private string? discussLineId;
    private string discussMessage = "";
    private string? discussError;

    private XeroLedgerLine? DiscussLine =>
        discussLineId is null
            ? null
            : Ledger.Lines(XeroAllocationStatus.Disputed)?.FirstOrDefault(line => line.XeroLedgerLineId == discussLineId);

    private bool isBusy => isSyncing || isRechecking || isApplying;

    // -- Panel gates. Each lists every source the panel reads, so it appears in one piece. --
    // Both panels name their rows through the project list and the cost-centre master, and both
    // offer them as dropdown options — neither can be drawn before those land.
    private bool MastersReady => ProjectsReadModel.Current is not null && CostCenters.IsLoaded;

    private bool TabBarReady => Ledger.Lines(XeroAllocationStatus.Unallocated) is not null && MastersReady;

    private bool QueueReady => Lines is not null && MastersReady;

    // The lines for the tab currently open. Reading this starts that status' load if it hasn't
    // happened yet, so a tab switch fetches just that tab.
    private IReadOnlyList<XeroLedgerLine>? Lines => Ledger.Lines(activeTab);

    // The unallocated queue, which the tab bar itself is built from (its project tabs and their
    // counts) and which the "allocate all matched" banner counts against — so it is loaded whatever
    // tab is open. On the Unallocated tab this is the same list as Lines.
    private IReadOnlyList<XeroLedgerLine> UnallocatedLines =>
        Ledger.Lines(XeroAllocationStatus.Unallocated) ?? Array.Empty<XeroLedgerLine>();

    private IReadOnlyList<Jewel.JPMS.Models.Project> Projects =>
        ProjectsReadModel.Current ?? Array.Empty<Jewel.JPMS.Models.Project>();

    // -- filtering & paging ----------------------------------------------------
    // The filtered list is memoized: Blazor reads Visible several times per render
    // and with thousands of lines the repeated LINQ passes (and re-rendering every
    // row's dropdowns) is what made tab switches crawl. Only PageSize rows render.

    private const int PageSize = 50;
    private int page;
    private IReadOnlyList<XeroLedgerLine>? visibleCache;
    private (IReadOnlyList<XeroLedgerLine>? Lines, object Projects, string Search, XeroAllocationStatus Tab,
             string? ProjectTab, string? Bucket, string AllocatedProject,
             bool LabourTab, bool ShowCovered, int NotLabour, bool WorkOrderBillsTab, int NotWorkOrderBill, string XeroFilter) visibleCacheKey;

    private IReadOnlyList<XeroLedgerLine> Visible
    {
        get
        {
            // Projects is in the key because GroupProjectFor validates against it.
            var key = (Lines, (object)Projects, search, activeTab, activeProjectId, bucketFilter, allocatedProjectFilter,
                       labourTab, showCoveredLabour, notLabourIds.Count, workOrderBillsTab, notWorkOrderBillInvoiceIds.Count, allocatedXeroFilter);
            if (visibleCache is null || key != visibleCacheKey)
            {
                visibleCache = Lines is null
                    ? Array.Empty<XeroLedgerLine>()
                    // Filtering by status is the server's job now — Lines IS the open tab's set —
                    // but the check stays as a cheap guard against rendering the previous tab's
                    // rows in the moment between a switch and its fetch landing.
                    : Lines.Where(line => line.AllocationStatus == activeTab)
                           // The queue, the Labour section and the Work Order bills tab partition
                           // the unallocated set: a labour-recognised line renders only in Labour,
                           // a work-order-matched one only on its card, everything else only in
                           // the plain/project queue — nothing appears twice, nothing disappears.
                           .Where(line => activeTab != XeroAllocationStatus.Unallocated
                                          || (labourTab
                                              ? IsLabourLine(line) && (showCoveredLabour || !line.CoveredByTimesheets)
                                              : workOrderBillsTab
                                                  ? IsWorkOrderBillLine(line)
                                                  : !IsLabourLine(line) && !IsWorkOrderBillLine(line)
                                                    && GroupProjectFor(line) == (activeProjectId ?? "")))
                           .Where(line => activeTab != XeroAllocationStatus.Bucketed
                                          || bucketFilter is null || line.Bucket == bucketFilter)
                           .Where(line => activeTab != XeroAllocationStatus.Allocated
                                          || allocatedProjectFilter == ""
                                          || MatchesAllocatedProjectFilter(line))
                           .Where(line => activeTab != XeroAllocationStatus.Allocated || MatchesAllocatedXeroFilter(line))
                           .Where(MatchesSearch)
                           .ToList();
                visibleCacheKey = key;
                page = Math.Clamp(page, 0, Math.Max(0, (visibleCache.Count - 1) / PageSize));
            }
            return visibleCache;
        }
    }


    private int PageCount => Math.Max(1, (Visible.Count + PageSize - 1) / PageSize);

    private IEnumerable<XeroLedgerLine> Paged =>
        Visible.Skip(page * PageSize).Take(PageSize);

    private void OnSearchChanged(ChangeEventArgs args)
    {
        search = args.Value?.ToString() ?? string.Empty;
        page = 0;
    }

    private bool MatchesAllocatedProjectFilter(XeroLedgerLine line) =>
        line.ProjectId == allocatedProjectFilter
        || (line.Splits?.Any(split => (split.ProjectId ?? line.ProjectId) == allocatedProjectFilter) ?? false);

    private void OnAllocatedProjectFilterChanged(string? projectId)
    {
        allocatedProjectFilter = projectId ?? "";
        page = 0;
    }

    // The filter takes ProjectOptions as-is: SearchSelect already leads its unfiltered list with a
    // blank entry labelled with the Placeholder ("All projects"), so adding one here printed it
    // twice.



    private bool MatchesSearch(XeroLedgerLine line)
    {
        if (string.IsNullOrWhiteSpace(search)) return true;
        var term = search.Trim();
        return Contains(line.ContactName) || Contains(line.Description) || Contains(line.InvoiceNumber)
            || Contains(line.Reference) || Contains(line.XeroSite) || Contains(line.XeroCostCode)
            // Allocated lines also match on the centre(s) they were coded to —
            // code or name (CostCenterText carries both) — so a mis-coded run
            // can be found by where it landed, then re-sent in bulk.
            || (line.CostCenterCode is not null && Contains(CostCenterText(line.CostCenterCode)))
            || (line.Splits?.Any(split => Contains(CostCenterText(split.CostCenterCode))) ?? false);

        bool Contains(string? value) =>
            value is not null && value.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private string ProjectName(string? projectId) =>
        Projects.FirstOrDefault(project => project.ProjectId == projectId)?.Name ?? projectId ?? "—";

    private string CostCenterText(string? code)
    {
        if (code is null) return "—";
        var centre = CostCenters.All().FirstOrDefault(c => c.Code == code);
        return centre is null ? code : $"{centre.Code} {centre.Name}";
    }


    private static decimal SignedNet(XeroLedgerLine line) => XeroLedgerDisplay.SignedNet(line);



    public void Dispose()
    {
        Ledger.OnChange -= StateHasChanged;
        CostCenters.OnChange -= StateHasChanged;
        ProjectsReadModel.OnChanged -= StateHasChanged;
        Subcontractors.OnChange -= StateHasChanged;
    }
}

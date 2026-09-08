using Jewel.JPMS.Features.Projects;


namespace Jewel.JPMS.Pages;

public partial class XeroAllocation
{

    // -- Excel export -----------------------------------------------------------
    // Exports the open tab as filtered: the current page by default, the whole
    // filtered set when "Include all rows" is picked from the export menu.

    private ExcelWorkbook? BuildExportWorkbook(bool includeAllRows)
    {
        var lines = (includeAllRows ? Visible : Paged.ToList() as IReadOnlyList<XeroLedgerLine>);
        if (lines.Count == 0) return null;

        var sheetName = activeTab == XeroAllocationStatus.Unallocated
            ? (labourTab ? "Labour" : activeProjectId is null ? "Unallocated" : ProjectName(activeProjectId))
            : activeTab == XeroAllocationStatus.Bucketed ? "Buckets"
            : activeTab.ToString();

        var columns = new List<ExcelColumn>
        {
            new("Date", ExcelFormat.Date),
            new("Supplier"),
            new("Description"),
            new("Invoice no"),
            new("Xero site"),
            new("Xero cost code"),
            new("Net", ExcelFormat.Currency),
        };
        if (activeTab == XeroAllocationStatus.Unallocated && labourTab)
        {
            columns.Add(new ExcelColumn("Worker"));
            columns.Add(new ExcelColumn("Settlement"));
        }
        else if (activeTab == XeroAllocationStatus.Allocated)
        {
            columns.Add(new ExcelColumn("Allocated to"));
            columns.Add(new ExcelColumn("Cost centre"));
            columns.Add(new ExcelColumn("Xero status"));
            columns.Add(new ExcelColumn("Write-back"));
            columns.Add(new ExcelColumn("Last write-back error"));
        }
        else if (activeTab == XeroAllocationStatus.Bucketed)
        {
            columns.Add(new ExcelColumn("Bucket"));
        }
        else if (activeTab == XeroAllocationStatus.Ignored)
        {
            columns.Add(new ExcelColumn("Reason"));
        }
        else if (activeTab == XeroAllocationStatus.Disputed)
        {
            columns.Add(new ExcelColumn("Proposed project"));
            columns.Add(new ExcelColumn("Proposed cost centre"));
            columns.Add(new ExcelColumn("Latest message"));
        }
        if (activeTab != XeroAllocationStatus.Unallocated)
        {
            columns.Add(new ExcelColumn("By"));
            columns.Add(new ExcelColumn("Allocated on", ExcelFormat.Date));
        }

        var workbook = new ExcelWorkbook();
        var sheet = workbook.AddSheet(sheetName, columns.ToArray());
        foreach (var line in lines)
        {
            var cells = new List<object?>
            {
                line.Date,
                line.ContactName,
                line.Description,
                line.InvoiceNumber,
                line.XeroSite,
                line.XeroCostCode,
                SignedNet(line),
            };
            if (activeTab == XeroAllocationStatus.Unallocated && labourTab)
            {
                cells.Add(line.MatchedWorkerName);
                cells.Add(line.CoveredByTimesheets
                    ? $"Covered · {line.CoveredPeriodStart:MMM yyyy}"
                    : "Outstanding");
            }
            else if (activeTab == XeroAllocationStatus.Allocated)
            {
                if (line.Splits is { Count: > 0 })
                {
                    var multiProject = line.Splits.Select(split => split.ProjectId ?? line.ProjectId)
                        .Distinct().Count() > 1;
                    cells.Add(multiProject ? "Multiple projects" : ProjectName(line.ProjectId ?? line.Splits[0].ProjectId));
                    cells.Add(string.Join("; ", line.Splits.Select(split =>
                        (multiProject ? $"{ProjectName(split.ProjectId ?? line.ProjectId)} · " : "")
                        + $"{CostCenterText(split.CostCenterCode)} {Money(line.Type == "ACCPAYCREDIT" ? -split.Net : split.Net)}")));
                }
                else
                {
                    cells.Add(ProjectName(line.ProjectId));
                    cells.Add(CostCenterText(line.CostCenterCode));
                }
                cells.Add(XeroStatusText(line));
                cells.Add(WriteBackText(line));
                cells.Add(line.WriteBackError);
            }
            else if (activeTab == XeroAllocationStatus.Bucketed)
            {
                cells.Add(line.Bucket);
            }
            else if (activeTab == XeroAllocationStatus.Ignored)
            {
                cells.Add(line.Note);
            }
            else if (activeTab == XeroAllocationStatus.Disputed)
            {
                cells.Add(line.ProjectId is null ? null : ProjectName(line.ProjectId));
                cells.Add(line.CostCenterCode is null ? null : CostCenterText(line.CostCenterCode));
                cells.Add(line.DisputeMessages?.LastOrDefault()?.Body ?? line.Note);
            }
            if (activeTab != XeroAllocationStatus.Unallocated)
            {
                cells.Add(line.AllocatedBy);
                cells.Add(line.AllocatedAtUtc?.ToLocalTime());
            }
            sheet.AddRow(cells.ToArray());
        }
        return workbook;
    }

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        Ledger.OnChange += StateHasChanged;
        CostCenters.OnChange += StateHasChanged;
        ProjectsReadModel.OnChanged += StateHasChanged;
        await RestoreLastTabAsync();
        tabRestored = true;

        if (ProjectsReadModel.Current is null) _ = ProjectsReadModel.RefreshAsync(CancellationToken.None);
        // The cost-centre master names every allocated row and fills both dropdowns. It used to be
        // pulled in by the first render that read it; behind a gate nothing reads it until it has
        // landed, so the load has to be started here.
        _ = CostCenters.ListAllAsync();
        // The directory feeds the Labour tab's inline link picker (2026-08-31); lazily loaded,
        // so the fetch has to be started here or the picker offers nothing forever.
        Subcontractors.OnChange += StateHasChanged;
        Subcontractors.All();
        // Load the tab being opened (RestoreLastTabAsync may have restored a different one) plus,
        // when that isn't the unallocated queue, the queue itself — the tab bar's project tabs and
        // the "allocate all matched" banner are built from it.
        _ = Ledger.RefreshAsync(activeTab);
        if (activeTab != XeroAllocationStatus.Unallocated)
            _ = Ledger.RefreshAsync(XeroAllocationStatus.Unallocated);
    }

    /// <summary>
    /// Re-runs the matching without touching Xero: reloads the project list (new
    /// projects added since the page opened) and re-reads the ledger — the API
    /// recomputes every unallocated line's suggestions against the live project
    /// and cost-centre masters on each read, so new projects get suggested and
    /// the "Allocate all matched" banner updates to include them.
    /// </summary>
    private async Task RecheckMatchesAsync()
    {
        isRechecking = true; syncMessage = null; errorMessage = null;
        try
        {
            var matchedBefore = FullyMatchedCount;
            await ProjectsReadModel.RefreshAsync(CancellationToken.None);
            // Suggestions are recomputed server-side on read, and they only exist on unallocated
            // lines — so that is the status to re-read.
            await Ledger.RefreshAsync(XeroAllocationStatus.Unallocated);
            var newlyMatched = FullyMatchedCount - matchedBefore;
            syncMessage = newlyMatched > 0
                ? $"Re-checked: {newlyMatched} more lines now match a project and cost centre — {FullyMatchedCount} matched in total."
                : $"Re-checked against the current project list: {FullyMatchedCount} lines fully matched.";
        }
        catch (CommandFailedException failure) { errorMessage = failure.Message; }
        finally { isRechecking = false; }
    }

    private async Task SyncAsync()
    {
        isSyncing = true; syncMessage = null; errorMessage = null;
        try
        {
            var result = await Ledger.SyncAsync();
            syncMessage = !result.IsConfigured
                ? "Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings."
                : result.Error is not null
                    ? $"Xero returned an error: {result.Error}"
                    : $"Synced. {result.NewLines} new lines, {result.UpdatedLines} refreshed"
                      + (result.RemovedLines > 0 ? $", {result.RemovedLines} voided/deleted lines removed from the queue" : "")
                      + $" — {result.UnallocatedLines} of {result.TotalLines} now awaiting allocation.";
        }
        catch (CommandFailedException failure) { errorMessage = failure.Message; }
        finally { isSyncing = false; }
    }

    private async Task AllocateAsync(XeroLedgerLine line) =>
        await ApplyAsync(new SetXeroAllocation(
            new[] { line.XeroLedgerLineId },
            XeroAllocationAction.Allocate,
            SelectedProjectFor(line),
            SelectedCostCenterFor(line)));

    private async Task IgnoreAsync(XeroLedgerLine line) =>
        await ApplyAsync(new SetXeroAllocation(new[] { line.XeroLedgerLineId }, XeroAllocationAction.Ignore));

    private async Task ResetAsync(XeroLedgerLine line) =>
        await ApplyAsync(new SetXeroAllocation(new[] { line.XeroLedgerLineId }, XeroAllocationAction.Reset));

    // A line approved as a Work Order bill undoes as a bill (the confirm); any other line resets alone.
    private Task UndoAllocatedAsync(XeroLedgerLine line)
    {
        if (line.WorkOrderApproval is null) return ResetAsync(line);
        OpenUndoWorkOrderBill(line);
        return Task.CompletedTask;
    }

    private async Task BulkAllocateAsync() =>
        await ApplyAsync(new SetXeroAllocation(selectedIds.ToList(), XeroAllocationAction.Allocate, bulkProjectId, bulkCostCenterCode));

    // -- SetProject: the half-step (project saved + Xero site written, line stays queued) --

    // A project pick alone arms Set. A chosen or suggested cost centre must NOT
    // block it: accountant-coded lines arrive with a Xero cost-code suggestion
    // pre-filling that dropdown, and Set is exactly how a wrong site guess gets
    // moved to the right project's tab (decision 2026-08-14 — it used to require
    // an empty cost centre, which greyed Set out on precisely those lines). Only
    // a bucket choice competes for the row's intent; when both project + centre
    // are armed, Allocate and Set sit side by side as the full and half steps.
    private bool CanSetProject(XeroLedgerLine line) =>
        !string.IsNullOrEmpty(SelectedProjectFor(line))
        && !CanBucket(line);

    private async Task SetProjectAsync(XeroLedgerLine line) =>
        await ApplyAsync(new SetXeroAllocation(
            new[] { line.XeroLedgerLineId },
            XeroAllocationAction.SetProject,
            SelectedProjectFor(line)));

    // SetProject with no project = unset: clears the saved project (the line
    // returns to the plain Unallocated tab, unless a suggestion still claims it)
    // and removes the Site tracking from the still-draft bill in Xero.
    private async Task UnsetProjectAsync(XeroLedgerLine line) =>
        await ApplyAsync(new SetXeroAllocation(
            new[] { line.XeroLedgerLineId },
            XeroAllocationAction.SetProject));

    private async Task BulkSetProjectAsync() =>
        await ApplyAsync(new SetXeroAllocation(selectedIds.ToList(), XeroAllocationAction.SetProject, bulkProjectId));

    private async Task BulkIgnoreAsync() =>
        await ApplyAsync(new SetXeroAllocation(selectedIds.ToList(), XeroAllocationAction.Ignore));

    // -- Allocated-tab bulk recode ---------------------------------------------
    // Undoing and re-coding one line at a time doesn't scale when a whole run of
    // costs went to the wrong centre. The Allocated tab therefore multi-selects
    // like the queue does, and "Send to cost centre" re-allocates the selection
    // to one centre — each line keeping the project it's already on. The API
    // allocates a batch to a single project + centre, so the selection is sent
    // as one command per project. Re-allocating replaces any stored split, so a
}

using Jewel.JPMS.Features.Directory;

namespace Jewel.JPMS.Pages;

public partial class ComplianceRegister
{
    private string search = "";
    private string statusFilter = DirectoryComplianceFilter.All;
    private bool dataFailed;

    // Widened for the unified directory (2026-07-22): Admin, MD, FD and PM may browse.
    private bool CanAccess => Session.AvailableRoles.Any(r =>
        r is Role.Admin or Role.ManagingDirector or Role.FinanceDirector or Role.ProjectManager);

    private bool IsLoaded => SubcontractorStore.IsLoaded && Compliance.Current is not null;

    private bool FiltersActive => !string.IsNullOrWhiteSpace(search) || statusFilter != DirectoryComplianceFilter.All;

    // Rebuilt when either source changes, not per render — the chips, the summary and the table
    // all read the same build.
    private IReadOnlyList<ComplianceRegisterRow> allRows = Array.Empty<ComplianceRegisterRow>();

    private void RebuildRows() =>
        allRows = IsLoaded
            ? ComplianceRegisterRow.Build(DirectoryCompanies(), Compliance.Current!)
            : Array.Empty<ComplianceRegisterRow>();

    private IReadOnlyList<ComplianceRegisterRow> FilteredRows =>
        allRows.Where(PassesStatusFilter).Where(MatchesSearch).ToList();

    private IReadOnlyList<TabItem> StatusChips =>
        DirectoryComplianceFilter.Chips(status => IsLoaded ? allRows.Count(row => row.Status == status) : null);

    private string Summary
    {
        get
        {
            var companies = DirectoryCompanies().Count;
            var lapsing = allRows.Count(row => row.Status is ComplianceStatus.Expired or ComplianceStatus.ExpiringSoon);
            return $"{companies} companies · {lapsing} document{(lapsing == 1 ? "" : "s")} expired or due within 30 days.";
        }
    }

    private bool PassesStatusFilter(ComplianceRegisterRow row) => DirectoryComplianceFilter.Passes(row.Status, statusFilter);

    private bool MatchesSearch(ComplianceRegisterRow row)
    {
        var query = search.Trim();
        if (query.Length == 0) return true;
        return row.Company.CompanyName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || row.Company.TradesLabel.Contains(query, StringComparison.OrdinalIgnoreCase)
            || row.DocumentLabel.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    // Tender-only prospects stay out, exactly as on the Directory page.
    private IReadOnlyList<Subcontractor> DirectoryCompanies() =>
        SubcontractorStore.All().Where(company => !company.IsProspect).ToList();

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        SubcontractorStore.OnChange += OnStoreChanged;
        Compliance.OnChanged += OnStoreChanged;
        _ = SubcontractorStore.All();
        _ = RefreshComplianceAsync();
        RebuildRows();
    }

    private async Task RefreshComplianceAsync()
    {
        try { await Compliance.RefreshAsync(CancellationToken.None); }
        catch { dataFailed = true; }
        RebuildRows();
        StateHasChanged();
    }

    public void Dispose()
    {
        SubcontractorStore.OnChange -= OnStoreChanged;
        Compliance.OnChanged -= OnStoreChanged;
    }

    private void OnStoreChanged()
    {
        RebuildRows();
        InvokeAsync(StateHasChanged);
    }

    private void Open(string subcontractorId) => Nav.NavigateTo($"/directory/{subcontractorId}");
}

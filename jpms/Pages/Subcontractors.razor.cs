using Jewel.JPMS.Contracts.Subcontractors;
using static Jewel.JPMS.Features.Directory.DirectoryDisplay;

namespace Jewel.JPMS.Pages;

public partial class Subcontractors
{
    private string search = "";
    private string categoryFilter = ""; // "" = all
    private string xeroFilter = XeroFilterAll;

    private const string XeroFilterAll = "all";
    private const string XeroFilterLinked = "linked";
    private const string XeroFilterUnlinked = "unlinked";

    // Counts only once the directory has loaded — a chip saying "Not linked 0" before the fetch
    // lands is the zero-then-value flash the loading convention forbids.
    private IReadOnlyList<TabItem> XeroFilterChips
    {
        get
        {
            var loaded = SubcontractorStore.IsLoaded;
            var companies = loaded ? DirectoryCompanies() : Array.Empty<Subcontractor>();
            return new[]
            {
                new TabItem(XeroFilterAll, "All"),
                new TabItem(XeroFilterLinked, "Linked to Xero", Count: loaded ? companies.Count(s => s.XeroLinked) : null),
                new TabItem(XeroFilterUnlinked, "Not linked to Xero", Count: loaded ? companies.Count(s => !s.XeroLinked) : null,
                    Title: "Records with no Xero contact — link each from its page, or from Import from Xero when the names match")
            };
        }
    }

    private static bool PassesXeroFilter(Subcontractor company, string filter) => filter switch
    {
        XeroFilterLinked => company.XeroLinked,
        XeroFilterUnlinked => !company.XeroLinked,
        _ => true
    };

    // Widened for the unified directory (2026-07-22): Admin, MD, FD and PM may browse.
    private bool CanAccess => Session.AvailableRoles.Any(r =>
        r is Role.Admin or Role.ManagingDirector or Role.FinanceDirector or Role.ProjectManager);

    // Adding companies mirrors the API's add authorisation (Admin, MD, FD — the API also allows
    // Compliance, but that role can't reach this page). FD added 2026-07-28.
    private bool CanManage => Session.AvailableRoles.Any(r =>
        r is Role.Admin or Role.ManagingDirector or Role.FinanceDirector);

    // ---- Group chips -------------------------------------------------------
    private enum DirectoryGroup { Clients, Architects, Subcontractors, Staff }

    // Subcontractors default: the company directory is what /directory has always shown.
    private DirectoryGroup group = DirectoryGroup.Subcontractors;

    private static readonly (DirectoryGroup Group, string Label)[] GroupChips =
    {
        (DirectoryGroup.Clients,        "Clients"),
        (DirectoryGroup.Architects,     "Architects"),
        (DirectoryGroup.Subcontractors, "Subcontractors"),
        (DirectoryGroup.Staff,          "Internal staff")
    };

    private string GroupChipClass(DirectoryGroup value) =>
        group == value
            ? "chip chip-active"
            : "chip";

    private string GroupSummary => group switch
    {
        DirectoryGroup.Clients => clientsLoaded
            ? $"{clients.Count} client account{(clients.Count == 1 ? "" : "s")}."
            : "Client accounts.",
        DirectoryGroup.Architects => architectsLoaded
            ? $"{architects.Count} architect practice{(architects.Count == 1 ? "" : "s")}."
            : "Architect practices.",
        DirectoryGroup.Staff => staffLoaded
            ? $"{StaffUsers.Count} internal staff."
            : "Internal staff.",
        // The companies count lives beside the filters (CountSummary) — one figure per screen,
        // and no "0 of 0" here before the store has loaded.
        _ => "The company directory."
    };

    private bool FiltersActive =>
        !string.IsNullOrWhiteSpace(search) || !string.IsNullOrWhiteSpace(categoryFilter) || xeroFilter != XeroFilterAll;

    // "12 of 118 companies" while the search or Type filter is narrowing the table; the plain
    // total otherwise. Only rendered once SubcontractorStore.IsLoaded, so the figure is real.
    private string CountSummary
    {
        get
        {
            var total = DirectoryCompanies().Count;
            var noun = total == 1 ? "company" : "companies";
            return FiltersActive
                ? $"{Filtered().Count} of {total} {noun}"
                : $"{total} {noun}";
        }
    }

    // Each group loads on first selection, best-effort: the groups keep their own API gates
    // (clients: Admin/MD/PM; staff: Admin/FD), so a 403 for this user's roles shows a quiet
    // restriction note rather than failing the page.
    private void SelectGroup(DirectoryGroup value)
    {
        group = value;
        _ = value switch
        {
            DirectoryGroup.Clients => LoadClientsAsync(),
            DirectoryGroup.Architects => LoadArchitectsAsync(),
            DirectoryGroup.Staff => LoadStaffAsync(),
            _ => Task.CompletedTask
        };
    }

    private IReadOnlyList<Client> clients = Array.Empty<Client>();
    private bool clientsLoaded;
    private string? clientsError;

    private async Task LoadClientsAsync()
    {
        if (clientsLoaded) return;
        try
        {
            clients = await ClientStore.ListAsync();
            clientsLoaded = true;
            clientsError = null;
        }
        catch { clientsError = "Client accounts couldn't be loaded — they're restricted to administrators, the managing director and project managers."; }
        StateHasChanged();
    }

    private IReadOnlyList<Architect> architects = Array.Empty<Architect>();
    private bool architectsLoaded;
    private string? architectsError;

    private async Task LoadArchitectsAsync()
    {
        if (architectsLoaded) return;
        try
        {
            architects = await ArchitectStore.ListAsync();
            architectsLoaded = true;
            architectsError = null;
        }
        catch { architectsError = "Architect practices couldn't be loaded — they're restricted to administrators, the managing director and project managers."; }
        StateHasChanged();
    }

    private bool staffLoaded;
    private string? staffError;

    private IReadOnlyList<DirectoryUser> StaffUsers =>
        Staff.Current ?? (IReadOnlyList<DirectoryUser>)Array.Empty<DirectoryUser>();

    private async Task LoadStaffAsync()
    {
        if (staffLoaded) return;
        try
        {
            await Staff.RefreshAsync(CancellationToken.None);
            staffLoaded = true;
            staffError = null;
        }
        catch { staffError = "The internal staff list couldn't be loaded — it's restricted to administrators and the finance director."; }
        StateHasChanged();
    }

    // Add-company form state
    private bool showAddModal;
    private bool saving;
    // The add-company fields live in DirectoryContactForm now — the Modal renders it fresh on
    // each open, and this page owns only the send.
    private DirectoryContactForm? addForm;

    private void OpenAddModal() => showAddModal = true;

    private void CloseAddModal() => showAddModal = false;

    private async Task SaveCompany()
    {
        if (saving || addForm is null) return;
        await addForm.TrySubmitAsync(); // a valid form comes back through SendCompanyAsync
    }

    private Task SendCompanyAsync(DirectoryContactForm.Draft draft)
    {
        try
        {
            saving = true;
            SubcontractorStore.Upsert(draft.Company);
            showAddModal = false;
        }
        catch { addForm?.ShowError("Couldn't add the company. Please try again."); }
        finally { saving = false; }
        return Task.CompletedTask;
    }

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        SubcontractorStore.OnChange += OnStoreChanged;
        Compliance.OnChanged += OnStoreChanged;
        _ = SubcontractorStore.All(); // kick off the directory load (async; raises OnChange)
        _ = SubcontractorStore.Trades(); // and the curated trade list for the add-company form
        _ = RefreshComplianceAsync(); // and the compliance column's whole-company read
    }

    // Store convention: refresh once from OnInitializedAsync. The failure is swallowed — the
    // column's pills simply never appear, and the query client has already raised the error toast.
    private async Task RefreshComplianceAsync()
    {
        try { await Compliance.RefreshAsync(CancellationToken.None); }
        catch { }
    }

    public void Dispose()
    {
        SubcontractorStore.OnChange -= OnStoreChanged;
        Compliance.OnChanged -= OnStoreChanged;
    }

    private void OnStoreChanged() => InvokeAsync(StateHasChanged);

    private void OnSearchInput(ChangeEventArgs e) => search = e.Value?.ToString() ?? "";

    private void OnCategoryChanged(ChangeEventArgs e) => categoryFilter = e.Value?.ToString() ?? "";

    // The directory proper: tender-only prospects (companies minted so a bid-package tender list
    // could hold them — see Subcontractor.IsProspect) are excluded everywhere on this page. They
    // join the list only when promoted from a submitted tender, or when a package is awarded to
    // them — that is what keeps the directory a curated list rather than everyone ever invited.
    private IReadOnlyList<Subcontractor> DirectoryCompanies() =>
        SubcontractorStore.All().Where(s => !s.IsProspect).ToList();

    private IReadOnlyList<Subcontractor> Filtered()
    {
        var q = (search ?? "").Trim();
        DirectoryCategory? cat = Enum.TryParse<DirectoryCategory>(categoryFilter, out var c) ? c : null;
        return DirectoryCompanies()
            .Where(s => cat is null || s.Category == cat)
            .Where(s => PassesXeroFilter(s, xeroFilter))
            .Where(s => q.Length == 0
                || (s.CompanyName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                || s.Trades.Any(t => t.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                || (s.ContactName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                || (s.ContactEmail ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                || (s.Town ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                || (s.County ?? "").Contains(q, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.CompanyName)
            .ToList();
    }

    private void Open(string id) => Nav.NavigateTo($"/directory/{id}");

}

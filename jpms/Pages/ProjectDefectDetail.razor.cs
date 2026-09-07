using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Features.Closeout;
using Jewel.JPMS.Features.Closeout.Detail;
using Jewel.JPMS.Features.Todos;

namespace Jewel.JPMS.Pages;

// The defect page's state and commands. The page owns the DEFECT (and the actions that change
// it: status, Edit, the send-to-supplier kick-off); the communications and to-dos panels own their
// reads. The gate mirrors are the same expressions the register and the to-do page use — the
// API's role sets decide for real, these only decide what controls to offer.
public partial class ProjectDefectDetail
{
    [Parameter] public string ProjectId { get; set; } = "";
    [Parameter] public string DefectId { get; set; } = "";

    private bool loading = true;
    private bool loadFailed;
    private Defect? defect;
    private bool busy;
    private string? error;
    private DefectCommunicationsPanel? communications;
    private IReadOnlyList<SearchSelect.Option> assigneeOptions = Array.Empty<SearchSelect.Option>();

    // Edit dialog
    private bool editOpen;
    private string? editError;
    private string editLocation = "";
    private string editDescription = "";
    private string editAssignedTo = "";
    private string editSubcontractorId = "";

    // Mirrors the API's UpdateDefectAuthorisation (Director, PM, Site Manager; Admin passes every gate).
    private bool CanEdit =>
        Session.AvailableRoles.Any(role => role is Role.Admin or Role.ManagingDirector or Role.ProjectManager or Role.SiteManager);

    // Mirrors the API's JpmsRoleSets.AllInternal — who may read the page and send from the
    // projects mailbox (the compose gate).
    private bool HasInternalRole =>
        Session.AvailableRoles.Any(role => role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector
            or Role.ProjectManager or Role.QuantitySurveyor or Role.SiteManager or Role.HealthSafetyOfficer
            or Role.OfficeComplianceCoordinator or Role.OfficeAdmin or Role.SalesMarketing or Role.Foreman or Role.Accounts);

    private bool CanSend => HasInternalRole;

    // Mirrors the API's TodoRoles.AllowedToManageTodos — who may raise a to-do about the defect.
    private bool CanManageTodos =>
        Session.AvailableRoles.Any(role => role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector
            or Role.ProjectManager or Role.SiteManager or Role.Accounts);

    private Project? Project => (Projects.Current ?? Array.Empty<Project>()).FirstOrDefault(p => p.ProjectId == ProjectId);
    private IReadOnlyList<Project> ProjectPool => Projects.Current ?? (IReadOnlyList<Project>)Array.Empty<Project>();
    private Subcontractor? Supplier => defect?.SubcontractorId is { } id ? Directory.Find(id) : null;
    private IReadOnlyList<SearchSelect.Option> SupplierOptions => DefectSuppliers.Options(Directory.All());

    protected override async Task OnInitializedAsync()
    {
        Directory.OnChange += StateHasChanged;
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        StateHasChanged();
        if (!Session.IsApproved || !HasInternalRole) { loading = false; return; }

        // The defect, the project labels, the directory (for the supplier's name/contact) and the
        // assignee pool are independent — they go out together.
        _ = Directory.All();
        var loads = new List<Task> { LoadDefectAsync(), LoadProjectLabelsAsync() };
        if (CanManageTodos) loads.Add(LoadAssigneeOptionsAsync());
        await Task.WhenAll(loads);
    }

    private string loadedDefectId = "";

    protected override async Task OnParametersSetAsync()
    {
        // Following a link to another defect re-keys the page (router by route value); a load
        // for a new id is the same fresh read as the first.
        if (loading || loadedDefectId == DefectId || string.IsNullOrEmpty(loadedDefectId)) return;
        loading = true;
        await LoadDefectAsync();
    }

    private async Task LoadDefectAsync()
    {
        loadedDefectId = DefectId;
        try
        {
            defect = await Queries.AskAsync(new GetDefectById(DefectId), CancellationToken.None);
            loadFailed = false;
        }
        catch { loadFailed = true; }
        finally { loading = false; }
    }

    // Labels only — a failed project read degrades the email subject, never the page.
    private async Task LoadProjectLabelsAsync()
    {
        try { if (Projects.Current is null) await Projects.RefreshAsync(CancellationToken.None); } catch { }
    }

    // A failed load leaves the pool empty, which leaves new to-dos unassigned — the panel still works.
    private async Task LoadAssigneeOptionsAsync()
    {
        try
        {
            var rolesTask = TodoStore.ListAssignableRolesAsync();
            var peopleTask = TodoStore.ListAssignablePeopleAsync();
            assigneeOptions = TodoAssigneePicker.BuildOptions(await rolesTask, await peopleTask);
        }
        catch { }
    }

    // ---- Send / chase: the communications panel hosts the composer; the page only kicks it off. ----

    private void StartSupplierEmail(bool chase)
    {
        if (defect is null) return;
        communications?.StartSupplierEmail(chase);
    }

    // After a send the server has stamped SentToSupplierAt (and Open → In progress) — re-read
    // the defect so the header, the Detail panel and the primary button follow.
    private async Task ReloadAfterSendAsync(ComposeOutcome outcome)
    {
        if (!outcome.Sent) return;
        try { defect = await Queries.AskAsync(new GetDefectById(DefectId), CancellationToken.None) ?? defect; }
        catch { /* the toast has the detail; the stale header is corrected on the next load */ }
    }

    // ---- Status ----

    private async Task SetStatusAsync(ChangeEventArgs e)
    {
        if (defect is null || !int.TryParse(e.Value?.ToString(), out var statusValue)) return;
        var status = (DefectStatus)statusValue;
        if (status == defect.Status) return;
        await RunAsync(() => Commands.SendAsync(new UpdateDefect(
            defect.DefectId, defect.Description, defect.Location, defect.AssignedToEmail, status,
            SubcontractorId: defect.SubcontractorId), CancellationToken.None));
    }

    // ---- Edit dialog ----

    private void OpenEdit()
    {
        if (defect is null) return;
        editLocation = defect.Location;
        editDescription = defect.Description;
        editAssignedTo = defect.AssignedToEmail;
        editSubcontractorId = defect.SubcontractorId ?? "";
        editError = null;
        editOpen = true;
    }

    private void CloseEdit()
    {
        editOpen = false;
        editError = null;
    }

    private async Task SaveEditAsync()
    {
        if (defect is null) return;
        if (string.IsNullOrWhiteSpace(editDescription)) { editError = "A description is required."; return; }
        editError = null;
        await RunAsync(() => Commands.SendAsync(new UpdateDefect(
            defect.DefectId, editDescription.Trim(), editLocation.Trim(), editAssignedTo.Trim(), defect.Status,
            SubcontractorId: string.IsNullOrWhiteSpace(editSubcontractorId) ? null : editSubcontractorId), CancellationToken.None));
        if (error is null) CloseEdit();
        else editError = error;
    }

    // Every defect-changing command: run it, keep its answer as the page's defect (the server
    // returns the row as saved, supplier resolved), report a failure in the red bar.
    private async Task RunAsync(Func<Task<Defect>> command)
    {
        if (busy) return;
        busy = true;
        error = null;
        try { defect = await command(); }
        catch (CommandFailedException ex) { error = ex.Message; }
        catch (Exception ex) { error = ex.Message; }
        finally { busy = false; }
    }

    public void Dispose() => Directory.OnChange -= StateHasChanged;
}

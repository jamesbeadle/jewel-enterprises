namespace Jewel.JPMS.Features.Directory;

public partial class XeroLinkModal
{
    [Parameter] public EventCallback<Subcontractor> OnLinked { get; set; }

    private bool open;
    private string subcontractorId = "";
    private string companyName = "";
    private string search = "";
    private XeroSuppliersSnapshot? snapshot;
    private bool loading;
    private string? error;
    private string? linkingContactId;

    /// <summary>Opens the dialog for one record, its search seeded with the company name so the
    /// likely contact is on screen at once.</summary>
    public void Open(string recordId, string recordCompanyName)
    {
        subcontractorId = recordId;
        companyName = recordCompanyName;
        search = recordCompanyName;
        error = null;
        open = true;
        _ = LoadSuppliersAsync(force: false);
        StateHasChanged();
    }

    private void Close() => open = false;

    private async Task LoadSuppliersAsync(bool force)
    {
        if (loading) return;
        error = null;
        try
        {
            loading = true;
            if (force) snapshot = null;
            snapshot = await SubcontractorStore.FetchXeroSuppliersAsync(force);
        }
        catch { error = "The Xero contact list couldn't be loaded. Please try again."; }
        finally { loading = false; StateHasChanged(); }
    }

    private bool IsSuggested(XeroSupplier supplier) =>
        string.Equals(supplier.MatchingSubcontractorId, subcontractorId, StringComparison.OrdinalIgnoreCase);

    // Unlinked contacts only (a linked one belongs to another record — unlink it there first),
    // suggested first, then the search narrowing. An empty search shows every unlinked contact,
    // for the record whose Xero name shares no words with its directory name.
    private IReadOnlyList<XeroSupplier> Candidates()
    {
        if (snapshot is null) return Array.Empty<XeroSupplier>();
        var q = (search ?? "").Trim();
        return snapshot.Suppliers
            .Where(supplier => !supplier.AlreadyImported)
            .Where(supplier => IsSuggested(supplier)
                || q.Length == 0
                || supplier.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || supplier.EmailAddress.Contains(q, StringComparison.OrdinalIgnoreCase)
                || supplier.Town.Contains(q, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(IsSuggested)
            .ThenBy(supplier => supplier.Name)
            .ToList();
    }

    private async Task LinkAsync(string contactId)
    {
        if (linkingContactId is not null) return;
        error = null;
        try
        {
            linkingContactId = contactId;
            var linked = await SubcontractorStore.LinkToXeroAsync(subcontractorId, contactId);
            open = false;
            await OnLinked.InvokeAsync(linked);
        }
        catch (CommandFailedException ex) { error = $"Couldn't link: {ex.Message}"; }
        catch { error = "Couldn't link that contact. Please try again."; }
        finally { linkingContactId = null; }
    }
}

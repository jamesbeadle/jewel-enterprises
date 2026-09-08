namespace Jewel.JPMS.Features.Directory;

public partial class XeroImportModal
{
    private bool open;
    private string search = "";
    // Customer-only Xero contacts (clients) are noise in a supplier import; hidden unless asked
    // for. Contacts with NEITHER flag (created in Xero but never yet billed) always show — that
    // is exactly the just-set-up supplier the import exists for.
    private bool includeCustomers;
    private XeroSuppliersSnapshot? snapshot;
    private bool loading;
    private string? error;
    private string? busyContactId;
    private BusyAction busyAction;
    // Contacts linked to an existing record from THIS dialog — their row says "Linked", not
    // "Imported", because no record was created.
    private readonly HashSet<string> linkedContactIds = new(StringComparer.OrdinalIgnoreCase);

    private enum BusyAction { Import, Link }

    /// <summary>Opens the dialog, its search seeded with the directory's own search text.</summary>
    public void Open(string directorySearch)
    {
        search = directorySearch;
        error = null;
        open = true;
        if (snapshot is null) _ = LoadSuppliersAsync(force: false);
        StateHasChanged();
    }

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
        catch { error = "The Xero supplier list couldn't be loaded. Please try again."; }
        finally { loading = false; StateHasChanged(); }
    }

    private IReadOnlyList<XeroSupplier> FilteredSuppliers()
    {
        if (snapshot is null) return Array.Empty<XeroSupplier>();
        var q = (search ?? "").Trim();
        return snapshot.Suppliers
            .Where(supplier => includeCustomers || supplier.IsSupplier || !supplier.IsCustomer)
            .Where(supplier => q.Length == 0
                || supplier.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || supplier.EmailAddress.Contains(q, StringComparison.OrdinalIgnoreCase)
                || supplier.Town.Contains(q, StringComparison.OrdinalIgnoreCase)
                || supplier.County.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private async Task ImportSupplierAsync(string contactId)
    {
        if (busyContactId is not null) return;
        error = null;
        try
        {
            busyContactId = contactId;
            busyAction = BusyAction.Import;
            await SubcontractorStore.ImportFromXeroAsync(contactId);
            MarkImported(contactId);
        }
        catch (CommandFailedException ex) { error = $"Couldn't import: {ex.Message}"; }
        catch { error = "Couldn't import that supplier. Please try again."; }
        finally { busyContactId = null; }
    }

    private async Task LinkSupplierAsync(XeroSupplier supplier)
    {
        if (busyContactId is not null || supplier.MatchingSubcontractorId is null) return;
        error = null;
        try
        {
            busyContactId = supplier.ContactId;
            busyAction = BusyAction.Link;
            await SubcontractorStore.LinkToXeroAsync(supplier.MatchingSubcontractorId, supplier.ContactId);
            linkedContactIds.Add(supplier.ContactId);
            MarkImported(supplier.ContactId);
        }
        catch (CommandFailedException ex) { error = $"Couldn't link: {ex.Message}"; }
        catch { error = "Couldn't link that contact. Please try again."; }
        finally { busyContactId = null; }
    }

    // Mark the row in place rather than re-reading Xero for a flag we already know.
    private void MarkImported(string contactId)
    {
        if (snapshot is null) return;
        snapshot = snapshot with
        {
            Suppliers = snapshot.Suppliers
                .Select(supplier => supplier.ContactId == contactId
                    ? supplier with { AlreadyImported = true }
                    : supplier)
                .ToList()
        };
    }
}

namespace Jewel.JPMS.Features.Directory;

public partial class XeroContactPushModal
{
    /// <summary>Raised with a one-line result the page can show once the push has been written.</summary>
    [Parameter] public EventCallback<string> OnPushed { get; set; }

    private bool open;
    private string subcontractorId = "";
    private string companyName = "";
    private XeroContactPushPreview? preview;
    private string? error;
    private bool pushing;

    public void Open(string recordId, string recordCompanyName)
    {
        subcontractorId = recordId;
        companyName = recordCompanyName;
        preview = null;
        error = null;
        open = true;
        _ = LoadPreviewAsync();
        StateHasChanged();
    }

    private void Close()
    {
        if (pushing) return;
        open = false;
    }

    private async Task LoadPreviewAsync()
    {
        try { preview = await SubcontractorStore.PreviewXeroContactPushAsync(subcontractorId); }
        catch (CommandFailedException ex) { error = ex.Message; }
        catch { error = "Couldn't read the contact from Xero. Please try again."; }
        finally { StateHasChanged(); }
    }

    private async Task PushAsync()
    {
        if (pushing || preview is null) return;
        error = null;
        try
        {
            pushing = true;
            var outcome = await SubcontractorStore.PushContactsToXeroAsync(subcontractorId);
            open = false;
            await OnPushed.InvokeAsync(
                $"Contacts pushed to Xero contact {outcome.XeroContactName}: primary {outcome.Written.PrimaryPerson?.Name ?? "unchanged"}, "
                + $"{outcome.Written.AdditionalPersons.Count} additional {(outcome.Written.AdditionalPersons.Count == 1 ? "person" : "people")}.");
        }
        catch (CommandFailedException ex) { error = $"Couldn't push: {ex.Message}"; }
        catch { error = "Couldn't push the contacts to Xero. Please try again."; }
        finally { pushing = false; }
    }
}

using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Features.Directory;

public partial class CisVerificationPanel
{
    [Parameter, EditorRequired] public Subcontractor Subcontractor { get; set; } = default!;
    [Parameter] public bool CanEdit { get; set; }

    private static readonly string[] StatusSuggestions =
    {
        "Verified 20% standard", "Gross payment", "Unverified 30%"
    };

    private bool recordOpen;
    private bool recordBusy;
    private string? recordError;
    private string status = "";
    private string verificationNumber = "";
    private DateTime? verifiedOn;

    private void OpenRecord()
    {
        status = Subcontractor.CisStatus;
        verificationNumber = Subcontractor.CisVerificationNumber;
        verifiedOn = Subcontractor.CisVerifiedOn?.ToDateTime(TimeOnly.MinValue);
        recordError = null;
        recordOpen = true;
    }

    private void CloseRecord()
    {
        if (recordBusy) return;
        recordOpen = false;
    }

    private async Task SaveRecord()
    {
        if (recordBusy || string.IsNullOrWhiteSpace(status)) return;
        recordError = null;
        try
        {
            recordBusy = true;
            await SubcontractorStore.RecordCisVerificationAsync(new RecordCisVerification(
                Subcontractor.SubcontractorId, status.Trim(), verificationNumber.Trim(), VerifiedOnDate));
            recordOpen = false;
        }
        catch (CommandFailedException ex) { recordError = $"Couldn't save: {ex.Message}"; }
        catch { recordError = "Couldn't save the verification. Please try again."; }
        finally { recordBusy = false; }
    }

    private DateOnly? VerifiedOnDate => verifiedOn is { } date ? DateOnly.FromDateTime(date) : null;

    private static string Dash(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;
}

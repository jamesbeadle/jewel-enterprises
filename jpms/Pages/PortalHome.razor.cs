using Jewel.JPMS.Contracts.Portal;

namespace Jewel.JPMS.Pages;

public partial class PortalHome
{

    private SubcontractorPortalRecord? myRecord;

    private bool CanAccess => Session.AvailableRoles.Contains(Role.Subcontractor);

    // Resolved server-side by /auth/me. Null for internal users (including admins, who carry
    // every role): they must never trigger the portal fetches, which would 403 after a wait.
    private bool HasLinkedRecord => !string.IsNullOrEmpty(Auth.CurrentSubcontractorId);

    private IReadOnlyList<ComplianceDocument> CurrentDocuments =>
        myRecord is null
            ? Array.Empty<ComplianceDocument>()
            : myRecord.ComplianceDocuments.Where(document => document.IsCurrentVersion)
                .OrderBy(document => document.Kind, StringComparer.OrdinalIgnoreCase).ToList();

    private IReadOnlyList<ComplianceDocument> SupersededDocuments =>
        myRecord is null
            ? Array.Empty<ComplianceDocument>()
            : myRecord.ComplianceDocuments.Where(document => !document.IsCurrentVersion)
                .OrderBy(document => document.Kind, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(document => document.Version).ToList();

    // ---- Work orders ----

    private static readonly System.Globalization.CultureInfo GbCulture =
        System.Globalization.CultureInfo.GetCultureInfo("en-GB");

    private IReadOnlyList<PortalWorkOrder> WorkOrders =>
        (CanAccess ? PortalStore.MyWorkOrders() : null) ?? Array.Empty<PortalWorkOrder>();

    // An issued order the supplier hasn't electronically accepted yet — the list nudges them in.
    private static bool NeedsAcceptance(PortalWorkOrder workOrder) =>
        workOrder.Order.Status == WorkOrderStatus.Released && !workOrder.Order.IsAccepted;

    private static string StatusLabel(WorkOrderStatus status) => status switch
    {
        WorkOrderStatus.Released  => "Issued",
        WorkOrderStatus.Complete  => "Complete",
        WorkOrderStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };


    // ---- Variation requests ----

    private IReadOnlyList<SubcontractorVariationRequest> VariationRequests =>
        (CanAccess ? PortalStore.MyVariationRequests() : null) ?? Array.Empty<SubcontractorVariationRequest>();

    // Variations can only be raised against orders that are live (issued or complete-but-varied).
    private IReadOnlyList<PortalWorkOrder> OpenWorkOrders =>
        WorkOrders.Where(workOrder => workOrder.Order.Status is WorkOrderStatus.Released or WorkOrderStatus.Complete).ToList();

    private bool variationBusy;
    private string? variationError;
    private string? variationNote;
    private string variationWorkOrderId = "";
    private string variationTitle = "";
    private string variationDescription = "";
    private decimal variationValue;

    private async Task RaiseVariation()
    {
        if (variationBusy) return;
        variationError = null;
        variationNote = null;
        try
        {
            variationBusy = true;
            var raised = await PortalStore.RaiseVariationRequestAsync(
                variationWorkOrderId, variationTitle.Trim(), variationDescription.Trim(), variationValue);
            variationNote = $"\"{raised.Title}\" sent to JBB for review.";
            variationWorkOrderId = "";
            variationTitle = "";
            variationDescription = "";
            variationValue = 0;
        }
        catch (CommandFailedException ex) { variationError = ex.Message; }
        catch { variationError = "Couldn't send the request. Please try again."; }
        finally { variationBusy = false; }
    }

    private async Task WithdrawVariationRequest(string variationRequestId)
    {
        if (variationBusy) return;
        variationError = null;
        try
        {
            variationBusy = true;
            await PortalStore.WithdrawVariationRequestAsync(variationRequestId);
        }
        catch (CommandFailedException ex) { variationError = ex.Message; }
        catch { variationError = "Couldn't withdraw the request. Please try again."; }
        finally { variationBusy = false; }
    }

    private static string RequestStatusLabel(VariationRequestStatus status) => status switch
    {
        VariationRequestStatus.Submitted   => "Awaiting review",
        VariationRequestStatus.UnderReview => "Under review",
        VariationRequestStatus.Accepted    => "Accepted",
        VariationRequestStatus.Rejected    => "Rejected",
        VariationRequestStatus.Withdrawn   => "Withdrawn",
        _ => status.ToString()
    };


    // Only live versions drive the attention banner; superseded ones are history.
    private IReadOnlyList<ComplianceDocument> ExpiringOrExpired =>
        CurrentDocuments
            .Where(document => document.Status() is ComplianceStatus.ExpiringSoon or ComplianceStatus.Expired)
            .ToList();

    // ---- Upload ----

    private static readonly string[] SuggestedKinds =
    {
        "Public liability insurance", "Employers liability insurance", "CIS certificate",
        "RAMS", "Method statement", "Training certificate", "Waste carrier licence"
    };

    private bool uploadBusy;
    private string? uploadError;
    private string? uploadNote;
    private string uploadKind = "";
    private DateTime? uploadExpiry;
    private decimal? uploadPublicLiabilityCover;
    private IBrowserFile? uploadFile;

    private void OnUploadFileSelected(InputFileChangeEventArgs e)
    {
        uploadFile = e.File;
        uploadError = null;
        uploadNote = null;
    }

    private async Task HandleUpload()
    {
        if (uploadBusy || uploadFile is null || string.IsNullOrWhiteSpace(uploadKind)) return;
        uploadError = null;
        uploadNote = null;
        try
        {
            uploadBusy = true;
            DateTimeOffset? expiresAt = uploadExpiry is null
                ? null
                : new DateTimeOffset(DateTime.SpecifyKind(uploadExpiry.Value.Date, DateTimeKind.Utc));
            await PortalStore.UploadDocumentAsync(uploadKind.Trim(), expiresAt, uploadFile, CancellationToken.None,
                uploadPublicLiabilityCover);
            uploadNote = $"{uploadKind.Trim()} uploaded.";
            uploadKind = "";
            uploadExpiry = null;
            uploadPublicLiabilityCover = null;
            uploadFile = null;
        }
        catch (Exception ex)
        {
            uploadError = $"Upload failed: {ex.Message}";
        }
        finally
        {
            uploadBusy = false;
        }
    }


    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        PortalStore.OnChange += HandleChange;
        // Revalidate in the background on every visit (stale-while-revalidate, per CLAUDE.md).
        // Only for linked portal logins — unlinked sessions get an immediate explanation instead.
        if (CanAccess && HasLinkedRecord) _ = PortalStore.Refresh();
        Reload();
    }

    // Don't touch the store for unlinked sessions — avoids a pointless 403 fetch.
    private void Reload() => myRecord = CanAccess && HasLinkedRecord ? PortalStore.MyRecord() : null;
    private void HandleChange() { Reload(); StateHasChanged(); }

    public void Dispose() => PortalStore.OnChange -= HandleChange;
}

using Jewel.JPMS.Contracts.Portal;

namespace Jewel.JPMS.Services;

/// <summary>The subcontractor portal's view of the signed-in user's own company record.</summary>
public interface IPortalStore
{
    /// <summary>False until the record has been fetched at least once, so views can distinguish
    /// "still loading" from "no record linked to this login".</summary>
    bool IsLoaded { get; }

    /// <summary>Render-time read; triggers a background fetch on first call (never blocks).</summary>
    SubcontractorPortalRecord? MyRecord();

    /// <summary>Issued work orders (Released and later), newest first. Render-time read with the
    /// same fetch-once semantics as MyRecord(). Null until fetched at least once.</summary>
    IReadOnlyList<PortalWorkOrder>? MyWorkOrders();

    /// <summary>Background revalidation. Call once from OnInitializedAsync on every portal page
    /// (stale-while-revalidate — see the front-end data-loading convention in CLAUDE.md).</summary>
    Task Refresh();

    /// <summary>Uploads a compliance document to the caller's own record. Re-uploading a kind
    /// creates a new version (the old one is kept as history). Throws with a user-showable
    /// message on failure; refreshes the record on success.</summary>
    Task UploadDocumentAsync(string kind, DateTimeOffset? expiresAt, IBrowserFile file, CancellationToken cancellationToken,
        decimal? publicLiabilityCover = null);

    /// <summary>The caller's variation requests, newest first. Render-time read with the same
    /// fetch-once semantics as MyRecord(). Null until fetched at least once.</summary>
    IReadOnlyList<SubcontractorVariationRequest>? MyVariationRequests();

    /// <summary>Raises a priced variation request against one of the caller's own work orders.
    /// Throws CommandFailedException with a user-showable message on rejection.</summary>
    Task<SubcontractorVariationRequest> RaiseVariationRequestAsync(string workOrderId, string title, string description, decimal proposedValue);

    /// <summary>Withdraws one of the caller's own open variation requests.</summary>
    Task WithdrawVariationRequestAsync(string variationRequestId);

    /// <summary>One-click electronic acceptance of one of the caller's own issued work orders.
    /// The acceptance is stamped server-side with the signed-in contact's name and email.
    /// Throws CommandFailedException with a user-showable message on rejection.</summary>
    Task<WorkOrder> AcceptWorkOrderAsync(string workOrderId);

    event Action? OnChange;
}

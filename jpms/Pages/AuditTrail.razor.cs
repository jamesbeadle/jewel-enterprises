using Jewel.JPMS.Contracts.Audit;

namespace Jewel.JPMS.Pages;

public partial class AuditTrail
{
    private const int PageSize = 50;

    // Session checked and the user signed in. This is NOT "the register is here" — that is
    // `loaded`, below; keeping the two apart is what lets the chrome and the filters show at once
    // while the first page is still in flight.
    private bool loaded;
    // The project labels have answered (or failed — a failure leaves the picker with the one
    // "All projects" option, which is honest once it is known to be all there is).
    private bool projectOptionsReady;
    private bool loadingMore;
    private string? loadError;

    private readonly List<AuditEvent> items = new();
    private string? nextCursor;
    private int total;

    private string? pathwayFilter;
    private string eventTypeRaw = "";
    private string projectFilter = "";

    private static readonly string[] PathwayLabels = { "Client", "Subcontractor", "Internal" };

    // The written vocabulary only — the reserved wider-scope values are declared in the enum but
    // never recorded, so offering them would only produce empty results. CostCentreRecoded is
    // written (the finance reconciliation trail, 2026-07-28); each project's Reconciliation Audit
    // page reads the same events narrowed to its project.
    private static readonly (AuditEventType Type, string Label)[] EventTypeOptions =
    {
        (AuditEventType.EmailTriaged,           "Email routed"),
        (AuditEventType.RecordLinked,           "Record linked"),
        (AuditEventType.RecordCreatedFromEmail, "Record created from email"),
        (AuditEventType.TagRemoved,             "Tag removed"),
        (AuditEventType.Discarded,              "Discarded"),
        (AuditEventType.Restored,               "Restored"),
        (AuditEventType.WallRejected,           "Wall refused"),
        (AuditEventType.DraftCreated,           "Draft created"),
        (AuditEventType.SnapshotTaken,          "Snapshot taken"),
        (AuditEventType.BackfillStamped,        "Backfill stamped"),
        (AuditEventType.CostCentreRecoded,      "Cost centre recoded"),
        (AuditEventType.ProjectDeleted,         "Project deleted"),
        (AuditEventType.SentToDocumentControl,  "Sent to Document Triage"),
        (AuditEventType.DocumentFiled,          "Document filed"),
        (AuditEventType.DocumentDiscarded,      "Document discarded"),
        (AuditEventType.WorkOrderSaleWarningOverridden, "WO sale warning overridden"),
        (AuditEventType.LabourBudgetOverridden,  "Labour budget overridden"),
        (AuditEventType.MailboxDraftDeleted,     "Draft deleted"),
        (AuditEventType.CostCodeBudgetSet,       "Cost code budget set"),
        (AuditEventType.WorkerLinkedToDirectory,  "Worker linked to directory"),
        (AuditEventType.LabourChaseDayDismissed,  "Chase day dismissed"),
        (AuditEventType.DrawingDataExtracted,    "Document data extracted"),
        (AuditEventType.DocumentArchiveExtracted, "Archive extracted"),
        (AuditEventType.BluebeamConnected,       "Bluebeam connected"),
        (AuditEventType.KpiEmailMarked,          "KPI marked"),
        (AuditEventType.KpiEmailRemoved,         "KPI removed"),
        (AuditEventType.LabourApprovalReversed,  "Labour approval reversed"),
        (AuditEventType.LabourDayMoved,          "Labour day moved"),
        (AuditEventType.DirectoryRecordXeroLinkChanged, "Directory Xero link changed")
    };

    // Mirrors the API's TriageRoles.AllowedToTriage — the audit trail is a triage-side tool.
    private bool CanAccess => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.FinanceDirector or Role.ProjectManager);

    private AuditEventType? EventTypeFilter =>
        int.TryParse(eventTypeRaw, out var parsed) ? (AuditEventType)parsed : null;

    private bool HasFilter =>
        pathwayFilter is not null || EventTypeFilter is not null || !string.IsNullOrEmpty(projectFilter);

    // Every project, Completed included (the audit trail is history), in the canonical work order
    // — live sites first, Completed last (ProjectOrdering.InWorkOrder).
    private IReadOnlyList<Project> ProjectOptions =>
        (Projects.Current ?? (IReadOnlyList<Project>)Array.Empty<Project>())
            .InWorkOrder()
            .ToList();

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        // Paint the chrome before the fetches: Blazor re-renders OnInitializedAsync only at its
        // FIRST await, which has already passed, so without this the page waits on the first page.
        StateHasChanged();
        if (!Session.IsApproved || !CanAccess) { loaded = true; projectOptionsReady = true; return; }

        // The audit query is the page; the project list only labels a filter dropdown. Neither
        // needs the other, so they run together — the register no longer waits on the labels.
        await Task.WhenAll(LoadProjectLabelsAsync(), LoadAsync(reset: true));
    }

    // Project labels for the filter dropdown; failing only degrades the filter, never the list.
    // Flips projectOptionsReady either way — an unlabelled filter beats one stuck on
    // "Loading projects…" — and repaints, since the audit query may still be in flight.
    private async Task LoadProjectLabelsAsync()
    {
        try { if (Projects.Current is null) await Projects.RefreshAsync(CancellationToken.None); } catch { }
        projectOptionsReady = true;
        StateHasChanged();
    }

    private async Task OnPathwayChanged(string? pathway)
    {
        if (pathwayFilter == pathway) return;
        pathwayFilter = pathway;
        await LoadAsync(reset: true);
    }

    private async Task OnEventTypeChanged(ChangeEventArgs e)
    {
        eventTypeRaw = e.Value?.ToString() ?? "";
        await LoadAsync(reset: true);
    }

    private async Task OnProjectChanged(ChangeEventArgs e)
    {
        projectFilter = e.Value?.ToString() ?? "";
        await LoadAsync(reset: true);
    }

    private Task LoadMoreAsync() => LoadAsync(reset: false);

    private async Task LoadAsync(bool reset)
    {
        if (reset)
        {
            loaded = false;
            items.Clear();
            nextCursor = null;
            total = 0;
        }
        loadError = null;
        loadingMore = !reset;
        try
        {
            var page = await Queries.AskAsync(
                new ListAuditEvents(
                    string.IsNullOrEmpty(projectFilter) ? null : projectFilter,
                    pathwayFilter,
                    EventTypeFilter,
                    null,
                    nextCursor,
                    PageSize),
                CancellationToken.None);
            items.AddRange(page.Items);
            nextCursor = page.NextCursor;
            total = page.Total;
        }
        catch
        {
            loadError = "Couldn't load the audit trail. Please try again.";
        }
        finally
        {
            loaded = true;
            loadingMore = false;
        }
    }

    private string PathwayTabClass(string? pathway) =>
        pathwayFilter == pathway
            ? "chip chip-active"
            : "chip";


    private static string EventLabel(AuditEventType type) => type switch
    {
        AuditEventType.EmailTriaged           => "Email routed",
        AuditEventType.RecordLinked           => "Record linked",
        AuditEventType.RecordCreatedFromEmail => "Record created from email",
        AuditEventType.TagRemoved             => "Tag removed",
        AuditEventType.Discarded              => "Discarded",
        AuditEventType.Restored               => "Restored",
        AuditEventType.WallRejected           => "Wall refused",
        AuditEventType.DraftCreated           => "Draft created",
        AuditEventType.SnapshotTaken          => "Snapshot taken",
        AuditEventType.BackfillStamped        => "Backfill stamped",
        AuditEventType.CrossPathwayOverride   => "Cross-pathway override",
        AuditEventType.ThreadSwept            => "Thread swept",
        AuditEventType.CostCentreRecoded      => "Cost centre recoded",
        AuditEventType.ProjectDeleted         => "Project deleted",
        AuditEventType.SentToDocumentControl  => "Sent to Document Triage",
        AuditEventType.DocumentFiled          => "Document filed",
        AuditEventType.DocumentDiscarded      => "Document discarded",
        AuditEventType.WorkOrderSaleWarningOverridden => "WO sale warning overridden",
        AuditEventType.LabourBudgetOverridden => "Labour budget overridden",
        AuditEventType.MailboxDraftDeleted    => "Draft deleted",
        AuditEventType.CostCodeBudgetSet      => "Cost code budget set",
        AuditEventType.WorkerLinkedToDirectory => "Worker linked to directory",
        AuditEventType.LabourChaseDayDismissed => "Chase day dismissed",
        AuditEventType.DrawingDataExtracted   => "Document data extracted",
        AuditEventType.DocumentArchiveExtracted => "Archive extracted",
        AuditEventType.BluebeamConnected      => "Bluebeam connected",
        AuditEventType.KpiEmailMarked         => "KPI marked",
        AuditEventType.KpiEmailRemoved        => "KPI removed",
        AuditEventType.LabourApprovalReversed => "Labour approval reversed",
        AuditEventType.LabourDayMoved         => "Labour day moved",
        AuditEventType.DirectoryRecordXeroLinkChanged => "Directory Xero link changed",
        _                                     => type.ToString()
    };

    // Requests have a detail page; every other record type reads as plain reference text.
    private static string? RecordHref(AuditEvent item) =>
        item.RecordType == RecordType.Request
        && !string.IsNullOrEmpty(item.ProjectId)
        && !string.IsNullOrEmpty(item.RecordId)
            ? $"/projects/{item.ProjectId}/requests/view/{item.RecordId}"
            : null;

    // Compact relative stamp for scanning; the exact moment sits in the cell's hover title.
    private static string Ago(DateTimeOffset at)
    {
        var span = DateTimeOffset.UtcNow - at;
        if (span < TimeSpan.FromMinutes(1)) return "just now";
        if (span < TimeSpan.FromHours(1)) return $"{(int)span.TotalMinutes}m ago";
        if (span < TimeSpan.FromHours(24)) return $"{(int)span.TotalHours}h ago";
        if (span < TimeSpan.FromDays(30)) return $"{(int)span.TotalDays}d ago";
        return DateText(at);
    }

    private static string Dash(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;
}

using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.RecordLinks;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;

namespace Jewel.JPMS.Pages;

public partial class ProjectVariationDetail
{
    // ---- Communications: emails tagged to the record's quoting reference and (once approved) its V-ref ----
    private bool emailsLoaded;
    private string? emailsError;
    private IReadOnlyList<MailboxMessage> emails = Array.Empty<MailboxMessage>();
    // The record only carries commercial figures (V-ref, value, cost code) once approved — this is
    // the "VO" the old two-record model kept separately; here it's just the current record, or null.
    private VariationOrder? ApprovedOrder => order is { Status: VariationOrderStatus.Approved } ? order : null;

    // The approve build-up is captured in a modal (the sidebar is too narrow for the line table).
    private bool approveModalOpen;

    // Why a line edit was refused. Held apart from the page's `error` because it has to render
    // inside the open dialog — the page's banner sits behind the overlay, where a reader who is
    // mid-edit never sees it, and the dialog staying put with no explanation reads as a hang.
    private string? editLinesError;

    private static readonly System.Globalization.CultureInfo Gb = System.Globalization.CultureInfo.GetCultureInfo("en-GB");
    private static string MoneyPennies(decimal value) => value.ToString("C2", Gb);

    // The approved variation's lines on the valuation report — its priced build-up, newest cost
    // centre split included. Empty until approved (no V-ref) or until the store's lines land.
    private IReadOnlyList<ValuationLineItem> VariationLines =>
        order?.VariationRef is { Length: > 0 } vref
            ? Valuation.LinesFor(ProjectId)
                .Where(line => line.ElementType == ValuationElementType.Variation && line.VariationRef == vref)
                .OrderBy(line => line.DisplayOrder)
                .ToList()
            : Array.Empty<ValuationLineItem>();

    // Distinct cost centres the approved variation touches — from its report lines, falling back to
    // the order's primary code before the lines have loaded.
    private IReadOnlyList<string> ApprovedCostCentres =>
        VariationLines.Count > 0
            ? VariationLines.Select(line => line.CostCode).Where(code => !string.IsNullOrWhiteSpace(code)).Distinct().ToList()
            : (string.IsNullOrWhiteSpace(order?.CostCode) ? Array.Empty<string>() : new[] { order!.CostCode! });

    private string RequestHref => order is null ? $"/projects/{ProjectId}/requests" : $"/projects/{ProjectId}/requests/view/{order.RequestId}";

    private bool CanManage => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.ProjectManager or Role.QuantitySurveyor);


    // The approved variation's build-up lives on the valuation report; until its lines land,
    // ApprovedCostCentres falls back to the order's single primary code.
    private bool ValuationLinesReady => Valuation.ReportLoadedFor(ProjectId);

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        Subcontractors.OnChange += StateHasChanged;
        _ = Subcontractors.All(); // warm the directory cache for the tender dropdown
        // Revalidate the request register in the background (stale-while-revalidate) — it feeds
        // the originating-request picker for unlinked (seeded) variation orders.
        RequestRegister.Refresh(ProjectId);
        RequestRegister.OnChange += StateHasChanged;
        // The approved variation's line breakdown reads from the valuation report store — warm it
        // and re-render when the lines land (stale-while-revalidate).
        Valuation.OnChange += StateHasChanged;
        Valuation.Refresh(ProjectId);
        // Activity dots on the tab bar land in the background — absent until then (never gated).
        Activity.OnChanged += StateHasChanged;
        Activity.Refresh(ProjectId);
        // The project list feeds the reply composer's attachment picker (drawings/photos by
        // project) — revalidated in the background like every other read model here.
        ProjectList.OnChanged += StateHasChanged;
        _ = LoadProjectListAsync();
        sessionReady = true;
        await ReloadAsync();
    }

    // Covers the case where the component is REUSED for a different variation while in view:
    // the record must be reloaded before anything renders against it.
    protected override async Task OnParametersSetAsync()
    {
        if (!sessionReady) return;
        if (order is not null && order.VariationOrderId != VariationOrderId)
        {
            orderLoaded = false;
            CloseOpenDialogs();
            await ReloadAsync();
        }
    }

    // Drops every dialog, editor and inline confirm the bar can open. Used when the page is about
    // to show a different record, so nothing opened against the old one hangs over the new.
    private void CloseOpenDialogs()
    {
        editLinesModalOpen = false;
        approveModalOpen = false;
        renamingOrder = false;
        editingSections = false;
        editingEstimate = false;
        revisingValue = false;
        buildUpDialogOpen = false;
        recordingTender = false;
        linkingRequest = false;
        deletingOrder = false;
        decliningOrder = false;
        rejectingOrder = false;
        returningToQuoting = false;
    }

    // Scrolls to and flashes the approved-figures panel. The lineage bar no longer needs this —
    // there is one variation chip now, rather than a quote chip pointing at an order chip on the
    // same page — but the status pill still uses it when routing Reject / Return-to-quoting from
    // Approved, so the confirmation the user has to give lands visibly instead of the click
    // appearing to do nothing.
    private async Task FocusVariationOrderPanel()
    {
        try { await Js.InvokeVoidAsync("jpmsFocusElement", "variation-order"); }
        catch { /* purely cosmetic — never let a scroll failure break the page */ }
    }

    public void Dispose()
    {
        Subcontractors.OnChange -= StateHasChanged;
        RequestRegister.OnChange -= StateHasChanged;
        Valuation.OnChange -= StateHasChanged;
        Activity.OnChanged -= StateHasChanged;
        ProjectList.OnChanged -= StateHasChanged;
    }

    // Losing the project list should cost the composer its drawing/photo sources, not the page.
    private async Task LoadProjectListAsync()
    {
        try { await ProjectList.RefreshAsync(CancellationToken.None); }
        catch { /* reported by the query client; the picker's project sources render empty */ }
    }

    // The tab bar's per-record activity dots (the containing request's linked mail, this
    // variation's, each bid package's) — one lookup shared by every tab.
    private RecordActivitySummary? TabActivity(RecordType type, string recordId) =>
        Activity.For(ProjectId, type, recordId);

    // Best-effort: the instruction register is context on this page, never the point of it, so a
    // failure (or a role without access to it) leaves the banner off rather than the page broken.
    private async Task LoadLinkedInstructionsAsync()
    {
        try
        {
            var all = await Instructions.ListAsync(ProjectId);
            linkedInstructions = all
                .Where(instruction => instruction.Links.Any(link => link.VariationOrderId == VariationOrderId))
                .ToList();
        }
        catch
        {
            linkedInstructions = new List<ArchitectInstruction>();
        }
        finally { instructionsLoaded = true; }
    }

    private async Task ReloadAsync()
    {
        try
        {
            order = await Variations.GetByIdAsync(VariationOrderId);
            // Originating request for the lineage bar — best-effort, the bar shows a placeholder
            // without it. Portal-accepted variation orders have no request at all (RequestId is empty).
            if (order is not null && !string.IsNullOrWhiteSpace(order.RequestId))
            {
                try { request = await RequestRegister.GetAsync(order.RequestId); }
                catch { request = null; }
            }
            // The originating-request picker must not offer requests already carrying a variation
            // order (a request has at most one), so it needs the project's other orders to exclude.
            if (order is not null && string.IsNullOrWhiteSpace(order.RequestId))
                projectQuotes = await Variations.ListForProjectAsync(ProjectId);
            error = null;
        }
        catch { error = "Couldn't load the variation order. Please try again."; }
        finally { orderLoaded = true; }

        await LoadLinkedInstructionsAsync();
        await LoadEmailsAsync();
    }

    // The mail behind this record: everything tagged to its quoting reference plus, once approved,
    // everything tagged to its V-ref — one merged, newest-first list (an email tagged to both, e.g.
    // a whole thread synced across the approval, appears once).
    private async Task LoadEmailsAsync()
    {
        emailsError = null;
        try
        {
            var tagged = new List<MailboxMessage>(
                await Queries.AskAsync(new ListRecordEmails(RecordType.VariationQuote, VariationOrderId), CancellationToken.None));
            if (ApprovedOrder is { } approved)
                tagged.AddRange(await Queries.AskAsync(new ListRecordEmails(RecordType.Variation, approved.VariationOrderId), CancellationToken.None));
            emails = tagged
                .GroupBy(email => string.IsNullOrEmpty(email.InternetMessageId) ? email.Id : email.InternetMessageId)
                .Select(group => group.First())
                .OrderByDescending(email => email.ReceivedAt)
                .ToList();
        }
        catch
        {
            emails = Array.Empty<MailboxMessage>();
            emailsError = "Couldn't load this variation's emails. Please try again.";
        }
        finally { emailsLoaded = true; }
    }

}

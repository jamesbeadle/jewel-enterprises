

namespace Jewel.JPMS.Pages;

public partial class DocumentControl
{
    // ---- Filing form state — reset whenever the selection changes. ----
    private FileDestination destination = FileDestination.Drawing;
    private string filingProjectId = "";
    private bool drawingIsRevision;
    private string drawingId = "";
    private string drawingCode = "";
    private string drawingTitle = "";
    private DrawingFolderPicker? drawingFolderPicker;
    private string drawingRevision = "";
    private string certificateNumber = "";
    private string certificateAmountText = "";
    private DateTime? certificateIssuedDate;
    private string certificateClaimId = "";
    private string subcontractorId = "";
    private string subcontractorKind = "";
    private DateTime? subcontractorExpiry;


    // ---- Project filter: narrows every view (Queue / Filed / Discarded) and the header count
    //      to one project. Client-side — the store already lists every item — and matched on
    //      the item's ProjectIdHint, which is the project set when the attachment was sent to
    //      triage from the Control Centre. "" is "All projects". ----
    private string projectFilter = "";

    private IReadOnlyList<DocumentControlItem> AllItems => items ?? Array.Empty<DocumentControlItem>();
    private IReadOnlyList<DocumentControlItem> FilteredItems =>
        string.IsNullOrWhiteSpace(projectFilter)
            ? AllItems
            : AllItems.Where(item => string.Equals(item.ProjectIdHint, projectFilter, StringComparison.Ordinal)).ToList();
    private IReadOnlyList<DocumentControlItem> PendingItems =>
        FilteredItems.Where(item => item.Status == DocumentControlStatus.Pending).ToList();
    private IReadOnlyList<DocumentControlItem> VisibleItems =>
        FilteredItems.Where(item => item.Status == StatusFor(view)).ToList();

    private bool IsFilteredByProject => !string.IsNullOrWhiteSpace(projectFilter);
    private string? FilterProjectName => ProjectNameFor(projectFilter);

    private IReadOnlyList<SearchSelect.Option> ProjectFilterOptions =>
        (ProjectList.Current ?? Array.Empty<Project>()).InWorkOrder()
            .Select(project => new SearchSelect.Option(project.ProjectId, $"{project.Reference} — {project.Name}"))
            .ToList();

    private DocumentControlItem? Selected =>
        selectedId is null ? null : AllItems.FirstOrDefault(item => item.DocumentControlItemId == selectedId);

    private static DocumentControlStatus StatusFor(DocView view) => view switch
    {
        DocView.Filed => DocumentControlStatus.Filed,
        DocView.Discarded => DocumentControlStatus.Discarded,
        _ => DocumentControlStatus.Pending
    };

    private string EmptyText => (view, IsFilteredByProject) switch
    {
        (DocView.Filed, true) => "Nothing has been filed for this project.",
        (DocView.Filed, false) => "Nothing has been filed yet.",
        (DocView.Discarded, true) => "Nothing has been discarded for this project.",
        (DocView.Discarded, false) => "Nothing has been discarded.",
        (_, true) => "No documents waiting for this project.",
        _ => "No documents waiting."
    };

    private IReadOnlyList<Project> ProjectOptions =>
        (ProjectList.Current ?? Array.Empty<Project>()).InWorkOrder().ToList();

    private IReadOnlyList<Drawing> DrawingOptions =>
        string.IsNullOrWhiteSpace(filingProjectId) ? Array.Empty<Drawing>() : DrawingStore.DrawingsFor(filingProjectId);

    private IReadOnlyList<ValuationClaim> ClaimOptions =>
        string.IsNullOrWhiteSpace(filingProjectId)
            ? Array.Empty<ValuationClaim>()
            : ValuationReport.ClaimsFor(filingProjectId).OrderByDescending(claim => claim.ClaimNumber).ToList();

    private IReadOnlyList<SearchSelect.Option> SubcontractorOptions =>
        (Subcontractors.Current ?? Array.Empty<Subcontractor>())
            .OrderBy(sub => sub.CompanyName, StringComparer.OrdinalIgnoreCase)
            .Select(sub => new SearchSelect.Option(sub.SubcontractorId, sub.CompanyName))
            .ToList();

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        StateHasChanged();

        DrawingStore.OnChange += StateHasChanged;
        ValuationReport.OnChange += StateHasChanged;
        ProjectList.OnChanged += StateHasChanged;
        Subcontractors.OnChanged += StateHasChanged;

        // The pickers' sources load in the background — losing one costs a picker its options,
        // not the page; each control says "Loading…" in its own language, never a gate.
        _ = LoadPickerSourcesAsync();
        await LoadItemsAsync();
    }

    private async Task LoadPickerSourcesAsync()
    {
        try { await Task.WhenAll(ProjectList.RefreshAsync(CancellationToken.None), Subcontractors.RefreshAsync(CancellationToken.None)); }
        catch { /* reported by the query client; the pickers render empty */ }
    }

    private async Task LoadItemsAsync()
    {
        try
        {
            loadError = null;
            items = await Store.ListAsync();
        }
        catch
        {
            // The error toast carries the reference and the detail; this opens the gate honestly.
            loadError = "Couldn't load Document Triage. Refresh to try again.";
        }
        StateHasChanged();
    }

    private void SwitchView(DocView next)
    {
        if (view == next) return;
        view = next;
        doneNote = null;
        // The open document stays open only if it lives in the new view.
        if (Selected is { } open && open.Status != StatusFor(next)) selectedId = null;
    }

    private void OnProjectFilterChanged(string value)
    {
        value ??= "";
        if (projectFilter == value) return;
        projectFilter = value;
        doneNote = null;
        // Same rule as switching view: the open document stays open only if it is still listed.
        if (Selected is { } open && IsFilteredByProject
            && !string.Equals(open.ProjectIdHint, projectFilter, StringComparison.Ordinal))
            selectedId = null;
    }

    private void Select(string itemId)
    {
        if (selectedId == itemId) return;
        selectedId = itemId;
        freshIds.Remove(itemId);
        actionError = null;
        doneNote = null;
        sourceEmailOpen = false;
        sourceEmailLoading = false;
        sourceEmail = null;
        ResetFilingForm();
    }

    // A completed action takes the document off the screen: the row has already left this view,
    // so keeping its detail open beside a list that no longer highlights it read as "nothing
    // happened". The outcome shows in the emptied right pane until the next pick.
    private void CloseAfterAction(string note)
    {
        selectedId = null;
        actionError = null;
        doneNote = note;
        sourceEmailOpen = false;
        sourceEmailLoading = false;
        sourceEmail = null;
    }

    private void ResetFilingForm()
    {
        var item = Selected;
        destination = FileDestination.Drawing;
        drawingIsRevision = false;
        drawingId = "";
        drawingFolderPicker?.Reset();
        certificateNumber = "";
        certificateAmountText = "";
        certificateIssuedDate = null;
        certificateClaimId = "";
        subcontractorId = "";
        subcontractorKind = "";
        subcontractorExpiry = null;
        // The email's triage project is the hint; the drawing fields prefill from the file name
        // ("PRO-064-(WD)-P-800 Rev I Site set out.pdf" → code / revision / title).
        filingProjectId = item?.ProjectIdHint ?? "";
        PrefillDrawingFieldsFromFileName(item?.FileName ?? "");
        if (!string.IsNullOrWhiteSpace(filingProjectId)) RefreshProjectScopedSources();
    }

    private void RefreshProjectScopedSources()
    {
        DrawingStore.Refresh(filingProjectId);
        ValuationReport.Refresh(filingProjectId);
    }

    private void OnFilingProjectChanged(ChangeEventArgs e)
    {
        filingProjectId = e.Value?.ToString() ?? "";
        drawingId = "";
        certificateClaimId = "";
        if (!string.IsNullOrWhiteSpace(filingProjectId)) RefreshProjectScopedSources();
    }

    private void SetDestination(FileDestination next)
    {
        destination = next;
        actionError = null;
    }

    private void SetDrawingMode(bool isRevision)
    {
        drawingIsRevision = isRevision;
        actionError = null;
        if (isRevision && !string.IsNullOrWhiteSpace(filingProjectId)) DrawingStore.Refresh(filingProjectId);
    }

    // "PRO-064-(WD)-P-800 Rev I Site set out.pdf" → code "PRO-064-(WD)-P-800", revision "I",
    // title "Site set out". With no "Rev X" the fields stay blank — the register then names the
    // drawing by its file, which beats a file name masquerading as a code. Moved from the Control
    // Centre's retired save-to-drawings form (2026-08-12).
    private void PrefillDrawingFieldsFromFileName(string fileName)
    {
        var name = System.IO.Path.GetFileNameWithoutExtension(fileName ?? "").Trim();
        var match = System.Text.RegularExpressions.Regex.Match(
            name, @"^(?<code>.+?)[\s\-–—]+Rev(?:ision)?\.?\s*(?<rev>[A-Za-z0-9]{1,3})\b[\s\-–—]*(?<title>.*)$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success)
        {
            drawingCode = match.Groups["code"].Value.Trim();
            drawingRevision = match.Groups["rev"].Value.Trim().ToUpperInvariant();
            drawingTitle = match.Groups["title"].Value.Trim();
        }
        else
        {
            drawingCode = "";
            drawingRevision = "";
            drawingTitle = "";
        }
    }

    private async Task ToggleSourceEmail()
    {
        sourceEmailOpen = !sourceEmailOpen;
        if (!sourceEmailOpen || sourceEmail is not null || sourceEmailLoading) return;
        var item = Selected;
        if (item is null) return;
        try
        {
            sourceEmailLoading = true;
            sourceEmail = await Intake.GetMessageDetailAsync(item.MessageId, item.InternetMessageId);
        }
        catch
        {
            // Null renders the snapshot-only fallback — the context never goes blank.
            sourceEmail = null;
        }
        finally { sourceEmailLoading = false; }
    }

    // ---- Filing actions: each runs its command, swaps the returned item into the list (so the
    //      row moves view without a refetch) and says where the document went. ----

    private async Task DoFileAsDrawing()
    {
        var item = Selected;
        if (item is null || busy) return;
        var code = drawingCode.Trim();
        var title = drawingTitle.Trim();
        string? targetDrawingId = null;
        if (drawingIsRevision)
        {
            // The revision joins the picked drawing by id — its code may be blank.
            var target = DrawingOptions.FirstOrDefault(d => d.DrawingId == drawingId);
            if (target is null) { actionError = "Select the document this revision belongs to."; return; }
            targetDrawingId = target.DrawingId;
            code = target.DrawingCode;
            title = target.Title;
        }
        if (!drawingIsRevision && drawingFolderPicker?.Problem is { } folderProblem)
        {
            actionError = folderProblem;
            return;
        }
        await RunFiling("Filing to project documents", async () =>
        {
            // The folder resolves first (creating it if asked), then the file lands inside it.
            var folderId = drawingIsRevision || drawingFolderPicker is null
                ? null
                : await drawingFolderPicker.ResolveFolderAsync(CancellationToken.None);
            return await Store.FileAsDrawingAsync(
                item.DocumentControlItemId, filingProjectId, code, title, drawingRevision.Trim(), targetDrawingId, folderId);
        });
    }

    private async Task DoFileAsPaymentCertificate()
    {
        var item = Selected;
        if (item is null || busy) return;
        decimal? amount = null;
        if (!string.IsNullOrWhiteSpace(certificateAmountText))
        {
            if (!decimal.TryParse(certificateAmountText.Replace("£", "").Replace(",", "").Trim(),
                    System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                actionError = "The certified amount isn't a number — clear it or correct it.";
                return;
            }
            amount = parsed;
        }
        if (certificateIssuedDate is null)
        {
            actionError = "Set the certificate's issued date.";
            return;
        }
        // Date-only, pinned to UTC so the stored day never drifts with the browser's timezone
        // (the same rule as the portal upload's expiry date).
        var issued = new DateTimeOffset(DateTime.SpecifyKind(certificateIssuedDate.Value.Date, DateTimeKind.Utc));
        await RunFiling("Filing payment certificate", () =>
            Store.FileAsPaymentCertificateAsync(
                item.DocumentControlItemId, filingProjectId, certificateNumber.Trim(), amount,
                issued, string.IsNullOrWhiteSpace(certificateClaimId) ? null : certificateClaimId));
    }

    private async Task DoFileToSubcontractor()
    {
        var item = Selected;
        if (item is null || busy) return;
        // Date-only, pinned to UTC — the same conversion the portal upload applies to its expiry.
        DateTimeOffset? expires = subcontractorExpiry is null
            ? null
            : new DateTimeOffset(DateTime.SpecifyKind(subcontractorExpiry.Value.Date, DateTimeKind.Utc));
        await RunFiling("Filing to subcontractor", () =>
            Store.FileToSubcontractorAsync(item.DocumentControlItemId, subcontractorId, subcontractorKind, expires));
    }

    private Task DoDiscard()
    {
        var item = Selected;
        if (item is null || busy) return Task.CompletedTask;
        return RunFiling("Discarding", () => Store.DiscardAsync(item.DocumentControlItemId));
    }

    private Task DoRestore()
    {
        var item = Selected;
        if (item is null || busy) return Task.CompletedTask;
        return RunFiling("Restoring", () => Store.RestoreAsync(item.DocumentControlItemId));
    }

    // Extraction creates several items and resolves the original, so the short in-place update
    // RunFiling does isn't enough — the whole (uncached) list is re-read instead.
    private async Task DoExtractArchive()
    {
        var item = Selected;
        if (item is null || busy) return;
        actionError = null;
        doneNote = null;
        try
        {
            busyLabel = "Extracting archive";
            busy = true;
            var extracted = await Store.ExtractArchiveAsync(item.DocumentControlItemId);
            items = await Store.ListAsync();
            // The archive itself is now Filed ("Extracted n files…") and has left the queue; its
            // contents are the new queue rows — badged so they stand out from the rest.
            freshIds.Clear();
            foreach (var child in extracted) freshIds.Add(child.DocumentControlItemId);
            CloseAfterAction(extracted.Count switch
            {
                0 => $"Nothing new to extract from {item.FileName} — its files were already in the queue.",
                1 => $"Extracted 1 file from {item.FileName} into the queue — it's marked \"Extracted\" in the list. Select it to file it.",
                _ => $"Extracted {extracted.Count} files from {item.FileName} into the queue — they're marked \"Extracted\" in the list. Select one to file it."
            });
        }
        catch (Jewel.JPMS.Cqrs.CommandFailedException ex) { actionError = ex.Message; }
        catch { actionError = "That didn't complete. Please try again."; }
        finally { busy = false; }
    }

    private async Task RunFiling(string label, Func<Task<DocumentControlItem>> action)
    {
        actionError = null;
        doneNote = null;
        try
        {
            busyLabel = label;
            busy = true;
            var updated = await action();
            items = AllItems
                .Select(existing => existing.DocumentControlItemId == updated.DocumentControlItemId ? updated : existing)
                .ToList();
            CloseAfterAction(updated.Status switch
            {
                DocumentControlStatus.Filed => $"{updated.FileName} filed as {updated.FiledLabel}. Select the next document.",
                DocumentControlStatus.Discarded => $"{updated.FileName} discarded — restorable from the Discarded view. Select the next document.",
                _ => $"{updated.FileName} is back in the queue. Select the next document."
            });
        }
        catch (Jewel.JPMS.Cqrs.CommandFailedException ex) { actionError = ex.Message; }
        catch { actionError = "That didn't complete. Please try again."; }
        finally { busy = false; }
    }

}

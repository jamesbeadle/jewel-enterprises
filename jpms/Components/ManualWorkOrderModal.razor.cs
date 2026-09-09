using Jewel.JPMS.Features.Procurement;


namespace Jewel.JPMS.Components;

public partial class ManualWorkOrderModal : IDisposable
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public string ProjectId { get; set; } = "";

    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>Raised after a successful save so hosts can refresh their stores.</summary>
    [Parameter] public EventCallback OnSaved { get; set; }

    /// <summary>Raised (after OnSaved) when releasing an order triggered the automatic
    /// purchase-order email — the note says what happened so the host can show it.</summary>
    [Parameter] public EventCallback<string> OnPoEmailNote { get; set; }

    /// <summary>The order being edited, with its lines — null when raising a new order.
    /// Manual orders for the whole team; awarded/variation/seeded orders open here too for the
    /// MD, FD and administrators (the API enforces the split and refuses everyone else).</summary>
    [Parameter] public ProjectWorkOrderDetail? Editing { get; set; }

    private WorkOrderForm? form;
    private bool IsEditing => Editing is not null;
    private string EditingReference => Editing?.Order.Reference ?? "";
    private bool FormSaveAsDraft => form?.SaveAsDraft ?? false;
    private decimal OrderTotal => form?.OrderTotal ?? 0m;

    private bool createPackage = true;
    private string packageName = "";
    private string lineSearch = "";
    // Sales line id → this package's £ share, as typed (invariant decimal text).
    private readonly Dictionary<string, string> pickedAmounts = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<ReconciliationPackage> allPackages = Array.Empty<ReconciliationPackage>();

    private bool seeded;
    private bool busy;
    private string? saveError;
    // Set once the order itself has saved, so a package failure can be retried
    // without raising a duplicate order.
    private WorkOrder? createdOrder;
    // Set once the purchase-order email has been handed to the server, so a later
    // retry through this form can never email the supplier twice.
    private bool poEmailAttempted;

    // ---- The no-matching-sale guardrail (raising only). A line's cost centre with no priced
    // valuation report line means committing cost with no sale to claim against — the dialog
    // asks for explicit confirmation, and the acknowledged command has the server record the
    // override in the audit trail. ----
    private bool saleWarningOpen;
    // The centres the user has confirmed the warning FOR — a failed create leaves the form
    // editable, so the gate re-checks each attempt's centres against this set rather than
    // trusting a one-off tick that might predate a swap to a different uncovered centre.
    private readonly HashSet<string> acknowledgedUncoveredCentres = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<string> uncoveredCostCentres = Array.Empty<string>();

    // ---- Attachments (record keeping only). Files picked here are STAGED and only uploaded once
    // the order exists (create) or the edit has saved. ----
    private readonly List<IBrowserFile> stagedAttachmentFiles = new();
    private List<WorkOrderAttachment> existingAttachments = new();
    private string? attachmentNote;
    private string? attachmentError;

    // ---- Files from the assistant chat (assistant-opened dialogs only). The quotes the user
    // attached to the conversation the order is being drafted from, offered as ticks so they can
    // be KEPT on the order without re-picking them from disk. The bytes copy server-side on save.
    // Ticks default to the documents (a quote, a SoW) and leave images (usually pasted
    // screenshots of the tracker) unticked. Keyed by attachment id; each remembers which
    // conversation it lives on, because a task's files split across the handover conversation
    // (attached before "draft this order") and the task's own (attached mid-task). ----
    private void HandleFormChanged() => StateHasChanged();

    public void Dispose()
    {
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!IsOpen)
        {
            seeded = false;
            return;
        }
        if (seeded) return;
        seeded = true;
        // The Modal renders children only while open, so each opening mounts a fresh
        // WorkOrderForm which seeds and freshens its own pickers; this resets the modal's half.
        createPackage = !IsEditing;
        packageName = "";
        lineSearch = "";
        pickedAmounts.Clear();
        saveError = null;
        createdOrder = null;
        poEmailAttempted = false;
        saleWarningOpen = false;
        acknowledgedUncoveredCentres.Clear();
        uncoveredCostCentres = Array.Empty<string>();
        stagedAttachmentFiles.Clear();
        existingAttachments = new List<WorkOrderAttachment>();
        attachmentNote = null;
        attachmentError = null;
        if (IsEditing)
        {
            await LoadExistingAttachmentsAsync();
            return;
        }
        // The packaging step's sales side — only needed when raising.
        var packagesTask = Queries.AskAsync(new ListReconciliationPackagesForProject(ProjectId), CancellationToken.None);
        await Task.WhenAll(
            ValuationLines.RefreshAsync(ProjectId, CancellationToken.None),
            Projects.RefreshAsync(CancellationToken.None));
        allPackages = await packagesTask;
    }

    private static decimal? Parse(string text) => WorkOrderForm.Parse(text);

    // Counting sales lines with how much of each is still available to this new package.
    private List<(ValuationLineItem Line, decimal Available)> FilteredSalesLines
    {
        get
        {
            var taken = allPackages
                .SelectMany(package => package.SalesLines)
                .GroupBy(slice => slice.ValuationLineItemId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Sum(slice => slice.Amount), StringComparer.OrdinalIgnoreCase);
            return ValuationLines.Current(ProjectId)
                .Where(line => line.CountsTowardTotals && line.LineAmount != 0m)
                .Where(line => string.IsNullOrWhiteSpace(lineSearch)
                               || line.Description.Contains(lineSearch.Trim(), StringComparison.OrdinalIgnoreCase)
                               || line.CostCode.Contains(lineSearch.Trim(), StringComparison.OrdinalIgnoreCase)
                               || line.SectionName.Contains(lineSearch.Trim(), StringComparison.OrdinalIgnoreCase)
                               || line.VariationRef.Contains(lineSearch.Trim(), StringComparison.OrdinalIgnoreCase)
                               || line.VariationTitle.Contains(lineSearch.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(line => (Line: line,
                    Available: line.LineAmount - (taken.TryGetValue(line.ValuationLineItemId, out var amount) ? amount : 0m)))
                .OrderBy(entry => entry.Line.CostCode, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.Line.DisplayOrder)
                .ToList();
        }
    }

    private void SetSalesAmount(string lineItemId, string? value) => pickedAmounts[lineItemId] = value ?? "";

    private void ToggleSalesLine((ValuationLineItem Line, decimal Available) entry)
    {
        if (pickedAmounts.Remove(entry.Line.ValuationLineItemId)) return;
        // Whole-line default: the full remaining value; edit the amount for a partial share.
        pickedAmounts[entry.Line.ValuationLineItemId] =
            entry.Available.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private decimal SalesTotal => pickedAmounts.Values.Sum(text => Parse(text) ?? 0m);
    private decimal TargetTotal => Math.Round(SalesTotal * FinancialSummaryAssumptions.CostFactor, 2);
    private decimal Difference => TargetTotal - OrderTotal;

    private static string DivisorText =>
        $"{1m + FinancialSummaryAssumptions.MarkupPercent / 100m:0.##}";

    // The packaging half's validation — the core order rules live in WorkOrderForm.
    private string? PackageValidationError
    {
        get
        {
            if (!createPackage || IsEditing || FormSaveAsDraft) return null;
            if (pickedAmounts.Values.Any(text => Parse(text) is not { } amount || amount == 0m))
                return "Every ticked sales line needs a non-zero amount.";
            var linesById = ValuationLines.Current(ProjectId)
                .ToDictionary(line => line.ValuationLineItemId, StringComparer.OrdinalIgnoreCase);
            foreach (var picked in pickedAmounts)
            {
                if (!linesById.TryGetValue(picked.Key, out var salesLine)) continue;
                var amount = Parse(picked.Value)!.Value;
                if (Math.Sign(amount) != Math.Sign(salesLine.LineAmount))
                    return $"\"{Truncate(DescriptionFor(salesLine), 40)}\" — the share must carry the line's sign ({Money(salesLine.LineAmount)}).";
                var available = salesLine.LineAmount - allPackages
                    .SelectMany(package => package.SalesLines)
                    .Where(slice => string.Equals(slice.ValuationLineItemId, picked.Key, StringComparison.OrdinalIgnoreCase))
                    .Sum(slice => slice.Amount);
                if (Math.Abs(amount) > Math.Abs(available))
                    return $"\"{Truncate(DescriptionFor(salesLine), 40)}\" — only {Money(available)} of the line is still available.";
            }
            return null;
        }
    }

    private async Task SaveAsync()
    {
        if (busy || form is null || !form.CanSave) return;
        if (PackageValidationError is not null) return;
        var draft = form.TryBuildDraft();
        if (draft is null) return;
        // The guardrail fires on raising only (drafts included — the commitment is intended the
        // moment it's drafted), and never once the order itself has saved (the package-retry
        // path). Any uncovered centre not yet confirmed reopens the dialog, so editing the
        // lines after a failed attempt can't ride on an earlier acknowledgement.
        if (!IsEditing && createdOrder is null)
        {
            uncoveredCostCentres = FindUncoveredCostCentres(draft.Lines);
            if (uncoveredCostCentres.Any(code => !acknowledgedUncoveredCentres.Contains(code)))
            {
                saleWarningOpen = true;
                return;
            }
        }
        busy = true;
        saveError = null;
        try
        {
            // Editing: the whole editable surface travels in one command — no packaging step.
            if (Editing is not null)
            {
                await Commands.SendAsync(new UpdateManualWorkOrder(
                    ProjectId, Editing.Order.WorkOrderId, draft.SubcontractorId,
                    draft.Title, draft.Scope, form.BuildEditedLines().ToList(),
                    draft.ProgrammeStart, draft.TargetCompletion, draft.ProgrammeNotes,
                    DepositRequired: draft.DepositRequired,
                    DepositPercent: draft.DepositPercent), CancellationToken.None);
                var editAttachmentNote = await UploadStagedAttachmentsAsync(Editing.Order.WorkOrderId);
                seeded = false; // reseed fresh on next open
                await OnSaved.InvokeAsync();
                if (!string.IsNullOrWhiteSpace(editAttachmentNote)) await OnPoEmailNote.InvokeAsync(editAttachmentNote);
                return;
            }

            // Step 1 — the order itself (skipped on a retry after a package failure,
            // so the order is never raised twice).
            createdOrder ??= await Commands.SendAsync(new CreateManualWorkOrder(
                ProjectId, draft.SubcontractorId, draft.Title, draft.Scope,
                Auth.CurrentUser?.Email ?? "", draft.Lines.ToList(),
                draft.ProgrammeStart, draft.TargetCompletion, draft.ProgrammeNotes,
                SaveAsDraft: draft.SaveAsDraft,
                DepositRequired: draft.DepositRequired,
                DepositPercent: draft.DepositPercent,
                UncoveredCostCentresAcknowledged: uncoveredCostCentres.Count > 0), CancellationToken.None);

            // Step 1.5 — the record-keeping attachments, straight onto the fresh order.
            attachmentNote = await UploadStagedAttachmentsAsync(createdOrder.WorkOrderId) ?? attachmentNote;

            // Step 2 — the package, with the fresh order already assigned. Never for a draft:
            // packages carry approved scope, and a draft hasn't been approved yet.
            if (!draft.SaveAsDraft && createPackage && pickedAmounts.Count > 0)
            {
                var slices = pickedAmounts
                    .Where(picked => Parse(picked.Value) is { } amount && amount != 0m)
                    .Select(picked => new PackageSalesSlice(picked.Key, Parse(picked.Value)!.Value))
                    .ToList();
                await Commands.SendAsync(new SaveReconciliationPackage(
                    ProjectId,
                    null,
                    string.IsNullOrWhiteSpace(packageName) ? draft.Title : packageName.Trim(),
                    new List<string> { createdOrder.WorkOrderId },
                    slices), CancellationToken.None);
            }

            // Step 3 — the promised purchase-order email (released orders only; a draft's email
            // waits for approval). Non-fatal by design.
            string? poEmailNote = null;
            if (!draft.SaveAsDraft && !poEmailAttempted)
            {
                var supplier = form.SelectedSupplier;
                if (supplier is null || string.IsNullOrWhiteSpace(supplier.ContactEmail))
                {
                    poEmailNote = $"{createdOrder.Reference} was raised, but the supplier has no email address in the "
                        + "directory so the purchase order wasn't emailed — add one, then send it from the PO page.";
                }
                else
                {
                    poEmailAttempted = true;
                    var projectName = Projects.Find(ProjectId)?.Name ?? "";
                    // A measured line emails as it prints: its real quantity, unit and total.
                    var emailLines = draft.Lines
                        .Select(line => line.Quantity is > 0m && line.UnitCost is not null
                            ? new WorkOrderPoEmail.Line(
                                line.Title, line.Quantity.Value,
                                string.IsNullOrWhiteSpace(line.Unit) ? "item" : line.Unit.Trim(),
                                line.Amount)
                            : new WorkOrderPoEmail.Line(line.Title, 1m, "item", line.Amount))
                        .ToList();
                    try
                    {
                        var outcome = await Commands.SendAsync(new SendWorkOrderPoEmail(
                            createdOrder.WorkOrderId,
                            WorkOrderPoEmail.Subject(createdOrder, string.IsNullOrWhiteSpace(projectName) ? ProjectId : projectName),
                            WorkOrderPoEmail.Body(createdOrder, supplier.CompanyName, emailLines, projectName)),
                            CancellationToken.None);
                        poEmailNote = outcome.Sent
                            ? $"{createdOrder.Reference} was raised and the purchase order was emailed to {outcome.RecipientEmail}."
                            : $"{createdOrder.Reference} was raised. {outcome.FailureNote}";
                    }
                    catch (CommandFailedException ex)
                    {
                        poEmailNote = $"{createdOrder.Reference} was raised, but the purchase-order email couldn't be sent: "
                            + $"{ex.Message} You can send it from the PO page.";
                    }
                    catch
                    {
                        poEmailNote = $"{createdOrder.Reference} was raised, but the purchase-order email couldn't be sent "
                            + "— you can send it from the PO page.";
                    }
                }
            }

            seeded = false; // reseed fresh on next open
            await OnSaved.InvokeAsync();
            // One note channel, one message: the PO-email outcome and any attachment failure
            // travel together.
            var savedNote = string.Join(" ", new[] { poEmailNote, attachmentNote }.Where(note => !string.IsNullOrWhiteSpace(note)));
            if (!string.IsNullOrWhiteSpace(savedNote)) await OnPoEmailNote.InvokeAsync(savedNote);
        }
        catch (CommandFailedException ex)
        {
            saveError = IsEditing
                ? $"Couldn't save the changes: {ex.Message}"
                : createdOrder is null
                ? $"Couldn't raise the order: {ex.Message}"
                : $"The order was raised ({createdOrder.Reference}) but the package couldn't be created: {ex.Message} — fix and save again, or close and build the package from the Packages section.";
        }
        finally
        {
            busy = false;
        }
    }

    private async Task ConfirmSaleWarningAsync()
    {
        foreach (var code in uncoveredCostCentres) acknowledgedUncoveredCentres.Add(code);
        saleWarningOpen = false;
        await SaveAsync();
    }

    private void CancelSaleWarning() => saleWarningOpen = false;

    /// <summary>The order's cost centres with no priced valuation report line — no contract or
    /// variation order sale to set the committed cost against. Declined/TBC lines don't count
    /// as cover (CountsTowardTotals); the server applies the same rule as the final word.</summary>
    private IReadOnlyList<string> FindUncoveredCostCentres(IReadOnlyList<ManualWorkOrderLine> orderLines)
    {
        var pricedCodes = ValuationLines.Current(ProjectId)
            .Where(line => line.CountsTowardTotals)
            .Select(line => line.CostCode);
        var pricedSet = new HashSet<string>(pricedCodes, StringComparer.OrdinalIgnoreCase);
        return orderLines
            .Select(line => line.CostCode)
            .Where(code => !pricedSet.Contains(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void OnAttachmentFilesSelected(InputFileChangeEventArgs e)
    {
        attachmentError = null;
        stagedAttachmentFiles.AddRange(e.GetMultipleFiles(20));
    }

    private async Task LoadExistingAttachmentsAsync()
    {
        if (Editing is null) return;
        try { existingAttachments = (await WorkOrderAttachments.ListAsync(Editing.Order.WorkOrderId)).ToList(); }
        catch { existingAttachments = new List<WorkOrderAttachment>(); }
    }

    private async Task RemoveExistingAttachmentAsync(WorkOrderAttachment attachment)
    {
        if (busy || Editing is null) return;
        attachmentError = null;
        try
        {
            existingAttachments = (await WorkOrderAttachments.RemoveAsync(
                Editing.Order.WorkOrderId, attachment.WorkOrderAttachmentId)).ToList();
        }
        catch (CommandFailedException ex) { attachmentError = ex.Message; }
        catch { attachmentError = "Couldn't remove that attachment. Please try again."; }
    }

    /// <summary>Uploads the staged files one at a time (successes leave the staged list, so a
    /// retry only re-sends what failed). Returns a human note when anything failed, else null.</summary>
    private async Task<string?> UploadStagedAttachmentsAsync(string workOrderId)
    {
        if (stagedAttachmentFiles.Count == 0) return null;
        var failed = new List<string>();
        foreach (var file in stagedAttachmentFiles.ToList())
        {
            try
            {
                await WorkOrderAttachments.UploadFilesAsync(workOrderId, new[] { file });
                stagedAttachmentFiles.Remove(file);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed.Add(file.Name + (string.IsNullOrWhiteSpace(ex.Message) ? "" : $" ({ex.Message})"));
            }
        }
        if (failed.Count == 0) return null;
        return (failed.Count == 1 ? "One attachment" : $"{failed.Count} attachments")
            + " couldn't be stored: " + string.Join("; ", failed)
            + " — the order itself saved; add the file(s) from its PO page.";
    }

    private static string FormatBytes(long bytes) =>
        bytes >= 1_048_576 ? $"{bytes / 1_048_576.0:0.#} MB"
        : bytes >= 1024 ? $"{bytes / 1024.0:0.#} KB"
        : $"{bytes} B";

    // Mirrors the Valuation Report tab's CodeFor so the picker cross-references against that tab.
    private static string Ref(ValuationLineItem line) =>
        line.ElementType == ValuationElementType.Variation
            ? (string.IsNullOrWhiteSpace(line.VariationRef) ? line.CostCode : line.VariationRef)
            : (string.IsNullOrWhiteSpace(line.CostCode) ? line.SectionCode : line.CostCode);

    private static string RefTitle(ValuationLineItem line) =>
        line.ElementType == ValuationElementType.Variation
            ? line.VariationTitle
            : string.IsNullOrWhiteSpace(line.SectionCode)
                ? line.SectionName
                : $"{line.SectionCode} — {line.SectionName}";

    // Variation lines mirror an approved VO whose descriptive text lives in VariationTitle.
    private static string DescriptionFor(ValuationLineItem line) =>
        line.ElementType == ValuationElementType.Variation && string.IsNullOrWhiteSpace(line.Description)
            ? line.VariationTitle
            : line.Description;

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

}

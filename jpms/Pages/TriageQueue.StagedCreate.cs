using Jewel.JPMS.Contracts.Audit;
using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Todos;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;
using Jewel.JPMS.Features.Triage.Workspace;

namespace Jewel.JPMS.Pages;

public partial class TriageQueue
{
    private sealed record StagedCreateOutcome(CreatedNowRecord Record, string? UploadError);

    /// <summary>
    /// Raises the staged record and tags the email to it — the create-on-apply body, shared
    /// verbatim by Apply and by System Actions' "Create now". Every command goes out with
    /// AllowCrossPathway: true — the pane choice IS the cross-filing decision (the confirm was
    /// retired 2026-08-28), and true keeps an older api from prompting.
    /// </summary>
    private async Task<StagedCreateOutcome> RaiseStagedRecordAsync(
        StagedRecordCreate staged, MailboxMessage anchor, LinkThreadScope scope)
    {
        if (staged.Kind == StagedRecordKind.BidPackage)
        {
            busyLabel = "Creating bid package";
            var package = await Intake.CreateBidPackageFromMessageAsync(new CreateBidPackageFromMessage(
                anchor.Id, triageProjectId, staged.Title.Trim(), staged.Trade?.Trim() ?? "",
                InternetMessageId: anchor.InternetMessageId,
                Scope: scope,
                AllowCrossPathway: true));
            return new StagedCreateOutcome(
                new CreatedNowRecord(package.Reference, "bid package", staged.Title.Trim()), null);
        }
        if (staged.Kind == StagedRecordKind.WorkOrder)
        {
            // The full manual-order surface staged in System Actions, raised through the
            // same rules as the Work Orders tab (numbering, draft semantics, cost-code
            // master guard) with the email tagged to the new order.
            busyLabel = "Raising work order";
            var orderLines = staged.EnteredLines
                .Where(line => line.CostCode != "" && line.Amount is { } amount && amount != 0m)
                .Select(line => new ManualWorkOrderLine(
                    line.CostCode, line.Title.Trim(), line.Amount!.Value, line.Description.Trim()))
                .ToList();
            var raisedOrder = await Intake.CreateWorkOrderFromMessageAsync(new CreateWorkOrderFromMessage(
                anchor.Id, triageProjectId, staged.SubcontractorId,
                staged.Title.Trim(), staged.Scope.Trim(), orderLines,
                ProgrammeStart: AsUtcDate(staged.ProgrammeStart),
                TargetCompletion: AsUtcDate(staged.TargetCompletion),
                ProgrammeNotes: staged.ProgrammeNotes.Trim(),
                SaveAsDraft: staged.SaveAsDraft,
                DepositRequired: staged.DepositRequired,
                DepositPercent: staged.DepositRequired
                    ? StagedRecordCreate.ParseDecimal(staged.DepositPercentText)
                    : null,
                InternetMessageId: anchor.InternetMessageId,
                // Named LinkScope on this command only: the order's own Scope (works text)
                // already owns the name — see CreateWorkOrderFromMessage.
                LinkScope: scope,
                // Ticked email attachments — copied onto the order server-side (record
                // keeping only; never sent to the supplier).
                AttachmentIds: staged.EmailAttachmentIds.Count > 0 ? staged.EmailAttachmentIds.ToList() : null,
                AllowCrossPathway: true));

            // The email the modal's warning promised: a released (non-draft) order sends
            // its purchase order to the supplier there and then. Non-fatal by design —
            // the order is raised and the email tagged to it either way; the note (shown
            // where the cleared selection was) says what happened.
            if (!staged.SaveAsDraft)
            {
                busyLabel = "Emailing purchase order";
                (poEmailNote, poEmailNoteIsSuccess) = await TrySendWorkOrderPoEmailAsync(raisedOrder, orderLines);
            }

            var orderRecord = new CreatedNowRecord(
                raisedOrder.Reference,
                staged.SaveAsDraft ? "draft work order" : "work order",
                staged.Title.Trim());

            // Files picked from this computer land straight after the order exists —
            // multipart to the order's attachment endpoint. Record keeping only: never
            // part of the purchase-order email above.
            if (staged.UploadFiles.Count > 0)
            {
                busyLabel = "Uploading attachments";
                try
                {
                    await WorkOrderAttachments.UploadFilesAsync(raisedOrder.WorkOrderId, staged.UploadFiles.ToList());
                }
                catch (Exception ex)
                {
                    var fileCount = staged.UploadFiles.Count;
                    return new StagedCreateOutcome(orderRecord,
                        $"{raisedOrder.Reference} was raised and this email tagged to it, but the picked "
                        + $"file{(fileCount == 1 ? "" : "s")} couldn't be uploaded — add "
                        + $"{(fileCount == 1 ? "it" : "them")} again from the order's PO page. ({ex.Message})");
                }
            }
            return new StagedCreateOutcome(orderRecord, null);
        }
        if (staged.Kind == StagedRecordKind.Defect)
        {
            // The defect staged in System Actions, raised through the same rules as a manual
            // defect (numbering, Open status) with the email tagged to it.
            busyLabel = "Raising defect";
            var defect = await Intake.CreateDefectFromMessageAsync(new Jewel.JPMS.Contracts.Closeout.CreateDefectFromMessage(
                anchor.Id, triageProjectId,
                staged.Description.Trim(),
                staged.DefectLocation.Trim(),
                staged.DefectAssignedTo.Trim(),
                InternetMessageId: anchor.InternetMessageId,
                Scope: scope,
                AllowCrossPathway: true));
            return new StagedCreateOutcome(
                new CreatedNowRecord(defect.Reference, "defect", staged.DisplayTitle), null);
        }

        if (staged.Kind == StagedRecordKind.Inventory)
        {
            // The inventory item staged in the Supplier pane's Actions, added through the same
            // rules as one added on the project's Inventory tab (INV numbering) with the
            // supplier's email tagged to it.
            busyLabel = "Adding inventory item";
            var item = await Intake.CreateInventoryItemFromMessageAsync(new Jewel.JPMS.Contracts.Inventory.CreateInventoryItemFromMessage(
                anchor.Id, triageProjectId,
                staged.Title.Trim(),
                staged.Description.Trim(),
                staged.InventoryLocation.Trim(),
                staged.InventoryLocationDetails.Trim(),
                InternetMessageId: anchor.InternetMessageId,
                Scope: scope,
                AllowCrossPathway: true));
            return new StagedCreateOutcome(
                new CreatedNowRecord(item.Reference, "inventory item", staged.Title.Trim()), null);
        }

        if (staged.Kind == StagedRecordKind.SiteInstruction)
        {
            // The site instruction written in the Internal pane's Actions, raised through the
            // same rules as one raised on the project's Site Instructions page (SI numbering)
            // with the prompting email tagged to it.
            busyLabel = "Raising site instruction";
            var instruction = await Intake.CreateSiteInstructionFromMessageAsync(new Jewel.JPMS.Contracts.SiteInstructions.CreateSiteInstructionFromMessage(
                anchor.Id, triageProjectId,
                staged.Title.Trim(),
                staged.Description.Trim(),
                staged.SiteInstructionLocation.Trim(),
                InternetMessageId: anchor.InternetMessageId,
                Scope: scope,
                AllowCrossPathway: true));
            return new StagedCreateOutcome(
                new CreatedNowRecord(instruction.Reference, "site instruction", staged.Title.Trim()), null);
        }

        if (staged.Kind == StagedRecordKind.CalendarEvent)
        {
            // The calendar event staged in System Actions, raised through the same rules as one
            // added on the Calendar tab (CAL numbering, midnight-UTC date) with the email tagged
            // to it.
            busyLabel = "Raising calendar event";
            var calendarEvent = await Intake.CreateCalendarEventFromMessageAsync(
                staged.CalendarEvent.ToCommand(anchor.Id, anchor.InternetMessageId, triageProjectId, scope, allowCrossPathway: true));
            return new StagedCreateOutcome(
                new CreatedNowRecord(calendarEvent.Reference, "calendar event", calendarEvent.Title), null);
        }

        if (staged.Kind == StagedRecordKind.BuildingControlInspection)
        {
            // The inspection staged in System Actions, raised through the same rules as one added
            // on the Building Control tab (BCI numbering, foot of the running order, Booked when
            // dated) with the inspector's email tagged to it. Requires the project's case; the
            // server's refusal lands in the red bar with its own wording.
            busyLabel = "Raising building control inspection";
            var inspection = await Intake.CreateBuildingControlInspectionFromMessageAsync(
                staged.BuildingControlInspection.ToCommand(anchor.Id, anchor.InternetMessageId, triageProjectId, scope, allowCrossPathway: true));
            return new StagedCreateOutcome(
                new CreatedNowRecord(inspection.Reference, "building control inspection", inspection.StageName), null);
        }

        busyLabel = staged.RequestKind == RequestType.Rfi ? "Raising RFI" : "Creating request";
        var request = await Intake.CreateRequestFromMessageAsync(new CreateRequestFromMessage(
            anchor.Id, triageProjectId, staged.RequestKind, "", staged.Title.Trim(),
            staged.Description?.Trim() ?? "",
            DrawingRef: NullIfBlank(staged.DrawingRef),
            ResponseDue: ParseDate(staged.ResponseDue),
            InternetMessageId: anchor.InternetMessageId,
            AddToProgramme: staged.AddToProgramme,
            Scope: scope,
            AllowCrossPathway: true));
        return new StagedCreateOutcome(
            new CreatedNowRecord(
                request.Reference,
                staged.RequestKind == RequestType.Rfi ? "RFI" : "request",
                staged.Title.Trim()),
            null);
    }

    /// <summary>
    /// System Actions' "Create now": raises the staged record IMMEDIATELY — same body as the
    /// apply's create (record raised, email tagged to it, PO email for a released order) — so the
    /// new record exists and can be worked with (linked elsewhere, named in the reply, picked in
    /// the tag pickers) before the rest of the triage lands. The chip in the pane swaps from
    /// "will raise" to the raised reference; Apply then lands whatever else is staged, with
    /// nothing left to double-create.
    /// </summary>
    private async Task DoCreateStagedNow()
    {
        if (busy) return;
        if (selected is not { } anchor || stagedCreate is not { } staged) return;
        if (!StagedCreateReady)
        {
            actionError = staged.Kind switch
            {
                StagedRecordKind.Defect => "Describe the defect first — then Create now.",
                StagedRecordKind.Inventory => "Name the product first — then Create now.",
                StagedRecordKind.SiteInstruction => "Give the instruction a title first — then Create now.",
                _ => "Give the staged record a title first — then Create now."
            };
            return;
        }
        if (string.IsNullOrWhiteSpace(triageProjectId))
        {
            actionError = "To create the record now, set the email's Project in the bar above first.";
            return;
        }
        // The same "decision not yet made" gates as Apply, for the decisions this act consumes.
        if (StagedCalendarEventProblem is { } calendarNowProblem)
        {
            actionError = $"The staged calendar event isn't ready — {calendarNowProblem}";
            return;
        }
        if (StagedBuildingControlInspectionProblem is { } inspectionNowProblem)
        {
            actionError = $"The staged inspection isn't ready — {inspectionNowProblem}";
            return;
        }
        if (staged is { Kind: StagedRecordKind.WorkOrder } stagedOrder
            && stagedOrder.WorkOrderProblem is { } orderProblem)
        {
            actionError = $"The staged work order isn't ready — {orderProblem}";
            return;
        }
        if (staged is { Kind: StagedRecordKind.Defect } stagedDefect
            && stagedDefect.DefectProblem is { } defectProblem)
        {
            actionError = $"The staged defect isn't ready — {defectProblem}";
            return;
        }
        if (staged is { Kind: StagedRecordKind.Inventory } stagedInventory
            && stagedInventory.InventoryProblem is { } inventoryProblem)
        {
            actionError = $"The staged inventory item isn't ready — {inventoryProblem}";
            return;
        }
        if (staged is { Kind: StagedRecordKind.SiteInstruction } stagedInstruction
            && stagedInstruction.SiteInstructionProblem is { } instructionProblem)
        {
            actionError = $"The staged site instruction isn't ready — {instructionProblem}";
            return;
        }
        // Creating now tags the email to the new record, so the thread-spread decision must be
        // made — the Relevant Event answer can wait for Apply, which is what consumes it.
        if (triageEntireThread is null)
        {
            actionError = "Answer Entire thread — Yes or No — so Create now knows how far the email tag spreads.";
            return;
        }

        var scope = triageEntireThread == true ? LinkThreadScope.EntireThread : LinkThreadScope.MessageOnly;
        actionError = null;
        busy = true;
        try
        {
            var created = await RaiseStagedRecordAsync(staged, anchor, scope);
            stagedCreate = null;
            createdNowRecords.Add(created.Record);
            if (created.UploadError is not null)
            {
                actionError = created.UploadError;
                return;
            }
            // The record exists and this email is tagged to it — surface it in Recently
            // processed and show the tag on the email itself. The email stays selected: the
            // rest of the triage (reply, tags, to-dos, the two Yes/No answers) still lands
            // with Apply.
            await Task.WhenAll(LoadRecentTriageAsync(), RefreshSelectedTagsAsync(anchor));
        }
        catch (CommandFailedException ex)
        {
            actionError = ex.Message;
        }
        catch
        {
            actionError = "That didn't complete. Please try again.";
        }
        finally { busy = false; }
    }

    /// <summary>Sends the purchase-order email a released (non-draft) work order promised —
    /// the same covering email every other route sends (WorkOrderPoEmail). Never throws: the
    /// order is already raised, so the outcome is a note, not an error.</summary>
    private async Task<(string Note, bool Sent)> TrySendWorkOrderPoEmailAsync(
        WorkOrder order, IReadOnlyList<ManualWorkOrderLine> orderLines)
    {
        var supplier = (Subcontractors.Current ?? Array.Empty<Subcontractor>()).FirstOrDefault(sub =>
            string.Equals(sub.SubcontractorId, order.SubcontractorId, StringComparison.OrdinalIgnoreCase));
        if (supplier is null || string.IsNullOrWhiteSpace(supplier.ContactEmail))
            return ($"{order.Reference} was raised, but the supplier has no email address in the directory "
                + "so the purchase order wasn't emailed — add one, then send it from the order's PO page.", false);

        var projectName = Projects.Find(triageProjectId)?.Name ?? "";
        var emailLines = orderLines
            .Select(line => new WorkOrderPoEmail.Line(line.Title, 1m, "item", line.Amount))
            .ToList();
        try
        {
            var outcome = await Commands.SendAsync(new SendWorkOrderPoEmail(
                order.WorkOrderId,
                WorkOrderPoEmail.Subject(order, string.IsNullOrWhiteSpace(projectName) ? triageProjectId : projectName),
                WorkOrderPoEmail.Body(order, supplier.CompanyName, emailLines, projectName)),
                CancellationToken.None);
            return outcome.Sent
                ? ($"{order.Reference} was raised and the purchase order was emailed to {outcome.RecipientEmail}.", true)
                : ($"{order.Reference} was raised. {outcome.FailureNote}", false);
        }
        catch (CommandFailedException ex)
        {
            return ($"{order.Reference} was raised, but the purchase-order email couldn't be sent: "
                + $"{ex.Message} You can send it from the order's PO page.", false);
        }
        catch
        {
            return ($"{order.Reference} was raised, but the purchase-order email couldn't be sent "
                + "— you can send it from the order's PO page.", false);
        }
    }

    // Each edit also marks the envelope as the user's (2026-08-28): the reply-all prefill rides
    // in on the detail fetch, and an address or subject typed BEFORE that slow fetch lands must
    // never be overwritten by it — first touch takes ownership, the late prefill backs off.
    private void OnReplyToInput(string value) { replyToField = value; replyEnvelopePrefilled = true; }
    private void OnReplyCcInput(string value) { replyCcField = value; replyEnvelopePrefilled = true; }
    private void OnReplyBccInput(string value) { replyBccField = value; replyEnvelopePrefilled = true; }
    private void OnReplySubjectInput(string value) { replySubject = value; replyEnvelopePrefilled = true; }

}

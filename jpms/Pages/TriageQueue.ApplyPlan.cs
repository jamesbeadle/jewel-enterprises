using Jewel.JPMS.Features.Triage;

namespace Jewel.JPMS.Pages;

public partial class TriageQueue
{
    /// <summary>One apply's inputs, captured at the button press so every step and every refusal
    /// reads the same snapshot the triager saw.</summary>
    private sealed record ApplyPlan(
        MailboxMessage? Anchor,
        bool Replying,
        List<TodoItemDraft> Drafts,
        List<LinkableRecord> Picks,
        bool CreateReady,
        bool RelevantEvent,
        bool Discarding,
        List<string> InheritStems,
        LinkThreadScope Scope,
        bool CreatedNowOnly);

    private ApplyPlan BuildApplyPlan()
    {
        var anchorEmail = selected;
        return new ApplyPlan(
            Anchor: anchorEmail,
            Replying: ReplyDraftPending && anchorEmail is not null,
            Drafts: anchorEmail is null ? new List<TodoItemDraft>() : CurrentTodoDrafts(),
            Picks: pickedRecords.ToList(),
            CreateReady: anchorEmail is not null && StagedCreateReady
                && !string.IsNullOrWhiteSpace(triageProjectId),
            RelevantEvent: relevantEventStaged == true && anchorEmail is not null,
            Discarding: discardArmed && anchorEmail is not null,
            // "Use existing tags" answered Yes: the thread's tag stems, captured now so the apply
            // works from what the triager saw. Resolved to records inside the try — before
            // anything else lands — and linked exactly like picks.
            InheritStems: useThreadTags == true && anchorEmail is not null
                ? SelectedThreadTags.ToList()
                : new List<string>(),
            // One scope for the whole apply: a thread-wide Yes opts every staged action into the thread.
            Scope: triageEntireThread == true ? LinkThreadScope.EntireThread : LinkThreadScope.MessageOnly,
            // A create-now-only triage: the record is already raised and the email tagged to it
            // (Create now did both), so there is nothing left to RUN — but the apply still owns
            // the close-out (queue reload, selection cleared). Letting it through is what
            // un-sticks the Apply button after Create now; every step no-ops on its own guard.
            CreatedNowOnly: anchorEmail is not null && createdNowRecords.Count > 0);
    }

    // StagedCreateReady on its own (no project yet) counts as work so the apply reaches the
    // project gate's refusal rather than returning silently.
    private bool ApplyHasWork(ApplyPlan plan) =>
        plan.Replying || plan.CreateReady || StagedCreateReady || plan.RelevantEvent || plan.Discarding
        || plan.Drafts.Count > 0
        || plan.Picks.Count > 0
        || stagedSystemActions.Count > 0 || queuedReplies.Count > 0
        || stagedDocControlIds.Count > 0
        || plan.InheritStems.Count > 0
        || plan.CreatedNowOnly;

    // Every way an apply refuses to run, in one gauntlet — the reason to show, or null to
    // proceed. The bar's Yes/No pairs start blank on purpose (an apply with any unanswered is a
    // decision not yet made), and every staged half-decision is finish-it-or-clear-it rather
    // than quietly skipped: belt-and-braces behind the disabled button, so no other route into
    // the apply can land with a blank answer.
    private string? ApplyRefusal(ApplyPlan plan)
    {
        if (plan.Anchor is not null && MissingDecisionNames() is { Count: > 0 } missingDecisions)
            return $"Answer {AndJoin(missingDecisions)} — Yes or No — then Apply.";

        // A half-built lined-up email — finish it or remove it, rather than have Apply skip it
        // (or the server reject it after the filing has already landed).
        if (queuedReplies.FirstOrDefault(lined => lined.Problem is not null) is { } notReady)
            return $"A lined-up {(notReady.IsForward ? "forward" : "reply")} ({notReady.AnchorSubject}) isn't ready — {notReady.Problem} Finish it in the Outbox, or remove it.";

        if (DiscardRefusal(plan) is { } discardRefusal) return discardRefusal;

        // Anything staged that files the email against a project needs the Project set — one
        // list (ProjectNeeds: Relevant Event, Document Triage attachments, the thread's existing
        // tags, picked project records, the staged create) shared with the button's hint.
        if (plan.Anchor is not null && string.IsNullOrWhiteSpace(triageProjectId)
            && ProjectNeeds() is { Count: > 0 } projectNeeds)
            return $"To apply {AndJoin(projectNeeds.Select(need => need.What).ToList())}, set the email's Project first — or {AndJoin(projectNeeds.Select(need => need.Undo).Distinct().ToList())}.";

        // A picked record from ANOTHER project than the one the bar says: the picks are cleared
        // whenever the project changes, so this should never happen — but a wrong-project filing
        // is the one mistake worth a second guard.
        if (plan.Anchor is not null && plan.Picks.FirstOrDefault(pick =>
                !string.IsNullOrWhiteSpace(pick.ProjectId)
                && !string.Equals(pick.ProjectId, triageProjectId, StringComparison.OrdinalIgnoreCase)) is { } strayPick)
            return $"{strayPick.Reference} belongs to {ProjectNameOrId(strayPick.ProjectId)}, not {ProjectNameOrId(triageProjectId)} — unpick it, or change the email's Project.";

        if (StagedCreateRefusal(plan) is { } createRefusal) return createRefusal;

        if (plan.Replying)
        {
            if (ParseRecipients(replyToField).Count == 0) return "Add a To recipient to the reply.";
            if (string.IsNullOrWhiteSpace(replySubject)) return "Write a subject for the reply.";
            // A reply alone triages the thread as Replied — pathway-less is fine (answering IS
            // dealing with it); choosing a tab in System Tags files it under that side as well.
        }
        return null;
    }

    // A reply and a discard contradict each other — an email worth answering isn't spam.
    // (Same rule for an unsent forward: send or discard the draft before binning the email.)
    private string? DiscardRefusal(ApplyPlan plan)
    {
        if (!plan.Discarding) return null;
        if (plan.Replying)
            return $"Discard and a {(replyIsForward ? "forward" : "reply")} don't mix — send (or discard) the draft first.";
        if (plan.Picks.Count > 0)
            return "Discard and record links don't mix — unpick the records first.";
        if (plan.InheritStems.Count > 0)
            return "Discard and the thread's existing tags don't mix — answer No to Use existing tags, or disarm the discard.";
        if (plan.RelevantEvent)
            return "Discard and a Relevant Event tag don't mix — answer No to Relevant Event, or disarm the discard.";
        return null;
    }

    // A staged record that isn't complete yet (no subcontractor, no priced line, no
    // description…) — finish it or clear it, rather than let the server reject a half-built
    // record after the to-dos have already been raised. (A staged record with no project is
    // the project gate's job above — ProjectNeeds lists it.)
    private string? StagedCreateRefusal(ApplyPlan plan)
    {
        if (!plan.CreateReady) return null;
        if (stagedCreate is { Kind: StagedRecordKind.WorkOrder } stagedOrder && stagedOrder.WorkOrderProblem is { } orderProblem)
            return $"The staged work order isn't ready — {orderProblem} Finish it in the pathway pane's Actions, or remove it.";
        if (stagedCreate is { Kind: StagedRecordKind.Defect } stagedDefect && stagedDefect.DefectProblem is { } defectProblem)
            return $"The staged defect isn't ready — {defectProblem} Finish it in the pathway pane's Actions, or remove it.";
        if (stagedCreate is { Kind: StagedRecordKind.Inventory } stagedInventory && stagedInventory.InventoryProblem is { } inventoryProblem)
            return $"The staged inventory item isn't ready — {inventoryProblem} Finish it in the pathway pane's Actions, or remove it.";
        if (stagedCreate is { Kind: StagedRecordKind.SiteInstruction } stagedInstruction && stagedInstruction.SiteInstructionProblem is { } instructionProblem)
            return $"The staged site instruction isn't ready — {instructionProblem} Finish it in the pathway pane's Actions, or remove it.";
        if (StagedCalendarEventProblem is { } calendarProblem)
            return $"The staged calendar event isn't ready — {calendarProblem} Finish it in the pathway pane's Actions, or remove it.";
        if (StagedBuildingControlInspectionProblem is { } inspectionProblem)
            return $"The staged inspection isn't ready — {inspectionProblem} Finish it in the pathway pane's Actions, or remove it.";
        return null;
    }
}

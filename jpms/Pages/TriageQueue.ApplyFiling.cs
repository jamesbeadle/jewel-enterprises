using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;

namespace Jewel.JPMS.Pages;

public partial class TriageQueue
{
    // ---- "Use existing tags" answered Yes: resolve the thread's tag stems back to records
    //      FIRST (the same ResolveRecordTags behind the search chips), so a stem that no longer
    //      names anything stops the apply before anything else lands — the same
    //      every-tag-verified-before-anything-saves rule as the rest of the filing. The links
    //      themselves land with the picks. Null = stop the apply (the error is set). ----
    private async Task<IReadOnlyList<LinkableRecord>?> ResolveInheritedRecordsAsync(ApplyPlan plan)
    {
        var stems = plan.InheritStems;
        if (plan.Anchor is null || stems.Count == 0) return Array.Empty<LinkableRecord>();
        busyLabel = "Matching the thread's tags";
        var inherited = await Queries.AskAsync(
            new ResolveRecordTags(stems.Select(TriageEmailDisplay.TagLabel).ToList()), CancellationToken.None);
        if (inherited.Count == 0)
        {
            actionError = "The thread's existing tags couldn't be matched to records — pick this email's records by hand instead.";
            return null;
        }
        // The tags' records must be the bar's project (2026-09-07): the gate already insists a
        // project is set, but the stems only resolve here, so a thread tagged to another
        // project's records — or to two projects', which is why the auto-match left the
        // project blank — is caught with nothing filed. The triager picks by hand instead.
        if (inherited.FirstOrDefault(record => !string.IsNullOrWhiteSpace(record.ProjectId)
                && !string.Equals(record.ProjectId, triageProjectId, StringComparison.OrdinalIgnoreCase)) is { } stray)
        {
            actionError = $"The thread's existing tag {stray.Reference} belongs to {ProjectNameOrId(stray.ProjectId)}, not {ProjectNameOrId(triageProjectId)} — change the email's Project, or answer No to Use existing tags and pick this email's records by hand.";
            return null;
        }
        return inherited;
    }

    // ---- Document Triage: ticked attachments copy out FIRST, so the files are safely in the
    //      queue before anything else (a discard included) moves the email on. Never consumes
    //      the email — only the files are copied out; `filed` deliberately stays untouched. ----
    private async Task SendDocTriageAttachmentsAsync(ApplyPlan plan)
    {
        if (plan.Anchor is not { } anchor || stagedDocControlIds.Count == 0) return;
        busyLabel = "Sending to Document Triage";
        await Commands.SendAsync(
            new SendAttachmentsToDocumentControl(
                anchor.Id, anchor.InternetMessageId,
                stagedDocControlIds.ToList(), NullIfBlank(triageProjectId)),
            CancellationToken.None);
        // One send per apply: clear the ticks (the server skips already-sent ids regardless).
        stagedDocControlIds.Clear();
    }

    // ---- Section 2: to-dos (their command verifies every tag before saving) ----
    private async Task<bool> RaiseTodoDraftsAsync(ApplyPlan plan)
    {
        if (plan.Drafts.Count == 0) return false;
        var anchor = plan.Anchor!;
        busyLabel = "Creating to-dos";
        // No request link here: to-dos are their own concern, and linking the email to a
        // record — a request included — is the filing section's job.
        await Intake.CreateTodoItemsFromMessageAsync(new CreateTodoItemsFromMessage(
            anchor.Id,
            NullIfBlank(triageProjectId),
            plan.Drafts,
            LinkRequestId: null,
            InternetMessageId: anchor.InternetMessageId,
            Pathway: pathway is { } chosenForTodos ? TriagePathways.Label(chosenForTodos) : null,
            Scope: plan.Scope));
        // One batch per apply: clear the rows so nothing can double-raise.
        createTodoRows = new List<TodoDraftRow> { new() };
        return true;
    }

    // ---- Record filing: every staged link applies, whatever picker is open ----
    private async Task<bool> LinkPickedRecordsAsync(ApplyPlan plan)
    {
        if (plan.Anchor is not { } anchor || plan.Picks.Count == 0) return false;
        busyLabel = "Linking";
        foreach (var record in plan.Picks)
        {
            // AllowCrossPathway: true — the pane choice IS the cross-filing decision
            // (confirm retired 2026-08-28; true also keeps an older api from prompting).
            await Intake.LinkMessageToRecordAsync(
                anchor.Id, anchor.InternetMessageId, record.Type, record.RecordId,
                pathway: CostCentrePathwayFor(record),
                allowCrossPathway: true,
                scope: plan.Scope);
        }
        return true;
    }

    // ---- The thread's existing tags, answered Yes: each resolved record links exactly like a
    //      picked one. Records the triager ALSO picked by hand are skipped — one link per record
    //      per apply. allowCrossPathway is true outright: these tags are already on the thread,
    //      so re-filing this reply under them is never a new cross-pathway decision. ----
    private async Task<bool> LinkInheritedRecordsAsync(ApplyPlan plan, IReadOnlyList<LinkableRecord> inheritedRecords)
    {
        if (plan.Anchor is not { } anchor || inheritedRecords.Count == 0) return false;
        busyLabel = "Linking to the thread's tags";
        var picks = plan.Picks;
        var linked = false;
        foreach (var record in inheritedRecords)
        {
            if (picks.Any(pick => pick.Type == record.Type
                && string.Equals(pick.RecordId, record.RecordId, StringComparison.Ordinal)))
                continue;
            await Intake.LinkMessageToRecordAsync(
                anchor.Id, anchor.InternetMessageId, record.Type, record.RecordId,
                pathway: CostCentrePathwayFor(record),
                allowCrossPathway: true,
                scope: plan.Scope);
            linked = true;
        }
        return linked;
    }

    // A Relevant Event answered Yes: link the thread to the project's programme bucket — the
    // record id IS the project id (one bucket per project, SchedulingLinkProvider). Scheduling
    // is a Client-side record, so on a non-client thread this cross-files the thread — allowed
    // without a confirm, like the picks.
    private async Task<bool> TagRelevantEventAsync(ApplyPlan plan)
    {
        if (!plan.RelevantEvent) return false;
        var anchor = plan.Anchor!;
        busyLabel = "Tagging relevant event";
        await Intake.LinkMessageToRecordAsync(
            anchor.Id, anchor.InternetMessageId, RecordType.Scheduling, triageProjectId,
            pathway: null,
            allowCrossPathway: true,
            scope: plan.Scope);
        return true;
    }

    // ---- System actions lined up in the Actions pane — run once the filing above has landed,
    //      each removed as it succeeds so a failed one can be retried without re-running its
    //      predecessors. A failure stops the apply with its reason. An action that tags the
    //      email (Mark as KPI) takes the plan's thread scope — the "Entire thread" answer —
    //      the same way every record link above does. ----
    private async Task RunStagedSystemActionsAsync(ApplyPlan plan)
    {
        foreach (var stagedAction in stagedSystemActions.ToList())
        {
            busyLabel = $"System action: {SystemActionKinds.Label(stagedAction.Kind)}";
            if (stagedAction.ExecuteWithScopeAsync is { } withScope)
                await withScope(plan.Scope);
            else
                await stagedAction.ExecuteAsync();
            stagedSystemActions.Remove(stagedAction);
        }
    }

    // "File it as nothing": tag the thread discarded — restorable from the Tagged tab. Runs
    // after the to-dos so "capture the follow-ups, then bin the email" works.
    private async Task<bool> DiscardAnchorAsync(ApplyPlan plan)
    {
        if (!plan.Discarding) return false;
        var anchor = plan.Anchor!;
        busyLabel = "Discarding";
        await Intake.DiscardMessageAsync(anchor.Id, anchor.InternetMessageId);
        return true;
    }
}

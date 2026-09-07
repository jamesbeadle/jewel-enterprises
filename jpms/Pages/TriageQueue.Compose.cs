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
    // ---- Reply compose (send for real — decision 2026-08-04) ----

    // Reply-all prefill, computed once per selection from the opened email's envelope: the sender
    // (or their Reply-To) goes in To; the original To + Cc — minus whoever is now in To — go in Cc.
    // The projects mailbox itself is filtered out — Cc'ing it would deliver a copy back to the
    // Inbox and land it in the triage queue (decision 2026-08-07: no auto-Cc anywhere).
    private void PrefillReplyEnvelope(MailboxMessage item, MailboxMessageDetail loaded)
    {
        if (replyEnvelopePrefilled) return;
        // A forward's envelope is deliberately blank (FW subject already set) — the late-landing
        // detail must not overwrite it with the reply-all prefill.
        if (replyIsForward) return;

        var toAddress = loaded.ReplyTo ?? loaded.FromEmail ?? item.FromEmail;
        replyToField = toAddress ?? "";

        var ccAddresses = (loaded.To ?? Array.Empty<string>())
            .Concat(loaded.Cc ?? Array.Empty<string>())
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Where(a => !a.Equals(toAddress, StringComparison.OrdinalIgnoreCase))
            // Strip the projects mailbox from the prefill: replying with it on Cc would deliver
            // the sent email back into the Inbox, where it lands in the triage queue again.
            .Where(a => loaded.MailboxAddress is null || !a.Equals(loaded.MailboxAddress, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        replyCcField = string.Join("; ", ccAddresses);

        var subject = loaded.Subject ?? item.Subject;
        replySubject = string.IsNullOrWhiteSpace(subject) ? "RE: (no subject)"
            : subject.TrimStart().StartsWith("RE:", StringComparison.OrdinalIgnoreCase) ? subject.Trim()
            : $"RE: {subject.Trim()}";

        replyEnvelopePrefilled = true;
    }

    private bool ReplyIsSendable =>
        ParseRecipients(replyToField).Count > 0
        && !string.IsNullOrWhiteSpace(replySubject)
        && HtmlHasContent(replyBody);

    // True while a reply is drafted but unsent — the filing panes' own buttons stand down so a
    // written reply can't be silently left behind by a tag-only action; Send applies both.
    private bool ReplyDraftPending => replyOpen && HtmlHasContent(replyBody);

    // The email's global project, giving the attachment picker its context.
    private string ComposeContextProjectId => triageProjectId;

    // What Send will do besides sending, phrased for the note above the button. Null = nothing.
    // Everything the action bar will do, phrased as one sentence ("send your reply, raise 2
    // to-dos and link this email to the selected record"). Null = nothing pending, button disabled.
    private string? PendingSummary
    {
        get
        {
            var parts = new List<string>();
            if (ReplyDraftPending)
                parts.Add(replyIsForward ? "send your forward" : "send your reply");
            if (queuedReplies.Count > 0)
                parts.Add(queuedReplies.Count == 1
                    ? $"send the lined-up {(queuedReplies[0].IsForward ? "forward" : "reply")} to {queuedReplies[0].AnchorFrom}"
                    : $"send {queuedReplies.Count} lined-up emails");
            var todoCount = CurrentTodoDrafts().Count;
            if (todoCount > 0)
                parts.Add(todoCount == 1 ? "raise the to-do" : $"raise {todoCount} to-dos");
            if (pickedRecords.Count > 0)
                parts.Add(pickedRecords.Count == 1
                    ? $"link this email to {pickedRecords[0].Reference}"
                    : $"link this email to {string.Join(", ", pickedRecords.Take(3).Select(r => r.Reference))}{(pickedRecords.Count > 3 ? $" +{pickedRecords.Count - 3} more" : "")}");
            if (useThreadTags == true && SelectedThreadTags is { Count: > 0 } inheritStems)
                parts.Add(inheritStems.Count == 1
                    ? $"file it under the thread's existing tag ({TriageEmailDisplay.TagLabel(inheritStems[0])})"
                    : $"file it under the thread's existing tags ({string.Join(", ", inheritStems.Take(3).Select(TriageEmailDisplay.TagLabel))}{(inheritStems.Count > 3 ? $" +{inheritStems.Count - 3} more" : "")})");
            if (stagedDocControlIds.Count > 0)
                parts.Add(stagedDocControlIds.Count == 1
                    ? "send 1 attachment to Document Triage"
                    : $"send {stagedDocControlIds.Count} attachments to Document Triage");
            // Listed with or without a project: without one the amber hint beside the button
            // says "set the Project first" — before (2026-09-07) the create simply vanished
            // from this sentence and Apply sat disabled with no reason on show.
            if (StagedCreateReady)
                parts.Add(stagedCreate!.Kind switch
                {
                    StagedRecordKind.BidPackage => "create the bid package from this email",
                    StagedRecordKind.WorkOrder => StagedWorkOrderSummary(stagedCreate),
                    StagedRecordKind.Defect => "raise the defect from this email",
                    StagedRecordKind.Inventory => "add the inventory item from this email",
                    StagedRecordKind.SiteInstruction => "raise the site instruction from this email",
                    StagedRecordKind.CalendarEvent => "raise the calendar event from this email",
                    StagedRecordKind.BuildingControlInspection => "raise the building control inspection from this email",
                    _ => stagedCreate.RequestKind == RequestType.Rfi
                        ? "raise the RFI from this email"
                        : "create the request from this email"
                });
            if (stagedSystemActions.Count == 1)
                parts.Add($"run 1 system action ({stagedSystemActions[0].Summary})");
            else if (stagedSystemActions.Count > 1)
                parts.Add($"run {stagedSystemActions.Count} system actions");
            if (relevantEventStaged == true)
                parts.Add("tag it a Relevant Event for the Programme");
            if (discardArmed)
                parts.Add("discard this email and its thread");
            // Create now already raised the record and tagged the email — with nothing else
            // staged, the apply's one remaining job is clearing the dealt-with email from the
            // queue. Without this clause Apply sat disabled after a create-now-only triage
            // ("it expects a staged tag") and the email was stuck open (reported 2026-08-28).
            if (parts.Count == 0 && selected is not null && createdNowRecords.Count > 0)
                return createdNowRecords.Count == 1
                    ? $"clear this email from the queue — {createdNowRecords[0].Reference} is already raised and the email tagged to it"
                    : $"clear this email from the queue — {string.Join(", ", createdNowRecords.Select(r => r.Reference))} are already raised and the email tagged to them";
            if (parts.Count == 0) return null;
            var summary = parts.Count == 1 ? parts[0] : string.Join(", ", parts.Take(parts.Count - 1)) + " and " + parts[^1];
            // A thread-wide Yes changes what an apply MEANS, so the sentence says so — but only when
            // something staged actually spreads (a bare discard is thread-wide regardless).
            if (triageEntireThread == true && !(discardArmed && parts.Count == 1))
                summary += " — covering every email currently in this thread";
            // Lined-up replies inherit the triage's record picks: the sentence says so, because
            // tagging OTHER emails is the one effect a reader wouldn't otherwise expect.
            if (queuedReplies.Count > 0 && pickedRecords.Count > 0)
                summary += $" (the lined-up {(queuedReplies.Count == 1 ? "email's anchor is" : "emails' anchors are")} tagged to the picked records too)";
            return summary;
        }
    }

    // The staged work order phrased for the apply note, counting the record-keeping attachments
    // (ticked email files + picked uploads) it will keep on the new order.
    private static string StagedWorkOrderSummary(StagedRecordCreate staged)
    {
        var label = staged.SaveAsDraft
            ? "raise the draft work order from this email"
            : "raise the work order from this email and email the purchase order to the subcontractor";
        var attachmentCount = staged.EmailAttachmentIds.Count + staged.UploadFiles.Count;
        return attachmentCount == 0
            ? label
            : $"{label} (keeping {attachmentCount} attachment{(attachmentCount == 1 ? "" : "s")} on the order — not emailed)";
    }

    // Done on a pathway pane: confirm the picks and land that window back on the open
    // email — the same place every time. The plain pane-history fallback ("whatever this window
    // showed before") read as a bug in practice: with System Actions earlier in the history,
    // Done appeared to open the RFI form out of nowhere (reported 2026-08-20). Close() first so
    // SystemTags leaves the history entirely — closing the email later must not resurface a
    // confirmed tags window and silently re-block Apply. When the email is already on show in
    // the other window, the plain close is enough — no point opening a mirror copy over here.
    private void ClosePathwayPane(PanelKind pane)
    {
        var side = workspace.SideShowing(pane);
        workspace.Close(pane);
        if (side is not { } paneSide || selected is null) return;
        // On mobile only the left pane is on screen, so the right pane "showing" the email
        // doesn't count as the email being visible — bring it to the one real window.
        var emailVisible = workspace.IsDesktop && workspace.SideShowing(PanelKind.Email) is not null;
        if (!emailVisible) workspace.Show(PanelKind.Email, paneSide);
    }

    // An action just closed the open email (applied, discarded, restored, re-tagged), so the
    // email window and its reading copy have nothing left to show. Bring the queue list back on
    // show wherever the panes were left — without this, an apply run while the mirror covered
    // the inbox landed on two empty windows with the list nowhere in sight (reported
    // 2026-08-28: "loaded without the mailbox selected"). The mirror closes outright (a reading
    // copy of nothing has no reason to wait in the history); the inbox then either resurfaces
    // from that pane's own history or is shown on the left, its home side.
    private void ReturnWorkspaceToQueue()
    {
        workspace.Close(PanelKind.EmailMirror);
        if (workspace.SideShowing(PanelKind.Inbox) is null)
            workspace.Show(PanelKind.Inbox, PanelSide.Left);
    }

    // NOTE (2026-08-27): the old "Apply stands down while a tags window is open" rule is GONE.
    // It fit the one modal System Tags pane; with four standing pathway panes (which also host
    // browsable registers) it left Apply disabled almost permanently, with the reason buried in
    // a tooltip — Nigel filled everything in, pressed Done everywhere he could see, and still
    // couldn't apply. Picks and ticks stage LIVE into the page's one list, and every staged
    // record form is readiness-checked by DoApplyAll itself, so an open pane holds nothing back.

    // True while either of the bar's Yes/No pairs is still blank for the open email. Apply (and
    // save-as-drafts) stand down until both are answered — the pairs deliberately start with
    // NEITHER side picked, so tagging the programme and sweeping the thread are always decisions
    // someone actually made, never a default that slipped through.
    private bool TriageDecisionsMissing =>
        selected is not null && MissingDecisionNames().Count > 0;

    // The blank pairs still awaiting an answer, by their on-screen names — one list feeds both
    // the amber hint next to Apply and the belt-and-braces error inside DoApplyAll, so the two
    // can never drift. "Use existing tags" counts only while its row is on show (the thread
    // actually carries tags to inherit).
    private List<string> MissingDecisionNames()
    {
        var missing = new List<string>();
        if (relevantEventStaged is null) missing.Add("Relevant Event for Programme");
        if (triageEntireThread is null) missing.Add("Entire thread");
        if (SelectedThreadTags.Count > 0 && useThreadTags is null) missing.Add("Use existing tags");
        return missing;
    }

    private static string AndJoin(IReadOnlyList<string> parts) =>
        parts.Count <= 1
            ? parts.FirstOrDefault() ?? ""
            : string.Join(", ", parts.Take(parts.Count - 1)) + " and " + parts[^1];

    // The record tags the open email's thread already carries — the queue row's outline "Thread:"
    // chips, populated only on queue listings (a new reply to an already-linked thread). Empty
    // everywhere else, so the "Use existing tags" row and its gate simply don't exist there.
    private IReadOnlyList<string> SelectedThreadTags =>
        selected?.ThreadTags is { Count: > 0 } tags ? tags : Array.Empty<string>();

    // One staged thing that files the email against a PROJECT, by its on-screen name, and the
    // way to back out of it instead of setting the project.
    private sealed record ProjectNeed(string What, string Undo);

    // Everything staged that NEEDS the email's project — the rule (2026-09-07): any filing that
    // lands the email on a project's record, programme or queue is refused until the Project in
    // the triage bar says which project. Before this the gate covered two cases by name (a
    // Relevant Event, 2026-09-03; Document Triage attachments, 2026-08-28) and a triager
    // answered Yes to "Use existing tags" with no project picked and applied — the thread's
    // tags ARE a project's records, so the email was filed with the project decision never
    // made. Now one list feeds both the amber hint next to Apply and the refusal inside
    // DoApplyAll, exactly like MissingDecisionNames, so the two can never drift and a new
    // project-bound staging is one line here. Record picks count only when the picked record
    // carries a project (the record-less communication registers and company-wide to-dos don't
    // — they file without one by design); the staged create is listed so its "needs a project"
    // reads next to the button instead of leaving Apply silently disabled.
    private List<ProjectNeed> ProjectNeeds()
    {
        var needs = new List<ProjectNeed>();
        if (relevantEventStaged == true)
            needs.Add(new("a Relevant Event for the Programme", "answer No to Relevant Event"));
        if (stagedDocControlIds.Count > 0)
            needs.Add(new(stagedDocControlIds.Count == 1 ? "an attachment for Document Triage" : "attachments for Document Triage", "untick them"));
        if (useThreadTags == true && SelectedThreadTags.Count > 0)
            needs.Add(new("the thread's existing tags", "answer No to Use existing tags"));
        if (pickedRecords.Any(record => !string.IsNullOrWhiteSpace(record.ProjectId)))
            needs.Add(new(pickedRecords.Count == 1 ? "the picked record" : "the picked records", "unpick them"));
        if (StagedCreateReady)
            needs.Add(new("the staged record", "remove it in the pathway pane's Actions"));
        return needs;
    }

    // True while something staged NEEDS the email's project and none is picked. The triage bar
    // — where the email says which job it is — is the cheapest place to set it. Same
    // standing-hint treatment as the Yes/No pairs (2026-08-27: the disable reason stands next
    // to the button), and the same refusal lives in ApplyRefusal as belt-and-braces behind the
    // disabled button.
    private bool ProjectMissing =>
        selected is not null && string.IsNullOrWhiteSpace(triageProjectId) && ProjectNeeds().Count > 0;

    private string ProjectMissingHint =>
        $"Set the Project first — {AndJoin(ProjectNeeds().Select(need => need.What).ToList())} can't file without one";

    // The name a project shows under in a refusal — its name when the list knows it, else the id.
    private string ProjectNameOrId(string projectId) =>
        AllProjects.FirstOrDefault(project => project.ProjectId == projectId)?.Name ?? projectId;

    private string DecisionsMissingHint =>
        $"Answer {AndJoin(MissingDecisionNames())} — Yes or No — first";

    private string ApplyButtonLabel
    {
        get
        {
            var filing = CurrentTodoDrafts().Count > 0
                || pickedRecords.Count > 0
                || (useThreadTags == true && SelectedThreadTags.Count > 0)
                || relevantEventStaged == true
                || stagedSystemActions.Count > 0
                || stagedDocControlIds.Count > 0
                || StagedCreateReady;
            var sendCount = (ReplyDraftPending ? 1 : 0) + queuedReplies.Count;
            if (sendCount > 0)
            {
                var send = sendCount == 1 ? "Send reply" : $"Send {sendCount} replies";
                return filing ? $"{send} & file" : send;
            }
            return discardArmed && !filing ? "Discard email" : "Apply";
        }
    }

    // Open the composer under the open email as a reply or a forward. The two kinds prime the
    // envelope differently — reply-all prefill vs a blank envelope with a "FW:" subject — so
    // switching kind re-primes it (the written body and any extra attachments survive; original
    // attachments picked for a reply are dropped on a switch to forward, because Graph carries
    // the originals on a forward draft automatically).
    private void OpenReplyComposer(bool forward)
    {
        replyOpen = true;
        if (replyIsForward == forward) return;
        replyIsForward = forward;
        replyShowBcc = false;
        if (forward)
        {
            replyToField = replyCcField = replyBccField = "";
            replySubject = MailCompose.ForwardSubjectFor(detail?.Subject ?? selected?.Subject);
            replyAttachments = replyAttachments
                .Where(a => a.Source != ComposeAttachmentSource.OriginalMessage)
                .ToList();
        }
        else
        {
            replyToField = replyCcField = replyBccField = "";
            replySubject = "";
            replyEnvelopePrefilled = false;
            if (selected is { } item && detail is { } loaded) PrefillReplyEnvelope(item, loaded);
        }
    }

    private void DiscardReplyDraft()
    {
        replyOpen = false;
        replyBody = "";
        replyAttachments = Array.Empty<ComposeDraftAttachment>();
        // A discarded forward hands the composer back in reply shape, reply-all re-prefilled, so
        // the next "↩ Reply" press starts from the normal envelope.
        if (replyIsForward)
        {
            replyIsForward = false;
            replyToField = replyCcField = replyBccField = "";
            replySubject = "";
            replyEnvelopePrefilled = false;
            if (selected is { } item && detail is { } loaded) PrefillReplyEnvelope(item, loaded);
        }
    }

    // The shared composer rules (MailCompose), aliased so every call site here reads the same as
    // it always did — the logic itself is defined once for all mail-writing surfaces.
    private static bool HtmlHasContent(string html) => MailCompose.HtmlHasContent(html);

    private void OnReplyAttachmentsChanged(IReadOnlyList<ComposeDraftAttachment> attachments) =>
        replyAttachments = attachments;

    private static IReadOnlyList<(string PartName, Microsoft.AspNetCore.Components.Forms.IBrowserFile File)> UploadPartsOf(
        IReadOnlyList<ComposeDraftAttachment> attachments) => MailCompose.UploadPartsOf(attachments);

    private static List<ComposeRecipient> ParseRecipients(string field) => MailCompose.ParseRecipients(field);
}

using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Features.Triage;

namespace Jewel.JPMS.Features.Closeout.Detail;

// The communications panel's working state: the tagged-mail list (rendered by the shared
// CorrespondenceThreadList, which owns expansion and full-body fetching itself), one open
// composer at a time (a reply/forward above the list, or the new-email form at the top — blank,
// or pre-written as the send-to-supplier / chase), and the outcome note after a send.
public partial class DefectCommunicationsPanel
{
    [Parameter, EditorRequired] public Defect Defect { get; set; } = default!;
    /// <summary>The defect's project — names the email subject; null degrades the subject only.</summary>
    [Parameter] public Project? Project { get; set; }
    /// <summary>The defect's supplier as a directory record — greets the contact by name; null
    /// falls back to the defect's free-typed address and a plain greeting.</summary>
    [Parameter] public Subcontractor? Supplier { get; set; }
    /// <summary>Whether the signed-in user may send from the projects mailbox — the API's compose
    /// gate (every internal role), mirrored by the page. Without it the list is read-only.</summary>
    [Parameter] public bool CanSend { get; set; }
    /// <summary>Project pool for the composer's drawing / photo attachment sources.</summary>
    [Parameter] public IReadOnlyList<Project> Projects { get; set; } = Array.Empty<Project>();
    /// <summary>Raised after every send outcome, so the page can re-read the defect (the server
    /// stamps SentToSupplierAt / Open → In progress when the email went to the supplier).</summary>
    [Parameter] public EventCallback<ComposeOutcome> OnSent { get; set; }

    private bool loading = true;
    private bool refreshing;
    private string? failed;
    private IReadOnlyList<MailboxMessage> emails = Array.Empty<MailboxMessage>();

    // One composer at a time: a reply (or forward — composeIsForward says which) above the list,
    // or the new-email form — blank or prefilled; composeKey re-mounts it so a new prefill lands.
    private MailboxMessage? replyingTo;
    private bool composeIsForward;
    private bool composingNew;
    private ComposePrefill? prefill;
    private string? composeLabel;
    private int composeKey;

    private string? sentNote;
    private string? sentNoteWebLink;

    private string OwnTag => $"JPMS/{Defect.Reference}";

    // The shared thread list renders Reply/Forward only when a delegate is passed — without send
    // rights the callbacks stay empty and the list is read-only.
    private EventCallback<MailboxMessage> ReplyCallback =>
        CanSend ? EventCallback.Factory.Create<MailboxMessage>(this, StartReply) : default;

    private EventCallback<MailboxMessage> ForwardCallback =>
        CanSend ? EventCallback.Factory.Create<MailboxMessage>(this, StartForward) : default;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
        loading = false;
    }

    private async Task LoadAsync()
    {
        failed = null;
        try
        {
            emails = (await Queries.AskAsync(new ListRecordEmails(RecordType.Defect, Defect.DefectId), CancellationToken.None))
                .OrderByDescending(email => email.ReceivedAt)
                .ToList();
        }
        catch
        {
            failed = "Couldn't read the linked emails. The reason is in the red bar above — refresh to try again.";
        }
    }

    private async Task RefreshAsync()
    {
        if (refreshing) return;
        refreshing = true;
        try { await LoadAsync(); }
        finally { refreshing = false; }
    }

    /// <summary>The page's "Send to supplier" (first send) or "Chase supplier": opens the new-email
    /// composer pre-addressed to the supplier and pre-written from the defect, for editing.</summary>
    public void StartSupplierEmail(bool chase)
    {
        replyingTo = null;
        sentNote = sentNoteWebLink = null;
        prefill = chase
            ? DefectSuppliers.ChaseEmail(Defect, Project, Supplier)
            : DefectSuppliers.RaiseEmail(Defect, Project, Supplier);
        composeLabel = chase
            ? $"Chasing {Defect.SupplierLabel} — edit the wording, then Send."
            : $"Sending {Defect.Reference} to {Defect.SupplierLabel} — edit the wording, then Send. Their reply files itself back here.";
        composeKey++;
        composingNew = true;
        StateHasChanged();
    }

    private void StartReply(MailboxMessage email)
    {
        composingNew = false;
        sentNote = sentNoteWebLink = null;
        replyingTo = email;
        composeIsForward = false;
    }

    private void StartForward(MailboxMessage email)
    {
        composingNew = false;
        sentNote = sentNoteWebLink = null;
        replyingTo = email;
        composeIsForward = true;
    }

    private void StartNewEmail()
    {
        replyingTo = null;
        sentNote = sentNoteWebLink = null;
        prefill = string.IsNullOrWhiteSpace(Defect.SupplierEmail) ? null : new ComposePrefill(To: Defect.SupplierEmail);
        composeLabel = null;
        composeKey++;
        composingNew = true;
    }

    private void CloseComposer()
    {
        replyingTo = null;
        composeIsForward = false;
        composingNew = false;
        prefill = null;
        composeLabel = null;
    }

    private async Task HandleSent(ComposeOutcome outcome)
    {
        CloseComposer();
        // The list reads the mailbox by tag, and the sent copy files itself under this defect's
        // tag — but Graph can take a moment to surface it, so the note manages that expectation.
        sentNote = outcome.Sent
            ? $"Sent \"{outcome.Subject}\" to {string.Join("; ", outcome.To)}. It can take a moment to appear in this list — refresh if it isn't here yet."
            : outcome.FailureNote
                ?? "The email is saved as a draft in the projects mailbox — review and send it from Outlook.";
        sentNoteWebLink = outcome.WebLink;
        if (OnSent.HasDelegate) await OnSent.InvokeAsync(outcome);
        await RefreshAsync();
    }
}
